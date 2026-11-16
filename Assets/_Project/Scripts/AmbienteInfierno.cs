using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Ambiente de infierno (Circulo 8): ceniza que cae, brasas que suben, humo, resplandor de fuego abajo
// y luz calida con parpadeo. Todo se genera solo, igual que AmbienteHielo.
// Uso: arrastra el prefab PF_AmbienteInfierno a la escena (o un objeto vacio con este componente).
public class AmbienteInfierno : MonoBehaviour
{
    [Header("Camara (vacio = camara principal)")]
    [SerializeField] private Camera cam;

    [Header("Viento (positivo = derecha)")]
    [SerializeField] private float wind = 0.5f;

    [Header("Ceniza lejana (detras del jugador)")]
    [SerializeField] private bool farAsh = true;
    [SerializeField] private float farRate = 30f;
    [SerializeField] private Vector2 farSize = new Vector2(0.03f, 0.08f);
    [SerializeField] private float farFall = 0.4f;
    [SerializeField] private float farAlpha = 0.45f;
    [SerializeField] private int farOrder = -1;

    [Header("Ceniza cercana (delante)")]
    [SerializeField] private bool nearAsh = true;
    [SerializeField] private float nearRate = 8f;
    [SerializeField] private Vector2 nearSize = new Vector2(0.08f, 0.18f);
    [SerializeField] private float nearFall = 0.9f;
    [SerializeField] private float nearAlpha = 0.4f;
    [SerializeField] private int nearOrder = 7;

    [Header("Brasas (suben)")]
    [SerializeField] private bool embers = true;
    [SerializeField] private float emberRate = 14f;
    [SerializeField] private Vector2 emberSize = new Vector2(0.03f, 0.09f);
    [SerializeField] private float emberRise = 1.2f;
    [Range(0f, 1f)] [SerializeField] private float emberAlpha = 0.9f;
    [SerializeField] private int emberOrder = 7;

    [Header("Humo")]
    [SerializeField] private bool smoke = true;
    [SerializeField] private float smokeRate = 0.7f;
    [SerializeField] private Vector2 smokeSize = new Vector2(2.5f, 5f);
    [SerializeField] private float smokeRise = 0.5f;
    [SerializeField] private Color smokeColor = new Color(0.3f, 0.2f, 0.2f);
    [Range(0f, 1f)] [SerializeField] private float smokeAlpha = 0.16f;
    [SerializeField] private int smokeBackOrder = -1;
    [SerializeField] private int smokeFrontOrder = 5;

    [Header("Resplandor de fuego (abajo)")]
    [SerializeField] private bool glow = true;
    [SerializeField] private Color glowColor = new Color(1f, 0.4f, 0.1f);
    [Range(0f, 1f)] [SerializeField] private float glowAlpha = 0.35f;
    [Range(0.1f, 0.8f)] [SerializeField] private float glowHeight = 0.35f;
    [SerializeField] private float glowOffsetY = 0f;
    [SerializeField] private int glowBackOrder = -1;
    [SerializeField] private int glowFrontOrder = 7;

    [Header("Luz global (calida y con parpadeo)")]
    [SerializeField] private bool ajustarLuz = true;
    [SerializeField] private Color lightColor = new Color(1f, 0.62f, 0.45f);
    [SerializeField] private float lightIntensity = 0.9f;
    [Range(0f, 0.3f)] [SerializeField] private float lightFlicker = 0.06f;

    private Camera c;
    private float halfH, halfW;
    private Light2D globalLight;
    private readonly List<SpriteRenderer> glowLayers = new List<SpriteRenderer>();
    private readonly List<float> glowBase = new List<float>();
    private readonly List<float> glowPhase = new List<float>();

