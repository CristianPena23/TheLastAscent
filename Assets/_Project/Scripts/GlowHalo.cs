using UnityEngine;

// Halo suave (degradado radial) que se coloca detras de un sprite para que parezca que emite luz.
public static class GlowHalo
{
    private static Sprite cached;

    public static Sprite GetSprite()
    {
        if (cached != null) return cached;
        const int s = 128;
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        float c = (s - 1) * 0.5f;
        for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * a; // cae rapido: centro fuerte, borde suave
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        cached = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), s);
        return cached;
    }

    public static Material UnlitMaterial()
    {
        var sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        return sh != null ? new Material(sh) : null;
    }

    // worldSize = diametro del halo en unidades del mundo
    public static SpriteRenderer Create(Transform parent, Color color, float worldSize, SpriteRenderer reference)
    {
        var go = new GameObject("Halo");
        go.transform.SetParent(parent, false);
        float parentScale = Mathf.Max(0.0001f, parent.lossyScale.x);
        go.transform.localScale = Vector3.one * (worldSize / parentScale);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSprite();
        sr.color = color;
        var mat = UnlitMaterial();
        if (mat != null) sr.sharedMaterial = mat;
        if (reference != null)
        {
            sr.sortingLayerID = reference.sortingLayerID;
            sr.sortingOrder = reference.sortingOrder - 1;
        }
        return sr;
    }
}
