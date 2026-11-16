using UnityEngine;

// Bola de fuego que viaja en horizontal por la arena. Hace dano al jugador y desaparece al tocarlo o al salir de la arena.
// Se crea por codigo con FuegoBola.Spawn(...). Si le pasas "frames" usa la animacion de tu companera.
public class FuegoBola : MonoBehaviour
{
    private static Sprite headSprite;

    private float dirX;
    private float speed;
    private int damage;
    private float minX, maxX;
    private SpriteRenderer sr;
    private SpriteRenderer glow;
    private Sprite[] frames;
    private float fps;
    private float age;
    private bool hit;

    // Bola con cola (cabeza a la derecha, cola a la izquierda), dibujada por codigo
    private static Sprite ProceduralBall()
    {
        if (headSprite != null) return headSprite;
        const int W = 128, H = 64;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (float)x / (W - 1);          // 0 = cola, 1 = cabeza
                float v = ((float)y / (H - 1) - 0.5f) * 2f;
                // la cola se estrecha hacia la izquierda
                float halfH = Mathf.Lerp(0.05f, 0.9f, Mathf.Pow(u, 0.8f));
                // la cabeza es redonda
                float headR = 0.9f;
                float dx = (u - 0.75f) * (W / (float)H) * 0.5f;
                bool inHead = (dx * dx + v * v) < headR * headR * 0.6f && u > 0.5f;
                float cov = inHead ? 1f : Mathf.Clamp01((halfH - Mathf.Abs(v)) * 6f) * Mathf.Clamp01(u * 1.6f);
                if (u > 0.95f) cov *= Mathf.Clamp01((1f - u) * 20f);
                if (cov <= 0f) continue;

                float heat = Mathf.Clamp01(u * 1.1f - Mathf.Abs(v) * 0.6f);
                Color edge = new Color(0.85f, 0.15f, 0.03f);
                Color mid = new Color(1f, 0.5f, 0.05f);
                Color hot = new Color(1f, 0.92f, 0.5f);
                Color col = heat < 0.5f ? Color.Lerp(edge, mid, heat * 2f) : Color.Lerp(mid, hot, (heat - 0.5f) * 2f);
                col.a = cov * Mathf.Clamp01(u * 1.5f);
                px[y * W + x] = col;
            }
        tex.SetPixels(px);
        tex.Apply();
        headSprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 64f);
        return headSprite;
    }

    // pos = centro de la bola. dirX = +1 derecha, -1 izquierda. size = alto de la bola en unidades.
    public static FuegoBola Spawn(Vector2 pos, float dirX, float speed, float size, int damage,
                                  float minX, float maxX, int sortingOrder, Sprite[] frames = null, float fps = 12f)
    {
        var go = new GameObject("FuegoBola");
        go.transform.position = new Vector3(pos.x, pos.y, 0f);

        var b = go.AddComponent<FuegoBola>();
        b.dirX = dirX >= 0f ? 1f : -1f;
        b.speed = speed;
        b.damage = damage;
        b.minX = minX;
        b.maxX = maxX;
        b.frames = frames;
        b.fps = Mathf.Max(1f, fps);

        var unlit = GlowHalo.UnlitMaterial();

        var body = new GameObject("Cuerpo");
        body.transform.SetParent(go.transform, false);
        b.sr = body.AddComponent<SpriteRenderer>();
        Sprite spr = (frames != null && frames.Length > 0 && frames[0] != null) ? frames[0] : ProceduralBall();
        b.sr.sprite = spr;
        b.sr.sortingOrder = sortingOrder + 1;
        b.sr.flipX = b.dirX < 0f;
        if (unlit != null) b.sr.sharedMaterial = unlit;
        float scale = size / Mathf.Max(0.01f, spr.bounds.size.y);
        body.transform.localScale = new Vector3(scale, scale, 1f);

        var gl = new GameObject("Brillo");
        gl.transform.SetParent(go.transform, false);
        gl.transform.localScale = Vector3.one * size * 1.8f;
        b.glow = gl.AddComponent<SpriteRenderer>();
        b.glow.sprite = GlowHalo.GetSprite();
        b.glow.sortingOrder = sortingOrder;
        b.glow.color = new Color(1f, 0.45f, 0.1f, 0.5f);
        if (unlit != null) b.glow.sharedMaterial = unlit;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        bool conSprites = frames != null && frames.Length > 0 && frames[0] != null;
        col.radius = size * (conSprites ? 0.3f : 0.4f);
        col.offset = new Vector2(b.dirX * size * (conSprites ? 0.19f : 0.3f), 0f);
        return b;
    }

    private void Update()
    {
        age += Time.deltaTime;
        transform.position += new Vector3(dirX * speed * Time.deltaTime, 0f, 0f);

        if (frames != null && frames.Length > 1)
            sr.sprite = frames[Mathf.FloorToInt(age * fps) % frames.Length];

        if (glow != null)
        {
            float f = 0.85f + 0.15f * Mathf.Sin(age * 25f);
            glow.color = new Color(1f, 0.45f, 0.1f, 0.5f * f);
        }

        if (transform.position.x < minX - 2f || transform.position.x > maxX + 2f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hit) return;
        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        hit = true;
        Vector2 dir = new Vector2(dirX, 0.3f).normalized;
        health.TakeDamage(damage, dir);
        Destroy(gameObject);
    }
}
