using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

// Jefe de fondo: reproduce sus animaciones de ataque y crea zonas de peligro en el suelo.
// Poner en el objeto Boss de SC_Boss1 (el que tiene el Animator con AC_Boss1).
public class BossAttackController : MonoBehaviour
{
    public enum Pattern
    {
        Sweep,   // zonas repartidas por toda la arena, en orden (ola de hielo)
        Target,  // zonas alrededor de donde esta el jugador (cristales)
        Chase    // zonas una tras otra justo donde esta el jugador (lo persiguen)
    }

    public enum Side
    {
        Whole,        // toda la arena
        RightOfBoss,  // desde el jefe hacia la derecha (de la pantalla)
        LeftOfBoss    // desde el jefe hacia la izquierda
    }

    [Serializable]
    public class BossAttack
    {
        public string name = "Ataque";
        public string animatorTrigger = "AttackRight";
        public Pattern pattern = Pattern.Sweep;
        [Tooltip("Segundos desde que empieza la animacion hasta que la zona hace dano (el golpe contra el suelo)")]
        public float hitDelay = 0.4f;
        [Tooltip("Duracion total de la animacion en segundos")]
        public float duration = 1.6f;
        public int zoneCount = 5;
        public float zoneWidth = 2.5f;
        public float zoneHeight = 3f;
        [Tooltip("Sweep: retraso entre una zona y la siguiente")]
        public float stagger = 0.15f;
        public bool sweepLeftToRight = true;
        [Tooltip("Target: separacion entre zonas")]
        public float targetSpacing = 3.5f;
        public int damage = 1;

        [Header("Persecucion (solo Chase)")]
        [Tooltip("Segundos de aviso de cada pico. Menos = mas dificil")]
        public float chaseWarn = 0.7f;
        [Tooltip("Cuanto se adelanta a donde va el jugador (segundos de su velocidad). 0 = justo donde esta")]
        public float chaseLead = 0.3f;

        [Header("Lado (solo Sweep)")]
        public Side side = Side.Whole;

        [Header("Segundo golpe (combo). 0 = sin segundo golpe")]
        public float secondHitDelay = 0f;
        public Side secondSide = Side.LeftOfBoss;

        [Header("Espejo (usa la animacion de la otra mano girada)")]
        public bool flipX = false;

        [Tooltip("Sonido (Resources/Audio/Jefe). Vacio = automatico segun el trigger y el espejo")]
        public string sonido = "";
    }

    [Header("Animacion")]
    [SerializeField] private Animator animator;
    [Tooltip("SpriteRenderer del jefe (para espejar). Vacio = se busca solo")]
    [SerializeField] private SpriteRenderer bossSprite;

    [Header("Arena (coordenadas del mundo)")]
    [SerializeField] private float arenaMinX = -14f;
    [SerializeField] private float arenaMaxX = 14f;
    [SerializeField] private float groundY = 0f;

    [Header("Inicio del combate")]
    [Tooltip("El jefe no ataca hasta que el jugador se mueva (o le pegue)")]
    [SerializeField] private bool waitForPlayerMove = true;

    [Header("Ritmo")]
    [SerializeField] private float firstAttackDelay = 2.5f;
    [SerializeField] private float minPause = 2.5f;
    [SerializeField] private float maxPause = 4.5f;

    [Header("Ataques")]
    [SerializeField] private BossAttack[] attacks =
    {
        new BossAttack { name = "Combo (derecha y luego izquierda)", animatorTrigger = "AttackCombo", pattern = Pattern.Sweep,
                         hitDelay = 0.37f, duration = 1.6f, zoneCount = 4, stagger = 0.12f,
                         side = Side.RightOfBoss, secondHitDelay = 0.77f, secondSide = Side.LeftOfBoss },
        new BossAttack { name = "Golpe izquierdo (cristales)", animatorTrigger = "AttackLeft", pattern = Pattern.Sweep,
                         hitDelay = 0.33f, duration = 0.85f, zoneCount = 3, stagger = 0.1f,
                         side = Side.LeftOfBoss },
        new BossAttack { name = "Golpe derecho (espejo)", animatorTrigger = "AttackLeft", pattern = Pattern.Sweep,
                         hitDelay = 0.33f, duration = 0.88f, zoneCount = 3, stagger = 0.1f,
                         side = Side.RightOfBoss, flipX = true }
    };