    private IEnumerator Start()
    {
        yield return null;
        c = cam != null ? cam : Camera.main;
        if (c == null) yield break;

        halfH = c.orthographicSize;
        halfW = halfH * c.aspect;

        var sprite = GlowHalo.GetSprite();
        var mat = ParticleMaterial(sprite.texture);

        var ashGrad = new Color(0.55f, 0.5f, 0.5f);
        if (farAsh) MakeParticles("CenizaLejos", mat, farOrder, true, farRate, farSize, farFall, 0f, farAlpha, ashGrad, ashGrad, 0.15f, false, false, false);
        if (nearAsh) MakeParticles("CenizaCerca", mat, nearOrder, true, nearRate, nearSize, nearFall, 0f, nearAlpha, ashGrad, ashGrad, 0.35f, false, false, false);
        if (embers) MakeParticles("Brasas", mat, emberOrder, false, emberRate, emberSize, emberRise, 0f, emberAlpha, new Color(1f, 0.35f, 0.05f), new Color(1f, 0.8f, 0.25f), 0.8f, true, false, true);
        if (smoke)
        {
            MakeParticles("HumoAtras", mat, smokeBackOrder, false, smokeRate, smokeSize, smokeRise, 9f, smokeAlpha, smokeColor, smokeColor * 0.7f, 0.25f, false, true, false);
            MakeParticles("HumoFrente", mat, smokeFrontOrder, false, smokeRate * 0.5f, smokeSize * 1.2f, smokeRise * 1.2f, 9f, smokeAlpha * 0.6f, smokeColor, smokeColor * 0.7f, 0.25f, false, true, false);
        }
        if (glow) MakeGlow();

        if (ajustarLuz)
        {
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global) { globalLight = l; break; }
            if (globalLight != null) globalLight.color = lightColor;
        }
    }

    private static Material ParticleMaterial(Texture tex)
    {
        var m = new Material(Shader.Find("Sprites/Default"));
        m.mainTexture = tex;
        return m;
    }

    // fallsFromTop: true = nace arriba y cae. false = nace abajo y sube.
    // fixedLife > 0: vida fija en segundos (humo). 0 = lo que tarde en cruzar la pantalla.
    private void MakeParticles(string objName, Material mat, int order, bool fallsFromTop, float rate, Vector2 size,
        float speed, float fixedLife, float alpha, Color c1, Color c2, float noise, bool emberFade, bool grow, bool shrink)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(c.transform, false);
        float y = fallsFromTop ? halfH + 1.5f : -halfH - 1.5f;
        go.transform.localPosition = new Vector3(0f, y, 10f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float travel = halfH * 2f + 3f;
        float life = fixedLife > 0f ? fixedLife : travel / Mathf.Max(0.05f, speed);
        float dir = fallsFromTop ? -1f : 1f;

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.9f, life * 1.1f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        c1.a = alpha; c2.a = alpha;
        main.startColor = new ParticleSystem.MinMaxGradient(c1, c2);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.CeilToInt(rate * life * 1.3f) + 20;
        main.gravityModifier = 0f;

        var em = ps.emission;
        em.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(halfW * 2f + Mathf.Abs(wind) * life * 0.6f + 6f, 0.5f, 1f);
        shape.position = new Vector3(-Mathf.Sign(wind) * Mathf.Abs(wind) * life * 0.25f, 0f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = wind * (speed > 0.5f ? 1.3f : 1f);
        vel.y = dir * speed;
        vel.z = 0f;

        var nz = ps.noise;
        nz.enabled = true;
        nz.strength = noise;
        nz.frequency = 0.25f;
        nz.scrollSpeed = 0.15f;
        nz.quality = ParticleSystemNoiseQuality.Low;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        Color end = emberFade ? new Color(0.5f, 0.12f, 0.05f) : Color.white;
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        if (grow || shrink)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, grow ? new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(1f, 1.6f))
                                                                : new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f)));
        }

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = mat;
        r.sortingOrder = order;

        ps.Play();
    }

    private void MakeGlow()
    {
        var tex = GlowTexture();
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 100f);
        var unlit = GlowHalo.UnlitMaterial();

        float bandH = halfH * 2f * glowHeight;
        float width = halfW * 2f * 1.3f;
        float sx = width / (tex.width / 100f);
        float sy = bandH / (tex.height / 100f);

        int[] orders = { glowBackOrder, glowFrontOrder };
        float[] alphas = { 1f, 0.35f };
        for (int i = 0; i < 2; i++)
        {
            var go = new GameObject("Resplandor" + i);
            go.transform.SetParent(c.transform, false);
            go.transform.localPosition = new Vector3(0f, -halfH + glowOffsetY, 10f);
            go.transform.localScale = new Vector3(sx, sy, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orders[i];
            if (unlit != null) sr.sharedMaterial = unlit;
            glowLayers.Add(sr);
            glowBase.Add(alphas[i]);
            glowPhase.Add(i * 3.1f);
        }
    }

    private void Update()
    {
        for (int i = 0; i < glowLayers.Count; i++)
        {
            if (glowLayers[i] == null) continue;
            float f = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 0.9f + glowPhase[i], 0.3f)
                            + 0.1f * Mathf.Sin(Time.time * 2.3f + glowPhase[i]);
            var col = glowColor;
            col.a = glowAlpha * glowBase[i] * f;
            glowLayers[i].color = col;
        }
        if (globalLight != null)
        {
            float f = 1f + lightFlicker * (Mathf.PerlinNoise(Time.time * 1.7f, 7.7f) - 0.5f) * 2f;
            globalLight.intensity = lightIntensity * f;
        }
    }

    private static Texture2D GlowTexture()
    {
        const int w = 128, h = 64;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w, ny = y / (float)h; // ny=0 abajo
                float n = 0.75f + 0.25f * Mathf.PerlinNoise(nx * 5f + 3.1f, ny * 2f + 3.1f);
                float vert = Mathf.Pow(1f - ny, 2f);
                float edge = Mathf.Pow(Mathf.Sin(nx * Mathf.PI), 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, vert * edge * n));
            }
        tex.Apply();
        return tex;
    }
}
