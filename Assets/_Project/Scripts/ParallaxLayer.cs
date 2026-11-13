using UnityEngine;

// Efecto de profundidad. Ponlo en un objeto padre (ej. Capa_Frente) y mete los decorados como hijos.
// Cada hijo queda EXACTAMENTE donde lo pongas cuando la camara pasa por su posicion en X/Y;
// antes y despues de ese punto se mueve mas rapido (primer plano) o mas lento (fondo).
//  factor < 0 : primer plano (-0.05 suave, -0.15 normal, -0.35 muy rapido)
//  factor > 0 : fondo lejano (0.1 a 0.6)
//  factor = 0 : fijo en el mundo
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private float factorX = -0.15f;
    [SerializeField] private float factorY = -0.05f;
    [SerializeField] private Transform cam;

    private Transform[] targets;
    private Vector3[] startPos;

    private void Start()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;

        int n = transform.childCount;
        targets = new Transform[n > 0 ? n : 1];
        if (n > 0) for (int i = 0; i < n; i++) targets[i] = transform.GetChild(i);
        else targets[0] = transform;

        startPos = new Vector3[targets.Length];
        for (int i = 0; i < targets.Length; i++) startPos[i] = targets[i].position;
    }

    private void LateUpdate()
    {
        if (cam == null) return;
        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 s = startPos[i];
            targets[i].position = new Vector3(
                s.x + (cam.position.x - s.x) * factorX,
                s.y + (cam.position.y - s.y) * factorY,
                s.z);
        }
    }
}
