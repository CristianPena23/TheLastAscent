using UnityEngine;

// Texto corto en el mundo que sube y se desvanece. Uso: FloatingText.Spawn("Llave obtenida", posicion, color);
public class FloatingText : MonoBehaviour
{
    private TextMesh tm;
    private float life = 1.6f;
    private float t;
    private Color baseColor;

    public static void Spawn(string text, Vector3 position, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = position;
        var ft = go.AddComponent<FloatingText>();
        ft.Init(text, color);
    }

    private void Init(string text, Color color)
    {
        baseColor = color;
        tm = gameObject.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 64;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
        {
            tm.font = font;
            GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }
        GetComponent<MeshRenderer>().sortingOrder = 100;
    }

    private void Update()
    {
        t += Time.deltaTime;
        transform.position += Vector3.up * (0.6f * Time.deltaTime);
        float a = 1f - Mathf.Clamp01((t - life * 0.5f) / (life * 0.5f));
        tm.color = new Color(baseColor.r, baseColor.g, baseColor.b, a);
        if (t >= life) Destroy(gameObject);
    }
}
