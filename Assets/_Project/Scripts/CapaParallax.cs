using UnityEngine;

// Pon este script en cada capa (o en el objeto padre de sus trozos).
// factor = 1   -> se mueve igual que el suelo (capa jugable)
// factor < 1   -> fondo, se mueve mas lento (0 = pegado a la camara)
// factor > 1   -> frente, se mueve mas rapido que el suelo
//
// NUEVO en el circulo 8: "alinearEnCentro". Si esta activo, la capa queda
// exactamente como en el PSD cuando la camara esta en el centro del nivel
// (48, 10). Asi el remolino del fondo siempre se ve, este donde este Felipe.
// Si lo apagas, funciona igual que en el nivel del circulo 9.
public class CapaParallax : MonoBehaviour
{
    [Range(0f, 1.5f)] public float factor = 0.5f;
    public bool tambienVertical = true;
    public bool alinearEnCentro = true;
    public Vector2 centroDelNivel = new Vector2(48f, 10f);
    public Transform camara;

    Vector3 posInicial;
    Vector3 camReferencia;

    void Start()
    {
        if (camara == null) camara = Camera.main.transform;
        posInicial = transform.position;
        camReferencia = alinearEnCentro
            ? new Vector3(centroDelNivel.x, centroDelNivel.y, camara.position.z)
            : camara.position;
    }

    void LateUpdate()
    {
        Vector3 d = camara.position - camReferencia;
        float k = 1f - factor;
        transform.position = new Vector3(
            posInicial.x + d.x * k,
            posInicial.y + (tambienVertical ? d.y * k : 0f),
            posInicial.z);
    }
}
