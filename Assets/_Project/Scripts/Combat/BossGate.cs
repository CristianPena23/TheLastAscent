using System.Collections;
using UnityEngine;

// Reja de salida de la arena del jefe. Cerrada mientras el jefe vive; al morir sube y deja pasar.
// Uso:
//  - Pon este script en un objeto vacio en la salida (lado derecho).
//  - "Bars" = objeto con el sprite de la reja (hijo). Es el que sube al abrirse.
//  - "Blocker" = Collider2D SOLIDO que cierra el paso. Se desactiva al abrir.
//  - "Exit Door" = objeto con SceneDoor (trigger) que lleva a la siguiente escena. Empieza desactivado
//    y se activa cuando la reja termina de abrirse.
public class BossGate : MonoBehaviour
{
    [SerializeField] private BossHealth boss;
    [SerializeField] private Transform bars;
    [SerializeField] private Collider2D blocker;
    [SerializeField] private GameObject exitDoor;

    [Header("Apertura")]
    [SerializeField] private float openDelay = 1.5f;
    [SerializeField] private float openTime = 2f;
    [Tooltip("Cuanto sube la reja (unidades)")]
    [SerializeField] private float raiseHeight = 4f;
    [SerializeField] private float shake = 0.04f;

    private Vector3 closedPos;
    private bool opened;

    private void Awake()
    {
        if (bars != null) closedPos = bars.localPosition;
        if (blocker != null) blocker.enabled = true;
        if (exitDoor != null) exitDoor.SetActive(false);
    }

    private void Start()
    {
        if (boss == null) boss = FindFirstObjectByType<BossHealth>();
        if (boss != null) boss.Died += Open;
    }

    private void OnDestroy()
    {
        if (boss != null) boss.Died -= Open;
    }

    public void Open()
    {
        if (opened) return;
        opened = true;
        StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        yield return new WaitForSeconds(openDelay);
        float t = 0f;
        while (t < openTime)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / openTime);
            float e = k * k * (3f - 2f * k);
            if (bars != null)
            {
                Vector3 p = closedPos + Vector3.up * (raiseHeight * e);
                p.x += Random.Range(-shake, shake) * (1f - k);
                bars.localPosition = p;
            }
            yield return null;
        }
        if (bars != null) bars.localPosition = closedPos + Vector3.up * raiseHeight;
        if (blocker != null) blocker.enabled = false;
        if (exitDoor != null) exitDoor.SetActive(true);
    }
}
