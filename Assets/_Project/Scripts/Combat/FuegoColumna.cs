using System.Collections;
using UnityEngine;

// Columna de fuego que sale del suelo hacia arriba: avisa con una llama chica que crece y titila, luego sube, hace dano y se apaga.
// Se crea por codigo con FuegoColumna.Spawn(...), no necesita prefab.
// Si le pasas "frames" (sprites de la animacion de fuego de tu companera) los usa; si no, dibuja una llama por codigo.
public class FuegoColumna : MonoBehaviour
{
    private static Sprite whiteSprite;
    private static Sprite flameSprite;

    private int damage;
    private float warnTime, activeTime;
    private Color warnColor;
    private SpriteRenderer flameSr;
    private BoxCollider2D box;
    private Vector2 baseScale;
    private Sprite[] frames;
    private float fps;
    private bool hasHit;

    private static Sprite WhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }

    // Llama dibujada por codigo (pivote abajo al centro): amarillo-blanco al centro, naranja y rojo en los bordes
    public static Sprite ProceduralFlame()
    {
        if (flameSprite != null) return flameSprite;

        const int W = 128, H = 256;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[W * H];

        for (int y = 0; y < H; y++)
        {
            float v = (float)y / (H - 1);
            for (int x = 0; x < W; x++)
            {
                float u = (float)x / (W - 1) - 0.5f;
                // ancho de la llama: ancha abajo, puntiaguda arriba, con ondulacion
                float wave = Mathf.PerlinNoise(v * 4f + 2.3f, 1.7f) - 0.5f;
                float center = wave * 0.18f * v;
                float halfW = 0.46f * Mathf.Pow(1f - v, 0.7f) * (0.85f + 0.3f * Mathf.PerlinNoise(v * 6f, 5.1f));
                float d = Mathf.Abs(u - center);
                float cov = Mathf.Clamp01((halfW - d) * W / 2f);
                if (cov <= 0f) continue;

                float core = Mathf.Clamp01(1f - d / Mathf.Max(0.001f, halfW));      // 1 al centro, 0 en el borde
                float heat = Mathf.Clamp01(core * 1.2f - v * 0.5f);
                Color edge = new Color(0.85f, 0.15f, 0.03f);
                Color mid = new Color(1f, 0.5f, 0.05f);
                Color hot = new Color(1f, 0.92f, 0.5f);
                Color col = heat < 0.5f ? Color.Lerp(edge, mid, heat * 2f) : Color.Lerp(mid, hot, (heat - 0.5f) * 2f);
                col.a = cov * Mathf.Clamp01(1.4f - v);
                px[y * W + x] = col;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        flameSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 1f);
        return flameSprite;
    }

    // center = centro de la zona (x, suelo + alto/2).
    public static FuegoColumna Spawn(Vector2 center, Vector2 size, int damage, float warnTime, float activeTime,
                                     Color warnColor, int sortingOrder, Sprite[] frames = null, float fps = 12f)
    {
        float groundY = center.y - size.y * 0.5f;

        var go = new GameObject("FuegoColumna");
        go.transform.position = new Vector3(center.x, groundY, 0f);

        var z = go.AddComponent<FuegoColumna>();
        z.damage = damage;
        z.warnTime = warnTime;
        z.activeTime = activeTime;
        z.warnColor = warnColor;
        z.frames = frames;
        z.fps = Mathf.Max(1f, fps);

        var unlit = GlowHalo.UnlitMaterial();

        // Llama
        Sprite spr = (frames != null && frames.Length > 0 && frames[0] != null) ? frames[0] : ProceduralFlame();
        var fl = new GameObject("Llama");
        fl.transform.SetParent(go.transform, false);
        z.flameSr = fl.AddComponent<SpriteRenderer>();
        z.flameSr.sprite = spr;
        z.flameSr.sortingOrder = sortingOrder + 1;
        z.flameSr.flipX = Random.value < 0.5f;
        if (unlit != null) z.flameSr.sharedMaterial = unlit;
        Vector3 b = spr.bounds.size;
        z.baseScale = new Vector2(size.x / b.x, size.y / b.y);
        z.SetShape(0.4f, 0f);

        z.box = go.AddComponent<BoxCollider2D>();
        z.box.isTrigger = true;
        z.box.size = new Vector2(size.x * 0.8f, size.y);
        z.box.offset = new Vector2(0f, size.y * 0.5f);
        z.box.enabled = false;

        z.StartCoroutine(z.Run());
        return z;
    }

    private void SetHeight(float k)
    {
        flameSr.transform.localScale = new Vector3(baseScale.x, baseScale.y * Mathf.Max(0f, k), 1f);
    }

    // wk = ancho (1 = ancho final), hk = alto (1 = alto final)
    private void SetShape(float wk, float hk)
    {
        flameSr.transform.localScale = new Vector3(baseScale.x * wk, baseScale.y * Mathf.Max(0f, hk), 1f);
    }

    private void Animate(float t)
    {
        if (frames != null && frames.Length > 1)
            flameSr.sprite = frames[Mathf.FloorToInt(t * fps) % frames.Length];
    }

    private IEnumerator Run()
    {
        // Aviso: una llama chica asoma en el suelo, titila y va creciendo hasta que sale la columna
        const float startH = 0.12f, endH = 0.34f, startW = 0.4f, endW = 0.75f;
        float t = 0f;
        while (t < warnTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / Mathf.Max(0.01f, warnTime));
            float growth = k * k;
            float flick = 1f + 0.18f * Mathf.Sin(t * 38f) + 0.2f * (Mathf.PerlinNoise(t * 14f, 3.3f) - 0.5f);
            float wk = Mathf.Lerp(startW, endW, growth) * (1f + 0.08f * Mathf.Sin(t * 31f));
            SetShape(wk, Mathf.Lerp(startH, endH, growth) * flick);
            Color c = Color.white;
            c.a = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(k * 2f));
            flameSr.color = c;
            Animate(t * 1.5f);
            yield return null;
        }
        flameSr.color = Color.white;
        flameSr.transform.localPosition = Vector3.zero;

        // Sale la columna: dano activo
        box.enabled = true;
        float rise = Mathf.Min(0.15f, activeTime);
        t = 0f;
        while (t < rise)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / rise);
            float e = 1f - (1f - k) * (1f - k);
            SetShape(Mathf.Lerp(endW, 1f, e), Mathf.Lerp(endH, 1.05f, e));
            Animate(t);
            yield return null;
        }
        SetHeight(1f);
        float hold = Mathf.Max(0f, activeTime - rise);
        float h = 0f;
        while (h < hold)
        {
            h += Time.deltaTime;
            // la llama oscila un poco de ancho y alto
            float w = 1f + 0.06f * Mathf.Sin(h * 30f);
            flameSr.transform.localScale = new Vector3(baseScale.x * w, baseScale.y * (1f + 0.04f * Mathf.Sin(h * 22f)), 1f);
            Animate(rise + h);
            yield return null;
        }

        // Se apaga
        box.enabled = false;
        const float fade = 0.25f;
        t = 0f;
        while (t < fade)
        {
            t += Time.deltaTime;
            float k = t / fade;
            SetHeight(1f - 0.4f * k);
            Color c = Color.white; c.a = 1f - k;
            flameSr.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (hasHit || !box.enabled) return;
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        hasHit = true;
        Vector2 dir = (health.transform.position - transform.position).normalized;
        health.TakeDamage(damage, dir);
    }
}
