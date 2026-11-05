using UnityEngine;

public class Luciernaga : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Transform jugador;

    [Header("Seguir")]
    [SerializeField] Vector2 offsetSeguir = new Vector2(0.7f, 0.8f);
    [SerializeField] float suavizado = 0.35f;

    [Header("Inquietud")]
    [SerializeField] float amplitudTemblor = 0.12f;
    [SerializeField] float velocidadTemblor = 3f;

    [Header("Merodear")]
    [SerializeField] float tiempoParaMerodear = 1.5f;
    [SerializeField] float radioOrbita = 1.2f;
    [SerializeField] float velocidadOrbita = 2f;
    [SerializeField] float umbralMovimiento = 0.1f;

    enum Estado { Seguir, Merodear }
    Estado estado = Estado.Seguir;

    Vector3 velocidadActual;
    Vector3 ultimaPosJugador;
    int lado = 1;
    float tiempoQuieto;
    float angulo;

    void Start()
    {
        ultimaPosJugador = jugador.position;
    }

    void LateUpdate()
    {
        ObservarJugador();

        Vector3 objetivo = estado == Estado.Seguir ? ObjetivoSeguir() : ObjetivoOrbita();
        objetivo += Temblor();

        transform.position = Vector3.SmoothDamp(
            transform.position, objetivo, ref velocidadActual, suavizado);
    }

    void ObservarJugador()
    {
        if (Time.deltaTime <= 0f) return;

        Vector3 delta = jugador.position - ultimaPosJugador;
        ultimaPosJugador = jugador.position;

        float rapidez = delta.magnitude / Time.deltaTime;

        if (Mathf.Abs(delta.x) > 0.001f)
        {
            lado = delta.x > 0 ? 1 : -1;
        }

        if (rapidez > umbralMovimiento)
        {
            tiempoQuieto = 0f;
            estado = Estado.Seguir;
        }
        else
        {
            tiempoQuieto += Time.deltaTime;

            if (estado == Estado.Seguir && tiempoQuieto >= tiempoParaMerodear)
            {
                IniciarMerodeo();
            }
        }
    }

    void IniciarMerodeo()
    {
        estado = Estado.Merodear;

        Vector3 aqui = transform.position - jugador.position;
        angulo = Mathf.Atan2(aqui.y, aqui.x);
    }

    Vector3 ObjetivoSeguir()
    {
        float x = Mathf.Abs(offsetSeguir.x) * -lado;
        return jugador.position + new Vector3(x, offsetSeguir.y, 0f);
    }

    Vector3 ObjetivoOrbita()
    {
        angulo += velocidadOrbita * Time.deltaTime;
        return jugador.position + new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0f) * radioOrbita;
    }

    Vector3 Temblor()
    {
        float x = Mathf.Sin(Time.time * velocidadTemblor);
        float y = Mathf.Cos(Time.time * velocidadTemblor * 1.3f);
        return new Vector3(x, y, 0f) * amplitudTemblor;
    }
}