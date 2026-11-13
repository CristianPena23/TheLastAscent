using UnityEngine;
using UnityEngine.Rendering.Universal;

// Da brillo propio a la luciernaga: halo detras del cuerpo y cuerpo que no se oscurece con la escena.
// Se agrega solo a toda Luciernaga al cargar la escena. Si prefieres ajustarlo, anadelo tu al prefab.
[RequireComponent(typeof(Luciernaga))]
public class LuciernagaHalo : MonoBehaviour
{
    [SerializeField] private Color haloColor = new Color(1f, 0.95f, 0.65f);
    [SerializeField] private float haloSize = 1.6f;      // diametro en unidades del mundo
    [SerializeField] private float haloAlpha = 0.75f;
    [SerializeField] private float breathe = 0.2f;
    [SerializeField] private float breatheSpeed = 3f;
    [SerializeField] private bool bodyUnlit = true;      // el cuerpo siempre se ve brillante

    private SpriteRenderer body, halo;
    private Light2D light2d;
    private float baseIntensity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttach()
    {
        foreach (var l in FindObjectsByType<Luciernaga>(FindObjectsSortMode.None))
            if (l.GetComponent<LuciernagaHalo>() == null) l.gameObject.AddComponent<LuciernagaHalo>();
    }

    private void Start()
    {
        body = GetComponent<SpriteRenderer>();
        if (body == null) body = GetComponentInChildren<SpriteRenderer>();
        light2d = GetComponentInChildren<Light2D>();
        if (light2d != null)
        {
            baseIntensity = Mathf.Max(0.01f, light2d.intensity);
            haloColor = Color.Lerp(haloColor, light2d.color, 0.5f);
        }

        halo = GlowHalo.Create(transform, haloColor, haloSize, body);

        if (bodyUnlit && body != null)
        {
            var mat = GlowHalo.UnlitMaterial();
            if (mat != null) body.sharedMaterial = mat;
        }
    }

    private void Update()
    {
        if (halo == null) return;
        float k = 1f + Mathf.Sin(Time.time * breatheSpeed) * breathe;
        if (light2d != null) k *= Mathf.Clamp(light2d.intensity / baseIntensity, 0.5f, 2f);
        var c = haloColor;
        c.a = Mathf.Clamp01(haloAlpha * k);
        halo.color = c;
    }
}
