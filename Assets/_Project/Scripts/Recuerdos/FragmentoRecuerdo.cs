using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Fragmento de luz que suelta un jefe: el recuerdo del accidente. Se ve como lo más valioso
// del círculo: cristal dorado que gira despacio, rayos, halos, chispas en órbita y una luz cálida
// que contrasta con el frío del Círculo IX. Al tocarlo el tiempo se frena, el cristal vuela
// hacia el jugador y se abre la pantalla del recuerdo (PantallaRecuerdo).
// No hace falta ponerlo en escenas: lo crea SoltarRecuerdo. Uso manual: FragmentoRecuerdo.Crear(datos, posicion);
public class FragmentoRecuerdo : MonoBehaviour
{
    public static readonly Color Oro = new Color(1f, 0.80f, 0.45f, 1f);
    public static readonly Color Blanco = new Color(1f, 0.97f, 0.90f, 1f);

    private const float AlturaSobreSuelo = 1.6f;
    private const int Chispas = 7;

    private DatosRecuerdo datos;
    private Vector3 origen;
    private Vector3 reposo;

    private Transform cuerpo;          // todo lo que flota
    private SpriteRenderer cristal, nucleo, haloGrande, haloMedio, rayosA, rayosB, onda, pilar;
    private SpriteRenderer[] chispas;
    private Light2D luz;
    private ParticleSystem particulas;
    private AudioSource fuente;
    private Collider2D zona;

    private int ordenBase;
    private float luzBase = 2.6f;
    private float cercania;  // 0..1, sube cuando el jugador se acerca
    private bool listo;
    private bool tomado;

    public static FragmentoRecuerdo Crear(DatosRecuerdo datos, Vector3 posicion)
    {
        var go = new GameObject("Recuerdo_" + datos.id);
        go.transform.position = new Vector3(posicion.x, posicion.y, 0f);
        var f = go.AddComponent<FragmentoRecuerdo>();
        f.datos = datos;
        f.origen = go.transform.position;
        return f;
    }

    private void Start()
    {
        reposo = BuscarReposo(origen);
        Construir();
        StartCoroutine(Aparecer());
    }

    // Flota a una altura fija sobre el suelo que haya debajo del jefe
    private static Vector3 BuscarReposo(Vector3 desde)
    {
        int suelo = LayerMask.GetMask("Ground", "capaSuelo");
        var hit = Physics2D.Raycast(desde, Vector2.down, 30f, suelo);
        return hit.collider != null ? new Vector3(desde.x, hit.point.y + AlturaSobreSuelo, 0f) : desde;
    }

    // ---------- Aparición ----------

    private IEnumerator Aparecer()
    {
        cuerpo.localScale = Vector3.zero;
        Alfa(pilar, 0f); Alfa(onda, 0f);
        luz.intensity = 0f;
        Sonar(SonidosMundo.Cargar("firefly_hint_glow"), 1f);

        // 1. Columna de luz que baja sobre donde cayó el jefe
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float k = Suave(t / 0.6f);
            Alfa(pilar, 0.75f * k);
            pilar.transform.localScale = new Vector3(1.4f * (0.4f + 0.6f * k), 16f, 1f);
            luz.intensity = 3f * k;
            yield return null;
        }

