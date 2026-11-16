using UnityEngine;

// Mueve el objeto suavemente de lado a lado (y un poquito arriba y abajo).
// Ponlo en un objeto HIJO del que tiene CapaParallax, porque CapaParallax mueve el objeto padre.
public class OscilacionSuave : MonoBehaviour
{
    [Tooltip("Cuanto se mueve (unidades) en X y en Y")]
    public Vector2 amplitud = new Vector2(1.8f, 0.06f);
    [Tooltip("Segundos que tarda en ir y volver, en X y en Y")]
    public Vector2 periodo = new Vector2(14f, 5f);

    private Vector3 basePos;

    private void Start() => basePos = transform.localPosition;

    private void Update()
    {
        float t = Time.time;
        float x = periodo.x > 0.01f ? Mathf.Sin(t * Mathf.PI * 2f / periodo.x) * amplitud.x : 0f;
        float y = periodo.y > 0.01f ? Mathf.Sin(t * Mathf.PI * 2f / periodo.y) * amplitud.y : 0f;
        transform.localPosition = basePos + new Vector3(x, y, 0f);
    }
}
