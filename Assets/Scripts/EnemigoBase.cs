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

    [Header("Detección")]
    [SerializeField] float rangoDeteccion = 7f;
    [SerializeField] float toleranciaVertical = 2.5f;

    Transform jugador;
    SpriteRenderer sprite;
    Color colorOriginal;

    int direccion = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject objJugador = GameObject.FindWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
        }

        sprite = GetComponent<SpriteRenderer>();
        colorOriginal = sprite.color;
    }
    void FixedUpdate()
    {
        
        if (!HaySueloAdelante() || HayParedAdelante())
        {
            Voltear();
        }

        rb.linearVelocity = new Vector2(direccion * velocidadPatrulla, rb.linearVelocity.y);
        sprite.color = PuedeVerAlJugador() ? Color.red : colorOriginal;
    }

    bool HaySueloAdelante()
    {
        return Physics2D.Raycast(sensorSuelo.position, Vector2.down, distanciaSuelo, capaSuelo);
    }

    bool HayParedAdelante()
    {
        return Physics2D.Raycast(sensorPared.position, Vector2.right * direccion, distanciaPared, capaSuelo);
    }

    bool PuedeVerAlJugador()
    {
        if (jugador == null) return false;

        Vector2 haciaJugador = jugador.position - transform.position;

        if (haciaJugador.magnitude > rangoDeteccion) return false;
        if (Mathf.Abs(haciaJugador.y) > toleranciaVertical) return false;

        bool estaAdelante = Mathf.Sign(haciaJugador.x) == direccion;
        if (!estaAdelante) return false;

        bool hayObstaculo = Physics2D.Linecast(transform.position, jugador.position, capaSuelo);
        return !hayObstaculo;
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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
    }
    
    
}
