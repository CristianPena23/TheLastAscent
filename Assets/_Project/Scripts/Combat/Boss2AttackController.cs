using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

// Ataques del jefe 2 (Circulo 8): columnas de fuego que salen del suelo y bolas de fuego horizontales.
// Poner en el objeto Boss de SC_Boss2 (el que tiene el Animator con AC_Boss2).
// Reemplaza a BossAttackController (desmarca ese componente o quitalo).
public class Boss2AttackController : MonoBehaviour
{
    public enum ColumnPattern
    {
        None,       // sin columnas
        NearBoss,   // columnas pegadas al jefe, hacia donde esta el jugador (tajo de fuego)
        Sweep,      // repartidas por toda la arena, en orden
        Target,     // alrededor de donde esta el jugador
        Chase       // una tras otra justo donde esta el jugador
    }

    [Serializable]
    public class BossAttack
    {
        public string name = "Ataque";
        public string animatorTrigger = "Attack";
        [Tooltip("Segundos desde que empieza la animacion hasta el golpe")]
        public float hitDelay = 0.5f;
        [Tooltip("Duracion total de la animacion en segundos")]
        public float duration = 1.2f;
        public int damage = 1;

        [Header("Columnas de fuego (del suelo hacia arriba)")]
        public ColumnPattern columns = ColumnPattern.NearBoss;
        public int columnCount = 3;
        public float columnWidth = 2.5f;
        public float columnHeight = 4f;
        [Tooltip("Segundos de aviso antes de que salga cada columna")]
        public float columnWarn = 0.7f;
        [Tooltip("Retraso entre una columna y la siguiente")]
        public float columnStagger = 0.15f;
        [Tooltip("Target / NearBoss: separacion entre columnas")]
        public float columnSpacing = 2.5f;

        [Header("Bolas de fuego (horizontales)")]
        [Tooltip("Cuantas bolas salen hacia cada lado. 0 = sin bolas")]
        public int ballsPerSide = 0;
        [Tooltip("Alturas sobre el suelo (en unidades). Se usan en orden, una por bola")]
        public float[] ballHeights = { 0.8f, 2.4f };
        public float ballSpeed = 9f;
        public float ballSize = 2.2f;
        [Tooltip("Retraso entre una bola y la siguiente")]
        public float ballStagger = 0.5f;
        public bool ballsToLeft = true;
        public bool ballsToRight = true;
    }

    [Header("Animacion")]
    [SerializeField] private Animator animator;

