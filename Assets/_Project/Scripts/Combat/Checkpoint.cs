using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Punto de control. Al tocarlo el jugador:
//  - se guarda como punto de reaparicion
//  - recupera la vida y la energia
// Poner en un objeto con un Collider2D (Is Trigger). Solo un checkpoint esta "activo" a la vez.
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Header("Reaparicion")]
    [Tooltip("Donde reaparece el jugador. Vacio = la posicion de este objeto")]
    [SerializeField] private Transform respawnPoint;

    [Header("Efecto")]
    [SerializeField] private bool healEveryVisit = true;
    [Tooltip("Tambien llena la barra de energia. Apagado: la energia solo se gana peleando")]
    [SerializeField] private bool recargarEnergia = false;

    [Header("Visual (opcional)")]
    [SerializeField] private SpriteRenderer sprite;
    [Tooltip("Imagen del checkpoint apagado y encendido. Si se asignan, se cambia entre ellas")]
    [SerializeField] private Sprite inactiveSprite;
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Light2D glow;
    [SerializeField] private Color inactiveColor = new Color(0.55f, 0.62f, 0.75f, 1f);
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private float inactiveIntensity = 0.35f;
    [SerializeField] private float activeIntensity = 1.2f;
    [SerializeField] private float flashMultiplier = 2.2f;
    [SerializeField] private float flashTime = 0.6f;

    private static Checkpoint current;
    private Coroutine flashRoutine;

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (glow == null) glow = GetComponentInChildren<Light2D>();
        SetActiveVisual(false);
    }

    private void OnDestroy()
    {
        if (current == this) current = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null || health.IsDead) return;

        bool isNew = current != this;

        if (isNew)
        {
            if (current != null) current.SetActiveVisual(false);
            current = this;
            Vector3 pos = respawnPoint != null ? respawnPoint.position : transform.position;
            health.SetRespawnPoint(pos);
            SetActiveVisual(true);
            SonidosMundo.Checkpoint();
        }

        if (!isNew && !healEveryVisit) return;

        bool healed = health.HealToFull();
        PlayerEnergy energy = health.GetComponent<PlayerEnergy>();
        bool recharged = recargarEnergia && energy != null && energy.RefillToFull();

        if (isNew || healed || recharged) Flash();
    }

    private void SetActiveVisual(bool on)
    {
        if (sprite != null)
        {
            if (inactiveSprite != null && activeSprite != null)
            {
                sprite.sprite = on ? activeSprite : inactiveSprite;
                sprite.color = Color.white;
            }
            else
            {
                sprite.color = on ? activeColor : inactiveColor;
            }
        }
        if (glow != null) glow.intensity = on ? activeIntensity : inactiveIntensity;
    }

    private void Flash()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        if (glow == null) yield break;
        float t = 0f;
        while (t < flashTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / flashTime) * Mathf.PI); // 0 -> 1 -> 0
            glow.intensity = Mathf.Lerp(activeIntensity, activeIntensity * flashMultiplier, k);
            yield return null;
        }
        glow.intensity = activeIntensity;
        flashRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p = respawnPoint != null ? respawnPoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(p, 0.4f);
        Gizmos.DrawLine(p, p + Vector3.up * 1.2f);
    }
}
