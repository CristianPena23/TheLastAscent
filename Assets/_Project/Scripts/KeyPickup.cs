using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Llave flotante con brillo. El jugador la toma al tocarla.
// Poner en un objeto con SpriteRenderer (el sprite de la llave). El Collider2D se crea solo.
[RequireComponent(typeof(Collider2D))]
public class KeyPickup : MonoBehaviour
{
    [Header("Llave")]
    [SerializeField] private string keyId = "LlaveJefe";
    [SerializeField] private string pickupMessage = "Llave obtenida";

    [Header("Flotar")]
    [SerializeField] private float bobHeight = 0.18f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float swayDegrees = 6f;

    [Header("Brillo")]
    [SerializeField] private Color glowColor = new Color(0.55f, 0.85f, 1f);
    [SerializeField] private float glowIntensity = 2.4f;
    [SerializeField] private float glowRadius = 4f;
    [SerializeField] private float haloSize = 3f;
    [SerializeField] private float haloAlpha = 0.55f;
    [SerializeField] private float pulseAmount = 0.35f;
    [SerializeField] private float pulseSpeed = 2.5f;

    private Light2D glow;
    private SpriteRenderer halo;
    private SpriteRenderer sr;
    private Vector3 startPos;
    private bool taken;

    private void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
        var circle = c as CircleCollider2D;
        if (circle != null) circle.radius = 0.8f;
    }

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        glow = GetComponentInChildren<Light2D>();
        if (glow == null)
        {
            var go = new GameObject("Brillo");
            go.transform.SetParent(transform, false);
            glow = go.AddComponent<Light2D>();
            glow.lightType = Light2D.LightType.Point;
            glow.color = glowColor;
            glow.intensity = glowIntensity;
            glow.pointLightOuterRadius = glowRadius;
            glow.pointLightInnerRadius = 0f;
        }
        halo = GlowHalo.Create(transform, glowColor, haloSize, sr);
    }

    private void Start()
    {
        startPos = transform.position;
        if (KeyInventory.Has(keyId)) Destroy(gameObject); // ya la tomo antes
    }

    private void Update()
    {
        if (taken) return;
        float s = Mathf.Sin(Time.time * bobSpeed);
        transform.position = startPos + Vector3.up * (s * bobHeight);
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * bobSpeed * 0.7f) * swayDegrees);
        if (glow != null)
            glow.intensity = glowIntensity * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount);
        if (halo != null)
        {
            var hc = glowColor;
            hc.a = Mathf.Clamp01(haloAlpha * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount));
            halo.color = hc;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (taken) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        taken = true;
        KeyInventory.Add(keyId);
        if (!string.IsNullOrEmpty(pickupMessage))
            FloatingText.Spawn(pickupMessage, transform.position + Vector3.up * 0.8f, glowColor);
        StartCoroutine(CollectEffect());
    }

    private IEnumerator CollectEffect()
    {
        GetComponent<Collider2D>().enabled = false;
        float t = 0f, dur = 0.5f;
        Vector3 s0 = transform.localScale;
        float i0 = glow != null ? glow.intensity : 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            transform.localScale = s0 * (1f + k * 0.8f);
            transform.position += Vector3.up * (2f * Time.deltaTime);
            if (sr != null) { var c = sr.color; c.a = 1f - k; sr.color = c; }
            if (glow != null) glow.intensity = Mathf.Lerp(i0 * 2.5f, 0f, k);
            if (halo != null) { var hc = glowColor; hc.a = haloAlpha * (1f - k); halo.color = hc; }
            yield return null;
        }
        Destroy(gameObject);
    }
}
