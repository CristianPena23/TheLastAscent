using System.Collections;
using UnityEngine;

// Ambiente de hielo: nieve en dos capas (lejos y cerca), viento y niebla baja.
// Uso: crea un objeto vacio "Ambiente" en la escena y agregale este componente. Todo se genera solo.
// Funciona en cualquier escena (Circulo 9, jefe, etc.). Guardalo como prefab para reutilizarlo.
public class AmbienteHielo : MonoBehaviour
{
    [Header("Camara (vacio = camara principal)")]
    [SerializeField] private Camera cam;

    [Header("Viento")]
    [Tooltip("Positivo = hacia la derecha, negativo = izquierda")]
    [SerializeField] private float wind = 0.7f;

    [Header("Nieve lejana (detras del jugador)")]
    [SerializeField] private bool farSnow = true;
    [SerializeField] private float farRate = 35f;
    [SerializeField] private Vector2 farSize = new Vector2(0.03f, 0.07f);
    [SerializeField] private float farFallSpeed = 0.35f;
    [SerializeField] private float farAlpha = 0.5f;
    [SerializeField] private int farOrder = -1;

    [Header("Nieve cercana (delante del jugador)")]
    [SerializeField] private bool nearSnow = true;
    [SerializeField] private float nearRate = 10f;
    [SerializeField] private Vector2 nearSize = new Vector2(0.09f, 0.2f);
    [SerializeField] private float nearFallSpeed = 0.9f;
    [SerializeField] private float nearAlpha = 0.45f;
    [SerializeField] private int nearOrder = 13;

    [Header("Niebla baja")]
    [SerializeField] private bool fog = true;
    [SerializeField] private Color fogColor = new Color(0.55f, 0.75f, 0.9f);
    [Range(0f, 1f)] [SerializeField] private float fogAlpha = 0.22f;
    [Tooltip("Que parte del alto de la pantalla ocupa (0.3 = el tercio de abajo)")]
    [Range(0.1f, 0.8f)] [SerializeField] private float fogHeight = 0.35f;
    [Tooltip("Mueve la niebla: negativo = mas abajo, positivo = mas arriba (en unidades)")]
    [SerializeField] private float fogOffsetY = 0f;
    [SerializeField] private int fogBackOrder = -1;
    [SerializeField] private int fogFrontOrder = 11;

    private Camera c;
    private float halfH, halfW;
    private readonly System.Collections.Generic.List<SpriteRenderer> fogLayers = new System.Collections.Generic.List<SpriteRenderer>();
    private readonly System.Collections.Generic.List<float> fogSpeed = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<float> fogPhase = new System.Collections.Generic.List<float>();
    private readonly System.Collections.Generic.List<float> fogBaseAlpha = new System.Collections.Generic.List<float>();

    private IEnumerator Start()
    {
        yield return null; // espera un frame para que la camara (Cinemachine) tome su tamano
        c = cam != null ? cam : Camera.main;
        if (c == null) yield break;

        halfH = c.orthographicSize;
        halfW = halfH * c.aspect;

        var sprite = GlowHalo.GetSprite();
        var mat = ParticleMaterial(sprite.texture);

        if (farSnow) MakeSnow("NieveLejos", mat, farOrder, farRate, farSize, farFallSpeed, farAlpha, 0.15f);
        if (nearSnow) MakeSnow("NieveCerca", mat, nearOrder, nearRate, nearSize, nearFallSpeed, nearAlpha, 0.35f);
        if (fog) MakeFog();
    }

    private static Material ParticleMaterial(Texture tex)
    {
        var sh = Shader.Find("Sprites/Default");
        var m = new Material(sh);
        m.mainTexture = tex;
        return m;
    }