    [Header("Arena (coordenadas del mundo)")]
    [SerializeField] private float arenaMinX = -18f;
    [SerializeField] private float arenaMaxX = 19f;
    [SerializeField] private float groundY = -1f;

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
        new BossAttack { name = "Tajo de fuego", animatorTrigger = "Attack", hitDelay = 0.45f, duration = 1.1f,
                         columns = ColumnPattern.NearBoss, columnCount = 3, columnWidth = 2.5f, columnHeight = 4f,
                         columnWarn = 0.45f, columnStagger = 0.12f, columnSpacing = 2.2f },
        new BossAttack { name = "Manos arriba (columnas y bolas)", animatorTrigger = "RaiseHands", hitDelay = 0.6f, duration = 1.6f,
                         columns = ColumnPattern.Chase, columnCount = 5, columnWidth = 2.5f, columnHeight = 5.5f,
                         columnWarn = 0.7f, columnStagger = 0.35f,
                         ballsPerSide = 2, ballHeights = new float[] { 1.1f, 3f }, ballSpeed = 9f, ballSize = 2.2f, ballStagger = 0.6f }
    };

    [Header("Fuego")]
    [SerializeField] private Color warnColor = new Color(1f, 0.35f, 0.05f, 0.5f);
    [SerializeField] private float columnActiveTime = 0.6f;
    [SerializeField] private int sortingOrder = 5;
    [Tooltip("Animacion de la columna de fuego (frames). Vacio = llama dibujada por codigo")]
    [SerializeField] private Sprite[] columnFrames;
    [Tooltip("Animacion de la bola de fuego (frames). Vacio = bola dibujada por codigo")]
    [SerializeField] private Sprite[] ballFrames;
    [SerializeField] private float fireFps = 12f;

    [Header("Fase 2 (rabia)")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float phase2HealthPercent = 0.5f;
    [Tooltip("1 = igual. 1.35 = 35% mas rapido")]
    [SerializeField] private float phase2Speed = 1.35f;

    public bool Active { get; set; } = true;

    private PlayerController player;
    private BossHealth health;
    private float speed = 1f;
    private bool phase2;
    private bool fightStarted;
    private PlayerHealth playerHealth;

    public void BeginFight() => fightStarted = true;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        player = FindFirstObjectByType<PlayerController>();
        health = GetComponent<BossHealth>();
        if (health != null) health.Changed += OnHealthChanged;
        playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null) playerHealth.OnDied += OnPlayerDied;
        StartCoroutine(Loop());
    }

    private void OnDestroy()
    {
        if (health != null) health.Changed -= OnHealthChanged;
        if (playerHealth != null) playerHealth.OnDied -= OnPlayerDied;
    }

    // El jugador murio: se cancela todo, se borra el fuego y el jefe vuelve a su estado inicial
    private void OnPlayerDied()
    {
        if (health != null && health.IsDead) return;
        StopAllCoroutines();
        StartCoroutine(ResetFight());
    }

    private IEnumerator ResetFight()
    {
        foreach (var c in FindObjectsByType<FuegoColumna>(FindObjectsSortMode.None)) Destroy(c.gameObject);
        foreach (var b in FindObjectsByType<FuegoBola>(FindObjectsSortMode.None)) Destroy(b.gameObject);

        phase2 = false;
        speed = 1f;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.speed = 1f;
            animator.Rebind();   // vuelve al estado inicial (Idle) y limpia triggers
            animator.Update(0f);
        }

        // Esperar a que el jugador reaparezca
        while (playerHealth != null && playerHealth.IsDead) yield return null;
        yield return new WaitForSeconds(0.5f);

        fightStarted = false;    // otra vez espera a que el jugador se mueva
        StartCoroutine(Loop());
    }

    private void OnHealthChanged(int cur, int max)
    {
        fightStarted = true;
        if (max <= 0 || cur <= 0) return;
        bool debajo = (float)cur / max <= phase2HealthPercent;

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
        yield return new WaitForSeconds(firstAttackDelay);
        int last = -1;

        while (true)
        {
            if (health != null && health.IsDead) yield break;
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

    private IEnumerator WaitForPlayerMove()
    {
        yield return new WaitForSeconds(0.3f);
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
        if (animator != null && animator.runtimeAnimatorController != null && !string.IsNullOrEmpty(a.animatorTrigger))
            animator.SetTrigger(a.animatorTrigger);

        StartCoroutine(ColumnsRoutine(a));
        StartCoroutine(BallsRoutine(a));

        yield return new WaitForSeconds(a.duration / speed);
    }

    // ---------- Columnas ----------
    private IEnumerator ColumnsRoutine(BossAttack a)
    {
        if (a.columns == ColumnPattern.None) yield break;
        yield return new WaitForSeconds(a.hitDelay / speed);

        Vector2 size = new Vector2(a.columnWidth, a.columnHeight);
        float half = a.columnWidth * 0.5f;
        int count = Mathf.Max(1, a.columnCount);
        float warn = a.columnWarn / speed;
        float px = player != null ? player.transform.position.x : (arenaMinX + arenaMaxX) * 0.5f;
        float bx = transform.position.x;
        Rigidbody2D rb = player != null ? player.GetComponent<Rigidbody2D>() : null;

        for (int i = 0; i < count; i++)
        {
            float x;
            switch (a.columns)
            {
                case ColumnPattern.NearBoss:
                {
                    float dir = px >= bx ? 1f : -1f;
                    x = bx + dir * (a.columnSpacing * (i + 1));
                    break;
                }
                case ColumnPattern.Sweep:
                {
                    float f = count == 1 ? 0.5f : (float)i / (count - 1);
                    x = Mathf.Lerp(arenaMinX + half, arenaMaxX - half, f);
                    break;
                }
                case ColumnPattern.Target:
                {
                    float offset = (i - (count - 1) * 0.5f) * a.columnSpacing;
                    x = px + offset;
                    break;
                }
                default: // Chase
                {
                    float cx = player != null ? player.transform.position.x : px;
                    if (rb != null) cx += rb.linearVelocity.x * 0.3f;
                    x = cx;
                    break;
                }
            }
            x = Mathf.Clamp(x, arenaMinX + half, arenaMaxX - half);

            FuegoColumna.Spawn(new Vector2(x, groundY + a.columnHeight * 0.5f), size, a.damage, warn,
                               columnActiveTime, warnColor, sortingOrder, columnFrames, fireFps);
            yield return new WaitForSeconds(a.columnStagger / speed);
        }
    }

    // ---------- Bolas de fuego ----------
    private IEnumerator BallsRoutine(BossAttack a)
    {
        if (a.ballsPerSide <= 0) yield break;
        yield return new WaitForSeconds(a.hitDelay / speed);

        float[] heights = (a.ballHeights != null && a.ballHeights.Length > 0) ? a.ballHeights : new float[] { 1f };
        for (int i = 0; i < a.ballsPerSide; i++)
        {
            float h = heights[i % heights.Length];
            Vector2 pos = new Vector2(transform.position.x, groundY + h);
            if (a.ballsToRight)
                FuegoBola.Spawn(pos, 1f, a.ballSpeed * speed, a.ballSize, a.damage, arenaMinX, arenaMaxX, sortingOrder, ballFrames, fireFps);
            if (a.ballsToLeft)
                FuegoBola.Spawn(pos, -1f, a.ballSpeed * speed, a.ballSize, a.damage, arenaMinX, arenaMaxX, sortingOrder, ballFrames, fireFps);
            yield return new WaitForSeconds(a.ballStagger / speed);
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
