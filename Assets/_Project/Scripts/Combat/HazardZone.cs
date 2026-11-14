using System.Collections;
using UnityEngine;

// Zona de peligro en el suelo: avisa (marca roja en el piso), luego salen picos de hielo que hacen dano.
// Se crea por codigo con HazardZone.Spawn(...), no necesita prefab.
// Si se le pasan sprites de pico (spikeSprites) usa esos; si no, dibuja unos picos de hielo por codigo.
public class HazardZone : MonoBehaviour
{
    private static Sprite whiteSprite;
    private static Sprite proceduralSpike;

    private int damage;
    private float warnTime, activeTime;
    private Color warnColor;
    private SpriteRenderer warnSr, spikeSr;
    private BoxCollider2D box;
    private Vector2 spikeBaseScale;
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

    // Tres picos de hielo dibujados por codigo (pivote abajo al centro)
    private static Sprite ProceduralSpike()
    {
        if (proceduralSpike != null) return proceduralSpike;

        const int W = 128, H = 192;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[W * H];

        // cx, mitad de la base, altura (0..1): primero los de atras, el central al final
        float[,] spikes = { { 0.2f, 0.2f, 0.62f }, { 0.8f, 0.2f, 0.74f }, { 0.5f, 0.27f, 1f } };
        Color baseCol = new Color(0.28f, 0.55f, 0.9f), tipCol = new Color(0.88f, 0.98f, 1f);

        for (int y = 0; y < H; y++)
        {
            float v = (float)y / (H - 1);
            for (int x = 0; x < W; x++)
            {
                float u = (float)x / (W - 1);
                Color pix = new Color(0, 0, 0, 0);
                for (int s = 0; s < 3; s++)
                {
                    float cx = spikes[s, 0], hb = spikes[s, 1], h = spikes[s, 2];
                    if (v > h) continue;
                    float halfW = hb * (1f - v / h);
                    float d = Mathf.Abs(u - cx);
                    float cov = Mathf.Clamp01((halfW - d) * W / 1.5f);
                    if (cov <= 0f) continue;

                    Color col = Color.Lerp(baseCol, tipCol, Mathf.Pow(v / h, 0.8f));
                    col = u < cx ? Color.Lerp(col, Color.white, 0.35f) : new Color(col.r * 0.8f, col.g * 0.85f, col.b * 0.95f);
                    if (halfW - d < 2.5f / W) col = Color.Lerp(col, Color.white, 0.5f);
                    col.a = 1f;

                    pix = Color.Lerp(pix, col, cov);
                    pix.a = Mathf.Max(pix.a, cov);
                }
                px[y * W + x] = pix;
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        proceduralSpike = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0f), 1f);
        return proceduralSpike;
    }

    // center = centro de la zona (x, suelo + alto/2). activeColor se conserva por compatibilidad (no se usa con picos).
    public static HazardZone Spawn(Vector2 center, Vector2 size, int damage, float warnTime, float activeTime,
                                   Color warnColor, Color activeColor, int sortingOrder, Sprite[] spikeSprites = null)
    {
        float groundY = center.y - size.y * 0.5f;

        var go = new GameObject("HazardZone");
        go.transform.position = new Vector3(center.x, groundY, 0f);

        var z = go.AddComponent<HazardZone>();
        z.damage = damage;
        z.warnTime = warnTime;
        z.activeTime = activeTime;
        z.warnColor = warnColor;

        // Marca de aviso plana en el suelo
        var warn = new GameObject("Warn");
        warn.transform.SetParent(go.transform, false);
        warn.transform.localPosition = new Vector3(0f, 0.15f, 0f);
        warn.transform.localScale = new Vector3(size.x, 0.3f, 1f);
        z.warnSr = warn.AddComponent<SpriteRenderer>();
        z.warnSr.sprite = WhiteSprite();
        z.warnSr.sortingOrder = sortingOrder;
        z.warnSr.color = warnColor;

        // Picos
        Sprite spr = (spikeSprites != null && spikeSprites.Length > 0)
            ? spikeSprites[Random.Range(0, spikeSprites.Length)]
            : ProceduralSpike();
        var spike = new GameObject("Spikes");
        spike.transform.SetParent(go.transform, false);
        z.spikeSr = spike.AddComponent<SpriteRenderer>();
        z.spikeSr.sprite = spr;
        z.spikeSr.sortingOrder = sortingOrder + 1;
        z.spikeSr.flipX = Random.value < 0.5f;
        Vector3 b = spr.bounds.size;
        z.spikeBaseScale = new Vector2(size.x / b.x, size.y / b.y);
        z.SetSpikeHeight(0f);

        z.box = go.AddComponent<BoxCollider2D>();
        z.box.isTrigger = true;
        z.box.size = size;
        z.box.offset = new Vector2(0f, size.y * 0.5f);
        z.box.enabled = false;

        z.StartCoroutine(z.Run());
        return z;
    }

    private void SetSpikeHeight(float k)
    {
        spikeSr.transform.localScale = new Vector3(spikeBaseScale.x, spikeBaseScale.y * Mathf.Max(0f, k), 1f);
    }

    private IEnumerator Run()
    {
        // Aviso: marca roja parpadeante y, al final, los picos asoman un poco temblando
        float t = 0f;
        float peekStart = warnTime * 0.6f;
        while (t < warnTime)
        {
            t += Time.deltaTime;
            float pulse = 0.6f + 0.4f * Mathf.Sin(t * 18f);
            Color c = warnColor;
            c.a = warnColor.a * pulse;
            warnSr.color = c;

            if (t > peekStart && warnTime > peekStart)
            {
                float k = (t - peekStart) / (warnTime - peekStart);
                SetSpikeHeight(0.1f * k);
                spikeSr.transform.localPosition = new Vector3(Random.Range(-0.04f, 0.04f), 0f, 0f);
            }
            yield return null;
        }
        spikeSr.transform.localPosition = Vector3.zero;

        // Salen los picos: daño activo
        box.enabled = true;
        SonidosJefe.Cristal();
        warnSr.enabled = false;
        float rise = Mathf.Min(0.1f, activeTime);
        t = 0f;
        while (t < rise)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / rise);
            SetSpikeHeight(Mathf.Lerp(0.1f, 1.08f, 1f - (1f - k) * (1f - k)));
            yield return null;
        }
        SetSpikeHeight(1f);
        yield return new WaitForSeconds(Mathf.Max(0f, activeTime - rise));

        // Se hunden y desaparecen
        box.enabled = false;
        const float sink = 0.2f;
        t = 0f;
        while (t < sink)
        {
            t += Time.deltaTime;
            float k = t / sink;
            SetSpikeHeight(1f - k);
            Color c = Color.white; c.a = 1f - k;
            spikeSr.color = c;
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