        // 2. El cristal nace con un destello y una onda
        Sonar(SonidosMundo.Cargar("checkpoint"), 0.55f);
        t = 0f;
        while (t < 1.1f)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / 1.1f);
            cuerpo.localScale = Vector3.one * Rebote(Mathf.Clamp01(t / 0.7f));
            onda.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 9f, Suave(k));
            Alfa(onda, 0.85f * (1f - k));
            Alfa(pilar, 0.75f * (1f - k));
            luz.intensity = Mathf.Lerp(8f, luzBase, Suave(k));
            luz.pointLightOuterRadius = Mathf.Lerp(12f, 7f, k);
            yield return null;
        }
        Alfa(pilar, 0f); Alfa(onda, 0f);
        particulas.Play();

        // 3. Baja despacio hasta quedar flotando
        t = 0f;
        float dur = Mathf.Clamp(Vector3.Distance(origen, reposo) * 0.35f, 0.6f, 2f);
        while (t < dur)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(origen, reposo, Suave(t / dur));
            yield return null;
        }
        transform.position = reposo;
        zona.enabled = true;
        listo = true;
    }

    // ---------- Reposo: gira, late, brilla ----------

    private void Update()
    {
        if (cuerpo == null) return;
        float tt = Time.time;

        var jugador = PlayerHealthCache();
        float objetivo = 0f;
        if (jugador != null && listo && !tomado)
        {
            float d = Vector2.Distance(jugador.transform.position, transform.position);
            objetivo = Mathf.InverseLerp(5f, 1.5f, d);
        }
        cercania = Mathf.MoveTowards(cercania, objetivo, Time.deltaTime * 2f);

        if (!tomado)
        {
            // Flota
            cuerpo.localPosition = new Vector3(0f, Mathf.Sin(tt * 1.6f) * 0.16f, 0f);

            // El cristal "gira" sobre su eje (se estrecha y ensancha) y destella al quedar de frente
            float giro = Mathf.Cos(tt * 1.3f);
            cristal.transform.localScale = new Vector3(0.55f + 0.45f * Mathf.Abs(giro), 1f, 1f);
            float destello = Mathf.Pow(Mathf.Abs(giro), 8f);
            cristal.color = Color.Lerp(Oro, Blanco, 0.35f + 0.65f * destello);
        }

        float pulso = 0.5f + 0.5f * Mathf.Sin(tt * 2.2f);
        float latido = 1f + 0.15f * cercania;

        rayosA.transform.localRotation = Quaternion.Euler(0f, 0f, tt * (9f + 25f * cercania));
        rayosB.transform.localRotation = Quaternion.Euler(0f, 0f, -tt * (6f + 18f * cercania));
        Alfa(rayosA, (0.30f + 0.12f * pulso + 0.2f * cercania));
        Alfa(rayosB, (0.20f + 0.10f * (1f - pulso) + 0.15f * cercania));
        rayosA.transform.localScale = Vector3.one * 4.6f * latido;
        rayosB.transform.localScale = Vector3.one * 3.4f * latido;

        haloGrande.transform.localScale = Vector3.one * (6.5f + 0.6f * pulso) * latido;
        Alfa(haloGrande, 0.28f + 0.08f * pulso + 0.12f * cercania);
        haloMedio.transform.localScale = Vector3.one * (2.6f + 0.25f * Mathf.Sin(tt * 3.1f));
        Alfa(haloMedio, 0.55f + 0.15f * pulso);
        nucleo.transform.localScale = Vector3.one * (0.75f + 0.2f * Mathf.Sin(tt * 5.3f));

        // Chispas en órbita inclinada: pasan por delante y por detrás del cristal
        for (int i = 0; i < chispas.Length; i++)
        {
            float a = tt * (0.9f + 0.08f * i) + i * Mathf.PI * 2f / chispas.Length;
            float r = 1.05f + 0.18f * Mathf.Sin(tt * 0.7f + i);
            float s = Mathf.Sin(a);
            chispas[i].transform.localPosition = new Vector3(Mathf.Cos(a) * r, s * r * 0.38f + 0.1f * Mathf.Sin(tt * 2f + i), 0f);
            chispas[i].sortingOrder = s > 0f ? ordenBase - 1 : ordenBase + 3;
            chispas[i].transform.localScale = Vector3.one * (0.32f + 0.12f * -s);
            Alfa(chispas[i], 0.65f + 0.35f * -s);
        }

        if (listo && !tomado)
        {
            luz.intensity = luzBase * (0.85f + 0.3f * pulso) + 1.5f * cercania;
            luz.pointLightOuterRadius = 7f + 1.5f * cercania;
        }
    }

    private PlayerHealth jugadorCache;
    private PlayerHealth PlayerHealthCache()
    {
        if (jugadorCache == null) jugadorCache = FindFirstObjectByType<PlayerHealth>();
        return jugadorCache;
    }

    // ---------- Recoger ----------

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (!listo || tomado) return;
        if (otro.GetComponentInParent<PlayerController>() == null) return;
        var vida = otro.GetComponentInParent<PlayerHealth>();
        if (vida != null && vida.IsDead) return;
        if (PantallaRecuerdo.Abierta || SceneTransition.IsBusy) return;
        StartCoroutine(Recoger(otro.GetComponentInParent<PlayerController>()));
    }

    private IEnumerator Recoger(PlayerController jugador)
    {
        tomado = true;
        zona.enabled = false;
        Recuerdos.Agregar(datos);

        // El jugador se queda quieto
        PausaJuego.InputJugador(false);
        var rb = jugador.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(0f, Mathf.Min(0f, rb.linearVelocity.y));

        SonidosMundo.Recoger();
        Sonar(SonidosMundo.Cargar("firefly_taken"), 0.8f);

        // El tiempo se frena y el cristal vuela hacia el pecho del jugador
        var col = jugador.GetComponentInChildren<Collider2D>();
        Vector3 inicio = transform.position;
        Vector3 escala0 = cuerpo.localScale;
        float t = 0f, dur = 1.1f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Suave(t / dur);
            Time.timeScale = Mathf.Lerp(1f, 0.15f, Mathf.Clamp01(t / 0.3f));
            Vector3 destino = col != null ? (Vector3)col.bounds.center : jugador.transform.position;
            transform.position = Vector3.Lerp(inicio, destino, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
            cuerpo.localScale = escala0 * Mathf.Lerp(1f, 1.7f, k);
            cristal.color = Color.Lerp(cristal.color, Blanco, k);
            luz.intensity = Mathf.Lerp(luzBase, 10f, k);
            luz.pointLightOuterRadius = Mathf.Lerp(7f, 14f, k);
            onda.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 7f, k);
            Alfa(onda, 0.7f * Mathf.Sin(k * Mathf.PI));
            yield return null;
        }

        // Destello y pantalla del recuerdo (el tiempo queda en 0 mientras se lee)
        PantallaRecuerdo.Mostrar(datos, () =>
        {
            PausaJuego.InputJugador(true);
        });
        particulas.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        cuerpo.gameObject.SetActive(false);
        t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            luz.intensity = Mathf.Lerp(10f, 0f, t / 0.6f);
            yield return null;
        }
        Destroy(gameObject, 3f);
    }

    // ---------- Construcción ----------

    private void Construir()
    {
        // Mismo sorting layer que el jugador, por delante del escenario
        var refSr = PlayerHealthCache() != null ? PlayerHealthCache().GetComponentInChildren<SpriteRenderer>() : null;
        int capa = refSr != null ? refSr.sortingLayerID : 0;
        ordenBase = (refSr != null ? refSr.sortingOrder : 0) + 10;

        var zonaCirculo = gameObject.AddComponent<CircleCollider2D>();
        zonaCirculo.isTrigger = true;
        zonaCirculo.radius = 0.9f;
        zonaCirculo.enabled = false;
        zona = zonaCirculo;

        fuente = gameObject.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;
        fuente.ignoreListenerPause = false;

        pilar = Capa("Pilar", transform, GlowHalo.GetSprite(), Oro, capa, ordenBase - 6);
        pilar.transform.localPosition = new Vector3(0f, 6f, 0f); // baja desde arriba
        onda = Capa("Onda", transform, SpriteAnillo(), Blanco, capa, ordenBase - 5);

        cuerpo = new GameObject("Cuerpo").transform;
        cuerpo.SetParent(transform, false);

        haloGrande = Capa("HaloGrande", cuerpo, GlowHalo.GetSprite(), Oro, capa, ordenBase - 4);
        rayosB = Capa("RayosB", cuerpo, SpriteRayos(9, 0.6f), Oro, capa, ordenBase - 3);
        rayosA = Capa("RayosA", cuerpo, SpriteRayos(12, 0f), Blanco, capa, ordenBase - 3);
        haloMedio = Capa("HaloMedio", cuerpo, GlowHalo.GetSprite(), Oro, capa, ordenBase - 2);
        cristal = Capa("Cristal", cuerpo, SpriteCristal(), Blanco, capa, ordenBase);
        nucleo = Capa("Nucleo", cuerpo, GlowHalo.GetSprite(), Color.white, capa, ordenBase + 1);
        nucleo.transform.localPosition = new Vector3(0f, 0.08f, 0f);

        chispas = new SpriteRenderer[Chispas];
        for (int i = 0; i < Chispas; i++)
            chispas[i] = Capa("Chispa", cuerpo, GlowHalo.GetSprite(), Blanco, capa, ordenBase + 3);

        var luzGo = new GameObject("Luz");
        luzGo.transform.SetParent(transform, false); // fuera del cuerpo: sigue encendida un momento tras recogerlo
        luz = luzGo.AddComponent<Light2D>();
        luz.lightType = Light2D.LightType.Point;
        luz.color = Oro;
        luz.pointLightInnerRadius = 0.5f;
        luz.pointLightOuterRadius = 7f;
        luz.falloffIntensity = 0.6f;

        particulas = CrearParticulas(cuerpo, capa, ordenBase + 2);
    }

    private static SpriteRenderer Capa(string nombre, Transform padre, Sprite sprite, Color color, int capa, int orden)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        var mat = MaterialSinLuz();
        if (mat != null) sr.sharedMaterial = mat;
        sr.sortingLayerID = capa;
        sr.sortingOrder = orden;
        return sr;
    }

    private static ParticleSystem CrearParticulas(Transform padre, int capa, int orden)
    {
        var go = new GameObject("Polvo de luz");
        go.transform.SetParent(padre, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.useUnscaledTime = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(Oro, Blanco);
        main.gravityModifier = -0.06f;
        main.maxParticles = 80;

        var emision = ps.emission;
        emision.rateOverTime = 16f;

        var forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Circle;
        forma.radius = 0.6f;

        var color = ps.colorOverLifetime;
        color.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        color.color = g;

        var ruido = ps.noise;
        ruido.enabled = true;
        ruido.strength = 0.25f;
        ruido.frequency = 0.6f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        var mat = MaterialSinLuz();
        if (mat != null)
        {
            mat.mainTexture = GlowHalo.GetSprite().texture;
            r.sharedMaterial = mat;
        }
        r.sortingLayerID = capa;
        r.sortingOrder = orden;
        return ps;
    }

    // ---------- Utilidades ----------

    private void Sonar(AudioClip clip, float volumen)
    {
        if (clip != null && fuente != null) fuente.PlayOneShot(clip, volumen);
    }

    private static void Alfa(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        var c = sr.color;
        c.a = Mathf.Clamp01(a);
        sr.color = c;
    }

    private static float Suave(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    // Crece pasándose un poco y vuelve (easeOutBack)
    private static float Rebote(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float y = x - 1f;
        return 1f + c3 * y * y * y + c1 * y * y;
    }

    // Sin luz: el recuerdo brilla igual aunque la escena esté oscura
    private static Material MaterialSinLuz() => GlowHalo.UnlitMaterial();

    // ---------- Sprites generados ----------

    private static Sprite spriteCristal, spriteAnillo;
    private static readonly System.Collections.Generic.Dictionary<int, Sprite> spritesRayos =
        new System.Collections.Generic.Dictionary<int, Sprite>();

    // Cristal alargado de dos facetas (punta larga arriba), con una arista brillante
    public static Sprite SpriteCristal()
    {
        if (spriteCristal != null) return spriteCristal;
        const int w = 96, h = 192;
        const float corte = 0.36f; // altura de la parte ancha
        var tex = NuevaTextura(w, h, "RecuerdoCristal");
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float v = (y + 0.5f) / h;
            float nx = ((x + 0.5f) / w) * 2f - 1f;
            float ny = v > corte ? (v - corte) / (1f - corte) : (corte - v) / corte;
            float ancho = 1f - ny;
            float borde = ancho - Mathf.Abs(nx);
            float a = Mathf.Clamp01(borde / 0.07f);
            a = a * a * (3f - 2f * a);

            // Faceta izquierda más oscura, derecha clara, arista central y borde brillante
            float brillo = nx < 0f ? 0.78f : 1f;
            float arista = Mathf.Exp(-Mathf.Pow((nx - 0.04f) / 0.06f, 2f));
            float filo = Mathf.Exp(-Mathf.Pow(borde / 0.05f, 2f)) * 0.5f;
            float centro = 1f - Mathf.Clamp01(Mathf.Sqrt(nx * nx + (ny * 0.8f) * (ny * 0.8f)));
            brillo = Mathf.Clamp01(brillo * (0.75f + 0.25f * centro) + arista * 0.35f + filo);
            byte b = (byte)(brillo * 255f);
            px[y * w + x] = new Color32(b, b, b, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        spriteCristal = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, corte), 160f); // 0.6 x 1.2 unidades
        return spriteCristal;
    }

    // Rayos finos que salen del centro (n rayos, desfase en fracción de rayo)
    public static Sprite SpriteRayos(int n, float desfase)
    {
        int clave = n * 100 + Mathf.RoundToInt(desfase * 10f);
        if (spritesRayos.TryGetValue(clave, out var s) && s != null) return s;
        const int t = 256;
        var tex = NuevaTextura(t, t, "RecuerdoRayos");
        var px = new Color32[t * t];
        float c = (t - 1) * 0.5f;
        for (int y = 0; y < t; y++)
        for (int x = 0; x < t; x++)
        {
            float dx = (x - c) / c, dy = (y - c) / c;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float ang = Mathf.Atan2(dy, dx);
            float onda = Mathf.Abs(Mathf.Cos((ang * n + desfase * Mathf.PI * 2f) * 0.5f));
            float rayo = Mathf.Pow(onda, 28f);
            // Rayos alternos más cortos
            float largo = (Mathf.FloorToInt((ang + Mathf.PI) / (Mathf.PI * 2f) * n + 0.5f) % 2 == 0) ? 1f : 0.65f;
            float caida = Mathf.Clamp01(1f - d / largo);
            float a = rayo * caida * caida * Mathf.Clamp01(d * 6f);
            px[y * t + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        s = Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), t); // 1 unidad
        spritesRayos[clave] = s;
        return s;
    }

    public static Sprite SpriteAnillo()
    {
        if (spriteAnillo != null) return spriteAnillo;
        const int t = 256;
        var tex = NuevaTextura(t, t, "RecuerdoAnillo");
        var px = new Color32[t * t];
        float c = (t - 1) * 0.5f;
        for (int y = 0; y < t; y++)
        for (int x = 0; x < t; x++)
        {
            float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
            float a = Mathf.Exp(-Mathf.Pow((d - 0.86f) / 0.06f, 2f)) + 0.15f * Mathf.Clamp01(1f - d) * Mathf.Clamp01(d - 0.3f);
            px[y * t + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        spriteAnillo = Sprite.Create(tex, new Rect(0, 0, t, t), new Vector2(0.5f, 0.5f), t);
        return spriteAnillo;
    }

    private static Texture2D NuevaTextura(int w, int h, string nombre) =>
        new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = nombre
        };
}
