using UnityEngine;
using UnityEngine.Rendering.Universal;

// Pon este script en un cristal (objeto con SpriteRenderer). Crea solo la luz azul, el halo y el parpadeo suave.
[RequireComponent(typeof(SpriteRenderer))]
public class CristalBrillo : MonoBehaviour
{
    [Header("Color")]
    public Color lightColor = new Color(0.45f, 0.85f, 1f, 1f);

    [Header("Luz (Light2D)")]
    public bool useLight = true;
    public float intensity = 1.2f;
    public float radius = 3.5f;

    [Header("Halo detras del sprite")]
    public bool useHalo = true;
    public float haloScale = 1.6f;   // multiplo del tamano del cristal
    [Range(0f, 1f)] public float haloAlpha = 0.35f;

    [Header("Parpadeo")]
    public float flickerAmount = 0.15f;
    public float flickerSpeed = 1.2f;

    [Header("Sprite")]
    public bool unlit = true;        // el cristal no se oscurece con la luz global

    SpriteRenderer sr, halo;
    Light2D light2d;
    float seed, baseIntensity;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        seed = Random.value * 100f;
        baseIntensity = intensity;

        if (unlit)
        {
            var mat = GlowHalo.UnlitMaterial();
            if (mat != null) sr.sharedMaterial = mat;
        }

        Vector3 center = sr.bounds.center;

        if (useLight)
        {
            var go = new GameObject("CristalLuz");
            go.transform.SetParent(transform, false);
            go.transform.position = center;
            light2d = go.AddComponent<Light2D>();
            light2d.lightType = Light2D.LightType.Point;
            light2d.color = lightColor;
            light2d.intensity = intensity;
            light2d.pointLightOuterRadius = radius;
            light2d.pointLightInnerRadius = 0f;
        }

        if (useHalo)
        {
            float size = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y) * haloScale;
            var c = lightColor; c.a = haloAlpha;
            halo = GlowHalo.Create(transform, c, size, sr);
            halo.transform.position = center;
        }
    }

    void Update()
    {
        float n = 1f + (Mathf.PerlinNoise(seed, Time.time * flickerSpeed) - 0.5f) * 2f * flickerAmount;
        if (light2d != null) light2d.intensity = baseIntensity * n;
        if (halo != null)
        {
            var c = halo.color; c.a = haloAlpha * n; halo.color = c;
        }
    }
}