    private void MakeSnow(string objName, Material mat, int order, float rate, Vector2 size, float fall, float alpha, float noise)
    {
        var go = new GameObject(objName);
        go.transform.SetParent(c.transform, false);
        go.transform.localPosition = new Vector3(0f, halfH + 1.5f, 10f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        float travel = halfH * 2f + 3f;
        float life = travel / Mathf.Max(0.05f, fall);

        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.9f, life * 1.1f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(size.x, size.y);
        main.startColor = new Color(0.9f, 0.96f, 1f, alpha);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Mathf.CeilToInt(rate * life * 1.3f) + 20;
        main.gravityModifier = 0f;

        var em = ps.emission;
        em.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(halfW * 2f + Mathf.Abs(wind) * life * 0.6f + 6f, 0.5f, 1f);
        // desplaza el origen contra el viento para que la nieve cubra toda la pantalla
        shape.position = new Vector3(-Mathf.Sign(wind) * Mathf.Abs(wind) * life * 0.25f, 0f, 0f);

        var vel = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;
        vel.x = wind * (fall > 0.5f ? 1.3f : 1f);
        vel.y = -fall;
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
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, 0.9f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = mat;
        r.sortingOrder = order;

        ps.Play();
    }

    private void MakeFog()
    {
        var tex = CloudTexture();
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        var unlit = GlowHalo.UnlitMaterial();

        float bandH = halfH * 2f * fogHeight;
        float width = halfW * 2f * 1.7f;
        // la niebla se extiende por debajo de la pantalla para que no se vea el borde inferior
        float extra = bandH * 0.45f;
        float totalH = bandH + extra;
        float sx = width / (tex.width / 100f);
        float sy = totalH / (tex.height / 100f);

        // 4 capas: 2 detras del jugador, 2 delante (mas tenues)
        int[] orders = { fogBackOrder, fogBackOrder, fogFrontOrder, fogFrontOrder };
        float[] alphas = { 1f, 0.8f, 0.55f, 0.4f };
        float[] speeds = { 0.05f, 0.08f, 0.07f, 0.11f };
        float[] yOff = { 0f, 0.1f, -0.05f, 0.05f };

        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject("Niebla" + i);
            go.transform.SetParent(c.transform, false);
            go.transform.localPosition = new Vector3(0f, -halfH - extra + totalH * 0.5f + yOff[i] * halfH * 0.5f + fogOffsetY, 10f);
            go.transform.localScale = new Vector3(sx * (i % 2 == 0 ? 1f : -1f), sy, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = orders[i];
            if (unlit != null) sr.sharedMaterial = unlit;

            fogLayers.Add(sr);
            fogSpeed.Add(speeds[i]);
            fogPhase.Add(i * 1.7f);
            fogBaseAlpha.Add(alphas[i]);
        }
    }

    private void Update()
    {
        for (int i = 0; i < fogLayers.Count; i++)
        {
            var sr = fogLayers[i];
            if (sr == null) continue;
            float t = Time.time * fogSpeed[i] + fogPhase[i];
            var p = sr.transform.localPosition;
            p.x = Mathf.Sin(t) * halfW * 0.3f;
            sr.transform.localPosition = p;
            var col = fogColor;
            col.a = fogAlpha * fogBaseAlpha[i] * (0.85f + 0.15f * Mathf.Sin(t * 1.7f));
            sr.color = col;
        }
    }

    private static Texture2D CloudTexture()
    {
        const int w = 256, h = 128;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float seed = 17.3f;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = x / (float)w, ny = y / (float)h; // ny=0 abajo
                float n = Mathf.PerlinNoise(nx * 3.2f + seed, ny * 2f + seed) * 0.65f
                        + Mathf.PerlinNoise(nx * 7f + seed, ny * 4f + seed) * 0.35f;
                // zona de abajo solida (queda fuera de pantalla) y luego se desvanece hacia arriba
                float vert = ny < 0.3f ? 1f : Mathf.Pow(1f - (ny - 0.3f) / 0.7f, 1.4f);
                float edge = Mathf.Pow(Mathf.Sin(nx * Mathf.PI), 0.6f);
                float a = Mathf.Clamp01(n * 1.5f - 0.2f) * vert * edge;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }
}
