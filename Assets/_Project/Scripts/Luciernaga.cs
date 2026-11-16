using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    [Header("Acciones del guion (diálogos)")]
    [SerializeField] float tiempoAparecer = 0.9f;
    [SerializeField] float escalaPulso = 1.6f;
    [SerializeField] float tiempoPulso = 0.45f;
    [Tooltip("Cuánto se adelanta al jugador para iluminar el camino")]
    [SerializeField] Vector2 offsetIluminar = new Vector2(2.8f, 1.2f);
    [SerializeField] float tiempoIluminar = 1.6f;

    [Header("Sonido")]
    [Tooltip("Aleteo en loop mientras se ve (sigue la transparencia: invisible = callada)")]
    [Range(0f, 1f)] [SerializeField] float volumenAleteo = 0.25f;

    enum Estado { Seguir, Merodear }
    Estado estado = Estado.Seguir;

    Vector3 velocidadActual;
    Vector3 ultimaPosJugador;
    int lado = 1;
    float tiempoQuieto;
    float angulo;
    SpriteRenderer sr;
    Light2D luz;
    float intensidadLuz;
    Vector3 escalaBase;
    float tiempoGuia;
    float direccionGuia = 1f;
    AudioSource aleteo;

    SpriteRenderer Sr => sr != null ? sr : (sr = GetComponent<SpriteRenderer>());

    void Awake()
    {
        escalaBase = transform.localScale;
        luz = GetComponentInChildren<Light2D>();
        if (luz != null) intensidadLuz = luz.intensity;

        aleteo = gameObject.AddComponent<AudioSource>();
        aleteo.clip = SonidosMundo.Cargar("firefly_flutter_loop");
        aleteo.loop = true;
        aleteo.playOnAwake = false;
        aleteo.spatialBlend = 0f;
        aleteo.volume = 0f;
        if (aleteo.clip != null) aleteo.Play();
    }

    void OnEnable()
    {
        DialogueManager.AlAccion += EjecutarAccion;
        DialogueManager.AlTerminar += Mostrar;
    }

    void OnDisable()
    {
        DialogueManager.AlAccion -= EjecutarAccion;
        DialogueManager.AlTerminar -= Mostrar;
    }

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        if (jugador == null)
            jugador = GameObject.FindWithTag("Player").transform;

        ultimaPosJugador = jugador.position;
    }

    void LateUpdate()
    {
        ObservarJugador();

        Vector3 objetivo = estado == Estado.Seguir ? ObjetivoSeguir() : ObjetivoOrbita();
        if (tiempoGuia > 0f)
        {
            tiempoGuia -= Time.unscaledDeltaTime;
            objetivo = jugador.position + new Vector3(offsetIluminar.x * direccionGuia, offsetIluminar.y, 0f);
        }
        objetivo += Temblor();

        transform.position = Vector3.SmoothDamp(
            transform.position, objetivo, ref velocidadActual, suavizado);

        if (aleteo != null && Sr != null) aleteo.volume = volumenAleteo * Sr.color.a;
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
            sr.flipX = lado < 0;
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

    // ---------- Acciones del guion ----------

    // Invisible hasta que el guion la haga aparecer (lo llama IntroCaida)
    public void Ocultar()
    {
        SetAlpha(0f);
        if (luz != null) luz.intensity = 0f;
    }

    // Si el diálogo se saltó antes de que apareciera, que no se quede invisible
    void Mostrar()
    {
        if (Sr == null || Sr.color.a >= 1f) return;
        StopAllCoroutines();
        SetAlpha(1f);
        transform.localScale = escalaBase;
        if (luz != null) luz.intensity = intensidadLuz;
    }

    void EjecutarAccion(AccionDialogo accion)
    {
        switch (accion)
        {
            case AccionDialogo.LuciernagaAparece:
                SonidosMundo.LuciernagaBrillo();
                StartCoroutine(Aparecer());
                break;
            case AccionDialogo.LuciernagaPulso: StartCoroutine(Pulso()); break;
            case AccionDialogo.LuciernagaIlumina:
                var pc = jugador != null ? jugador.GetComponent<PlayerController>() : null;
                direccionGuia = pc != null ? pc.FacingDirection : lado;
                tiempoGuia = tiempoIluminar;
                SonidosMundo.LuciernagaBrillo();
                StartCoroutine(Pulso());
                break;
        }
    }

    IEnumerator Aparecer()
    {
        float t = 0f;
        while (t < tiempoAparecer)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / tiempoAparecer);
            SetAlpha(k);
            if (luz != null) luz.intensity = intensidadLuz * k;
            yield return null;
        }
        SetAlpha(1f);
        if (luz != null) luz.intensity = intensidadLuz;
        yield return Pulso();
    }

    // Un pulso de luz: crece y brilla, y vuelve
    IEnumerator Pulso()
    {
        float t = 0f;
        while (t < tiempoPulso)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Sin(Mathf.Clamp01(t / tiempoPulso) * Mathf.PI); // 0 -> 1 -> 0
            transform.localScale = escalaBase * Mathf.Lerp(1f, escalaPulso, k);
            if (luz != null) luz.intensity = intensidadLuz * (1f + k * 1.5f);
            yield return null;
        }
        transform.localScale = escalaBase;
        if (luz != null) luz.intensity = intensidadLuz;
    }

    void SetAlpha(float a)
    {
        if (Sr == null) return;
        Color c = Sr.color;
        c.a = a;
        Sr.color = c;
    }
}
