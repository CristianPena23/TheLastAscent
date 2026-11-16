using System.Collections.Generic;
using UnityEngine;

// Hace parpadear los ojos de una capa de fondo (ej. 05_Ojos_en_la_oscuridad).
// Uso: agregalo al objeto de la capa (el que tiene el SpriteRenderer). Corta la imagen en una cuadricula
// y cada celda parpadea por su cuenta y en momentos distintos, asi los ojos no parpadean todos a la vez.
// El parallax (CapaParallax) sigue funcionando porque las celdas son hijas de la capa.
[RequireComponent(typeof(SpriteRenderer))]
public class OjosParpadeo : MonoBehaviour
{
    [Header("Cuadricula (mas celdas = ojos mas independientes)")]
    [SerializeField] private int columnas = 24;
    [SerializeField] private int filas = 4;

    [Header("Parpadeo")]
    [Tooltip("Segundos entre parpadeos (minimo, maximo)")]
    [SerializeField] private Vector2 intervalo = new Vector2(1.5f, 6f);
    [Tooltip("Duracion de un parpadeo en segundos")]
    [SerializeField] private float duracion = 0.18f;
    [Tooltip("Cuanto se apagan al parpadear (1 = se apagan del todo)")]
    [Range(0f, 1f)] [SerializeField] private float profundidad = 0.95f;
    [Tooltip("Variacion suave de brillo cuando no parpadean")]
    [Range(0f, 0.5f)] [SerializeField] private float latido = 0.12f;

    private readonly List<SpriteRenderer> celdas = new List<SpriteRenderer>();
    private readonly List<float> proximo = new List<float>();
    private readonly List<float> fase = new List<float>();

    private void Start()
    {
        var src = GetComponent<SpriteRenderer>();
        var spr = src.sprite;
        if (spr == null) return;

        Texture2D tex = spr.texture;
        Rect r = spr.rect;
        float ppu = spr.pixelsPerUnit;
        int cw = Mathf.CeilToInt(r.width / columnas);
        int ch = Mathf.CeilToInt(r.height / filas);

        for (int fy = 0; fy < filas; fy++)
            for (int fx = 0; fx < columnas; fx++)
            {
                float x = fx * cw, y = fy * ch;
                float w = Mathf.Min(cw, r.width - x), h = Mathf.Min(ch, r.height - y);
                if (w <= 0f || h <= 0f) continue;

                var piece = Sprite.Create(tex, new Rect(r.x + x, r.y + y, w, h), Vector2.zero, ppu, 0, SpriteMeshType.FullRect);
                var go = new GameObject("Celda_" + fx + "_" + fy);
                go.transform.SetParent(transform, false);
                // el sprite original tiene pivote abajo-izquierda: la celda va en su posicion dentro de la imagen
                go.transform.localPosition = new Vector3(x / ppu - spr.pivot.x / ppu, y / ppu - spr.pivot.y / ppu, 0f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = piece;
                sr.sharedMaterial = src.sharedMaterial;
                sr.sortingLayerID = src.sortingLayerID;
                sr.sortingOrder = src.sortingOrder;
                sr.color = src.color;

                celdas.Add(sr);
                proximo.Add(Time.time + Random.Range(0f, intervalo.y));
                fase.Add(Random.Range(0f, 10f));
            }

        src.enabled = false; // ahora la dibujan las celdas
    }

    private void Update()
    {
        float t = Time.time;
        for (int i = 0; i < celdas.Count; i++)
        {
            float a = 1f - latido * (0.5f + 0.5f * Mathf.Sin(t * 1.3f + fase[i]));

            float dt = t - proximo[i];
            if (dt >= 0f)
            {
                if (dt < duracion)
                {
                    // cierra y abre: forma de "V" en el brillo
                    float k = Mathf.Abs(dt / duracion * 2f - 1f);
                    a *= Mathf.Lerp(1f - profundidad, 1f, k);
                }
                else proximo[i] = t + Random.Range(intervalo.x, intervalo.y);
            }

            var c = celdas[i].color;
            c.a = a;
            celdas[i].color = c;
        }
    }
}
