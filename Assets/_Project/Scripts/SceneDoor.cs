using System.Collections;
using UnityEngine;

// Puerta / salida de zona. Al entrar el jugador hace fade y carga otra escena.
// Poner en un objeto con Collider2D (se marca como Trigger solo).
// Opcional: exigir una llave y mostrar sprite de puerta cerrada / abierta.
[RequireComponent(typeof(Collider2D))]
public class SceneDoor : MonoBehaviour
{
    [SerializeField] private string targetScene = "SC_Boss1";
    [SerializeField] private string targetSpawnId = "Entrada";

    [Header("Llave (opcional, dejar vacio si la puerta siempre esta abierta)")]
    [SerializeField] private string requiredKeyId = "";
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [Tooltip("Collider SOLIDO (sin Is Trigger) que bloquea el paso mientras la puerta esta cerrada")]
    [SerializeField] private Collider2D blocker;
    [SerializeField] private string lockedMessage = "Necesitas una llave";
    [SerializeField] private bool consumeKey = false;

    [Header("Animacion de entrada")]
    [SerializeField] private bool walkIn = true;
    [SerializeField] private float walkInTime = 0.7f;
    [SerializeField] private float endScale = 0.85f;

    [Header("Sonido (Resources/Audio). Vacio = sin sonido")]
    [Tooltip("Zumbido en loop mientras la puerta esta abierta (posicional)")]
    [SerializeField] private string sonidoAbierta = "Jefe/portal_idle_loop";
    [SerializeField] private string sonidoEntrar = "Jefe/portal_enter";
    [Range(0f, 1f)] [SerializeField] private float volumenAbierta = 0.6f;
    [Range(0f, 1f)] [SerializeField] private float volumenEntrar = 0.9f;

    private bool used;
    private float nextMessageTime;
    private VozEnemigo voz;
    private bool zumbando;

    private bool IsUnlocked => KeyInventory.Has(requiredKeyId);

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnEnable()
    {
        KeyInventory.OnChanged += Refresh;
        Refresh();
    }

    private void OnDisable() => KeyInventory.OnChanged -= Refresh;

    private void Refresh()
    {
        if (blocker != null) blocker.enabled = !IsUnlocked;
        RefrescarSonido();
        if (doorRenderer == null) return;
        Sprite s = IsUnlocked ? openSprite : closedSprite;
        if (s != null) doorRenderer.sprite = s;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        if (!IsUnlocked)
        {
            if (Time.time >= nextMessageTime)
            {
                nextMessageTime = Time.time + 1.5f;
                FloatingText.Spawn(lockedMessage, transform.position + Vector3.up * 1.5f, new Color(1f, 0.6f, 0.6f));
            }
            return;
        }

        used = true;
        if (consumeKey) KeyInventory.Remove(requiredKeyId);
        if (voz != null && !string.IsNullOrEmpty(sonidoEntrar))
        {
            voz.SonarDesprendido(VozEnemigo.Cargar(sonidoEntrar), volumenEntrar, true); // sigue sonando en la escena nueva
            voz.ApagarFondo(0.8f);
        }
        player.InputLocked = true;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (walkIn) StartCoroutine(WalkIn(player, rb));
        else SceneTransition.Go(targetScene, targetSpawnId);
    }

    // Puerta abierta: zumba (posicional, se oye al acercarse). Cerrada: en silencio
    private void RefrescarSonido()
    {
        if (!Application.isPlaying || string.IsNullOrEmpty(sonidoAbierta) || used) return;
        if (voz == null) voz = GetComponent<VozEnemigo>();
        if (voz == null) voz = gameObject.AddComponent<VozEnemigo>();
        bool abierta = IsUnlocked;
        if (abierta && !zumbando) voz.Fondo(VozEnemigo.Cargar(sonidoAbierta), volumenAbierta);
        else if (!abierta && zumbando) voz.ApagarFondo(0.5f);
        zumbando = abierta;
    }

    private IEnumerator WalkIn(PlayerController player, Rigidbody2D rb)
    {
        var col = GetComponent<Collider2D>();
        float targetX = col.bounds.center.x;
        Vector3 start = player.transform.position;
        Vector3 scale0 = player.transform.localScale;
        var renderers = player.GetComponentsInChildren<SpriteRenderer>();

        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.bodyType = RigidbodyType2D.Kinematic; }

        float t = 0f;
        while (t < walkInTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / walkInTime);
            float e = k * k * (3f - 2f * k);
            var p = player.transform.position;
            p.x = Mathf.Lerp(start.x, targetX, e);
            player.transform.position = p;
            player.transform.localScale = scale0 * Mathf.Lerp(1f, endScale, e);
            Color tint = Color.Lerp(Color.white, new Color(0.08f, 0.1f, 0.14f, 1f), e);
            foreach (var r in renderers) if (r != null) r.color = tint;
            yield return null;
        }

        SceneTransition.Go(targetScene, targetSpawnId);
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.6f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