    [Header("Zonas de peligro")]
    [SerializeField] private float activeTime = 0.5f;
    [SerializeField] private Color warnColor = new Color(1f, 0.3f, 0.3f, 0.35f);
    [SerializeField] private Color activeColor = new Color(0.7f, 0.9f, 1f, 0.85f);
    [SerializeField] private int sortingOrder = 5;

    [Header("Fase 2 (rabia)")]
    [Tooltip("Cuando la vida baja de este porcentaje, el jefe ataca mas rapido")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float phase2HealthPercent = 0.5f;
    [Tooltip("1 = igual. 1.35 = 35% mas rapido (animaciones, avisos y pausas)")]
    [SerializeField] private float phase2Speed = 1.35f;

    [Header("Picos de hielo (opcional)")]
    [Tooltip("Sprites de pico (pivote abajo). Vacio = picos dibujados por codigo")]
    [SerializeField] private Sprite[] spikeSprites;

    public bool Active { get; set; } = true;

    private PlayerController player;
    private BossHealth health;
    private float speed = 1f;
    private bool phase2;
    private bool fightStarted;

    public void BeginFight() => fightStarted = true;

    private void Start()
    {
        if (bossSprite == null && animator != null)
            bossSprite = animator.GetComponent<SpriteRenderer>();
        if (bossSprite == null)
            bossSprite = GetComponentInChildren<SpriteRenderer>();
        player = FindFirstObjectByType<PlayerController>();
        health = GetComponent<BossHealth>();
        if (health != null) health.Changed += OnHealthChanged;
        StartCoroutine(Loop());
    }

    private void OnDestroy()
    {
        if (health != null) health.Changed -= OnHealthChanged;
    }

    private void OnHealthChanged(int cur, int max)
    {
        fightStarted = true; // si le pegan, empieza el combate
        if (max <= 0 || cur <= 0) return;
        bool debajo = (float)cur / max <= phase2HealthPercent;

        // Si recupera la vida (el jugador murio), vuelve a la fase 1
        if (phase2 && !debajo)
        {
            phase2 = false;
            speed = 1f;
            if (animator != null) animator.speed = 1f;
            return;
        }
        if (phase2 || !debajo) return;

        phase2 = true;
        speed = Mathf.Max(0.1f, phase2Speed);
        if (animator != null) animator.speed = speed;
    }

    private IEnumerator Loop()
    {
        if (waitForPlayerMove) yield return WaitForPlayerMove();
        fightStarted = true;
        if (SonidosJefe.Instancia != null) SonidosJefe.Instancia.EmpezarPelea(); // entra la musica de pelea
        yield return new WaitForSeconds(firstAttackDelay);
        int last = -1;

        while (true)
        {
            if (!Active || attacks == null || attacks.Length == 0)
            {
                yield return null;
                continue;
            }

            int i = Random.Range(0, attacks.Length);
            if (attacks.Length > 1 && i == last) i = (i + 1) % attacks.Length;
            last = i;

            yield return DoAttack(attacks[i]);
            yield return new WaitForSeconds(Random.Range(minPause, maxPause) / speed);
        }
    }

    // Espera a que el jugador camine o salte (despues de aparecer en la escena)
    private IEnumerator WaitForPlayerMove()
    {
        yield return new WaitForSeconds(0.3f); // deja que el jugador termine de aparecer
        if (player == null) yield break;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        float startX = player.transform.position.x;

        while (!fightStarted)
        {
            bool walked = Mathf.Abs(player.transform.position.x - startX) > 0.5f;
            bool jumped = rb != null && rb.linearVelocity.y > 2f;
            if (walked || jumped) break;
            yield return null;
        }
        fightStarted = true;
    }

    private IEnumerator DoAttack(BossAttack a)
    {
        if (bossSprite != null) bossSprite.flipX = a.flipX;
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(a.animatorTrigger);
        if (SonidosJefe.Instancia != null)
            SonidosJefe.Instancia.Ataque(a.animatorTrigger, a.flipX, a.sonido, a.hitDelay / speed, a.duration / speed);

        if (a.pattern == Pattern.Chase)
        {
            StartCoroutine(ChaseRoutine(a));
        }
        else
        {
            SpawnZones(a, a.side, a.hitDelay / speed);
            if (a.secondHitDelay > 0f)
                SpawnZones(a, a.secondSide, a.secondHitDelay / speed);
        }
        yield return new WaitForSeconds(a.duration / speed);
        if (bossSprite != null) bossSprite.flipX = false;
    }

    // Cada pico aparece donde esta el jugador (adelantandose un poco a su movimiento)
    private IEnumerator ChaseRoutine(BossAttack a)
    {
        yield return new WaitForSeconds(a.hitDelay / speed);

        Rigidbody2D rb = player != null ? player.GetComponent<Rigidbody2D>() : null;
        Vector2 size = new Vector2(a.zoneWidth, a.zoneHeight);
        float half = a.zoneWidth * 0.5f;
        int count = Mathf.Max(1, a.zoneCount);

        for (int i = 0; i < count; i++)
        {
            float px = player != null ? player.transform.position.x : (arenaMinX + arenaMaxX) * 0.5f;
            if (rb != null) px += rb.linearVelocity.x * a.chaseLead;
            float x = Mathf.Clamp(px, arenaMinX + half, arenaMaxX - half);

            HazardZone.Spawn(new Vector2(x, groundY + a.zoneHeight * 0.5f), size, a.damage,
                             a.chaseWarn / speed, activeTime, warnColor, activeColor, sortingOrder, spikeSprites);
            yield return new WaitForSeconds(a.stagger / speed);
        }
    }

    private void SpawnZones(BossAttack a, Side side, float hitDelay)
    {
        Vector2 size = new Vector2(a.zoneWidth, a.zoneHeight);
        float cy = groundY + a.zoneHeight * 0.5f;
        float half = a.zoneWidth * 0.5f;
        int count = Mathf.Max(1, a.zoneCount);

        if (a.pattern == Pattern.Sweep)
        {
            float bx = transform.position.x;
            float minX = arenaMinX, maxX = arenaMaxX;
            bool leftToRight = a.sweepLeftToRight;
            if (side == Side.RightOfBoss) { minX = Mathf.Max(arenaMinX, bx); leftToRight = true; }
            else if (side == Side.LeftOfBoss) { maxX = Mathf.Min(arenaMaxX, bx); leftToRight = false; }

            for (int i = 0; i < count; i++)
            {
                float f = count == 1 ? 0.5f : (float)i / (count - 1);
                float x = Mathf.Lerp(minX + half, maxX - half, f);
                int order = leftToRight ? i : (count - 1 - i);
                float warn = hitDelay + order * a.stagger / speed;
                HazardZone.Spawn(new Vector2(x, cy), size, a.damage, warn, activeTime, warnColor, activeColor, sortingOrder, spikeSprites);
            }
        }
        else
        {
            float px = player != null ? player.transform.position.x : (arenaMinX + arenaMaxX) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float offset = (i - (count - 1) * 0.5f) * a.targetSpacing;
                float x = Mathf.Clamp(px + offset, arenaMinX + half, arenaMaxX - half);
                HazardZone.Spawn(new Vector2(x, cy), size, a.damage, hitDelay, activeTime, warnColor, activeColor, sortingOrder, spikeSprites);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
        Gizmos.DrawLine(new Vector3(arenaMinX, groundY, 0), new Vector3(arenaMaxX, groundY, 0));
        Gizmos.DrawLine(new Vector3(arenaMinX, groundY, 0), new Vector3(arenaMinX, groundY + 4f, 0));
        Gizmos.DrawLine(new Vector3(arenaMaxX, groundY, 0), new Vector3(arenaMaxX, groundY + 4f, 0));
    }
}
