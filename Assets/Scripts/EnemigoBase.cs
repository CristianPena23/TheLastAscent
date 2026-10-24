using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemigoBase : MonoBehaviour
{
    [SerializeField] float velocidadPatrulla = 1.5f;
    Rigidbody2D rb;
    [Header("Sensores")]
    [SerializeField] Transform sensorSuelo;
    [SerializeField] Transform sensorPared;
    [SerializeField] LayerMask capaSuelo;
    [SerializeField] float distanciaSuelo = 0.5f;
    [SerializeField] float distanciaPared = 0.2f;

    int direccion = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void FixedUpdate()
    {
        bool HaySueloAdelante()
        {
            return Physics2D.Raycast(sensorSuelo.position, Vector2.down, distanciaSuelo, capaSuelo);
        }

        bool HayParedAdelante()
        {
            return Physics2D.Raycast(sensorPared.position, Vector2.right * direccion, distanciaPared, capaSuelo);
        }
        if (!HaySueloAdelante() || HayParedAdelante())
        {
            Voltear();
        }

        rb.linearVelocity = new Vector2(direccion * velocidadPatrulla, rb.linearVelocity.y);
    }
    [ContextMenu("Voltear")]
    void Voltear()
    {
        direccion = -direccion;

        Vector3 scale = transform.localScale;
        scale.x = -scale.x;
        transform.localScale = scale;
    }
    void OnDrawGizmos()
    {
        if (sensorSuelo != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(sensorSuelo.position, sensorSuelo.position + Vector3.down * distanciaSuelo);
        }

        if (sensorPared != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(sensorPared.position, sensorPared.position + Vector3.right * direccion * distanciaPared);
        }
    }
    
    
}
