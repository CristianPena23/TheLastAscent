using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EnemigoBase : MonoBehaviour
{
    [SerializeField] float velocidadPatrulla = 1.5f;
    [SerializeField] float velocidadPersecucion = 2.5f;
    
    Rigidbody2D rb;

    [Header("Sensores")]
    [SerializeField] Transform sensorSuelo;
    [SerializeField] Transform sensorPared;
    [SerializeField] LayerMask capaSuelo;
    [SerializeField] float distanciaSuelo = 0.5f;
    [SerializeField] float distanciaPared = 0.2f;

    [Header("Detección")]
    [SerializeField] float rangoDeteccion = 6f;
    [SerializeField] float rangoPerdida = 9f;
    [SerializeField] float tiempoDescanso = 2f;

    float temporizadorDescanso;
    bool persiguiendo;

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
        ActualizarPersecucion();

        if (persiguiendo)
        {
            Perseguir();
        }
        else
        {
            Patrullar();
        }

        sprite.color = persiguiendo ? Color.red : colorOriginal;
    }
    void ActualizarPersecucion()
    {
        if (jugador == null)
        {
            persiguiendo = false;
            return;
        }

        if (temporizadorDescanso > 0f)
        {
            temporizadorDescanso -= Time.fixedDeltaTime;
            return;
        }

        if (!persiguiendo && PuedeVerAlJugador())
        {
            persiguiendo = true;
        }
        else if (persiguiendo && DistanciaAlJugador() > rangoPerdida)
        {
            persiguiendo = false;
        }
    }
    void Patrullar()
    {
        if (!HaySueloAdelante() || HayParedAdelante())
        {
            Voltear();
        }

        Mover(velocidadPatrulla);
    }
    void Perseguir()
    {
        float diferenciaX = jugador.position.x - transform.position.x;
        MirarHacia(diferenciaX);

        if (!HaySueloAdelante() || HayParedAdelante())
        {
            Rendirse();
            return;
        }

        bool estaEncima = Mathf.Abs(diferenciaX) < 0.3f;
        Mover(estaEncima ? 0f : velocidadPersecucion);
    }
    void MirarHacia(float diferenciaX)
    {
        if (Mathf.Abs(diferenciaX) < 0.1f) return;

        int direccionDeseada = diferenciaX > 0 ? 1 : -1;
        if (direccionDeseada != direccion)
        {
            Voltear();
        }
    }
    void Mover(float velocidad)
    {
        rb.linearVelocity = new Vector2(direccion * velocidad, rb.linearVelocity.y);
    }
    bool HaySueloAdelante()
    {
        return Physics2D.Raycast(sensorSuelo.position, Vector2.down, distanciaSuelo, capaSuelo);
    }
    void Rendirse()
    {
        persiguiendo = false;
        temporizadorDescanso = tiempoDescanso;
        Voltear();
    }
    
    bool HayParedAdelante()
    {
        return Physics2D.Raycast(sensorPared.position, Vector2.right * direccion, distanciaPared, capaSuelo);
    }

    bool PuedeVerAlJugador()
    {
        if (jugador == null) return false;
        if (DistanciaAlJugador() > rangoDeteccion) return false;

        bool hayObstaculo = Physics2D.Linecast(transform.position, jugador.position, capaSuelo);
        return !hayObstaculo;
    }

    float DistanciaAlJugador()
    {
        return Vector2.Distance(transform.position, jugador.position);
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

        Gizmos.color = new Color(1f, 0.5f, 0f);   // naranja
        Gizmos.DrawWireSphere(transform.position, rangoPerdida);
    }
    
    
}
