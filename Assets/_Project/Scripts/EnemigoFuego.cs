using System.Collections;
using UnityEngine;

// Dragoncito volador que escupe fuego a corta distancia.
// Patrulla flotando cerca de su nido; al ver al jugador vuela hasta quedar a su lado (a la
// distancia del fuego), se prepara (abre la boca) y lanza una llamarada corta.
// El fuego no se puede hacer parry (EnemyAttackHitbox tipo Ranged): hay que esquivarlo o alejarse.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemigoFuego : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] int vida = 3;
    [SerializeField] Color colorGolpe = new Color(1f, 0.5f, 0.5f);
    [SerializeField] float tiempoParpadeo = 0.1f;
    [SerializeField] float empujeAlRecibirGolpe = 4f;
    [SerializeField] float tiempoAturdido = 0.35f;

    [Header("Patrulla (alrededor del nido)")]
    [SerializeField] float radioPatrulla = 1.5f;
    [SerializeField] float velocidadPatrulla = 0.8f;
    [SerializeField] float amplitudFlote = 0.25f;
    [SerializeField] float velocidadFlote = 2.5f;

    [Header("Detección")]
    [SerializeField] float rangoDeteccion = 6f;
    [Tooltip("Si el jugador se aleja más que esto del nido, el dragón vuelve a casa")]
    [SerializeField] float rangoPerdida = 9f;
    [Tooltip("Suelo y paredes: bloquean la vista")]
    [SerializeField] LayerMask capaSuelo = (1 << 3) | (1 << 6);

    [Header("Acercamiento")]
    [SerializeField] float velocidadVuelo = 3f;
    [Tooltip("Distancia horizontal al jugador desde la que escupe fuego")]
    [SerializeField] float distanciaAtaque = 2f;
    [SerializeField] float alturaSobreJugador = 0.4f;
    [SerializeField] float toleranciaPosicion = 0.35f;

    [Header("Fuego")]
    [SerializeField] GameObject hitboxFuego;
    [Tooltip("Boca abierta antes de escupir: el aviso para el jugador")]
    [SerializeField] float tiempoPreparacion = 0.5f;
    [SerializeField] float duracionFuego = 0.6f;
    [SerializeField] float tiempoRecuperacion = 0.4f;
    [SerializeField] float cooldown = 1.6f;

    [Header("Animación (cuadros de la hoja)")]
    [SerializeField] Sprite[] framesVuelo;
    [SerializeField] Sprite[] framesPreparar;
    [SerializeField] Sprite[] framesFuego;
    [SerializeField] Sprite[] framesRecuperar;
    [SerializeField] float fps = 14f;

    [Header("Sonido (Resources/Audio/Enemigos)")]
    [Range(0f, 1f)] [SerializeField] float volRespiracion = 0.5f;
    [Range(0f, 1f)] [SerializeField] float volRugido = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volResoplido = 0.55f;
    [Range(0f, 1f)] [SerializeField] float volInhalar = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volFuego = 0.9f;
    [Range(0f, 1f)] [SerializeField] float volDolor = 0.85f;
    [Range(0f, 1f)] [SerializeField] float volMuerte = 0.9f;
    [Tooltip("Segundos mínimos entre rugidos al ver al jugador")]
    [SerializeField] float esperaEntreRugidos = 6f;
    [Tooltip("Cada cuánto resopla mientras patrulla (al azar entre estos valores)")]
    [SerializeField] Vector2 intervaloResoplido = new Vector2(4f, 9f);

    enum Estado { Patrulla, Persecucion, Preparacion, Fuego, Recuperacion, Aturdido, Regreso, Muerto }
    Estado estado = Estado.Patrulla;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Transform jugador;
    Vector2 nido;
    float temporizador;
    float tiempoUltimoFuego = -999f;
    float fase;
    float escalaX;
    Sprite[] animActual;
    float tiempoAnim;
    bool animEnBucle;
    Coroutine parpadeo;

    VozEnemigo voz;
    AudioClip sRespiracion, sRugido, sResoplido, sInhalar, sFuego, sFuegoLoop, sDolor, sMuerte;
    float ultimoRugido = -999f;
    float proximoResoplido;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        sr = GetComponentInChildren<SpriteRenderer>();
        escalaX = Mathf.Abs(transform.localScale.x);
        if (hitboxFuego != null) hitboxFuego.SetActive(false);

        voz = GetComponent<VozEnemigo>();
        if (voz == null) voz = gameObject.AddComponent<VozEnemigo>();
        sRespiracion = VozEnemigo.Cargar("dragon_idle_loop");
        sRugido = VozEnemigo.Cargar("dragon_alert_roar");
        sResoplido = VozEnemigo.Cargar("dragon_snort");
        sInhalar = VozEnemigo.Cargar("dragon_inhale_telegraph");
        sFuego = VozEnemigo.Cargar("dragon_fire_breath");
        sFuegoLoop = VozEnemigo.Cargar("dragon_fire_loop");
        sDolor = VozEnemigo.Cargar("dragon_hurt");
        sMuerte = VozEnemigo.Cargar("dragon_death");
    }

    void Start()
    {
        nido = transform.position;
        var objJugador = GameObject.FindWithTag("Player");
        if (objJugador != null) jugador = objJugador.transform;
        else
        {
            var salud = FindFirstObjectByType<PlayerHealth>();
            if (salud != null) jugador = salud.transform;
        }
        Animar(framesVuelo, true);
        voz.Fondo(sRespiracion, volRespiracion);
        proximoResoplido = Time.time + Random.Range(intervaloResoplido.x, intervaloResoplido.y);
    }

    void Update()
    {
        ActualizarAnimacion();

        // Resopla de vez en cuando mientras está tranquilo
        if ((estado == Estado.Patrulla || estado == Estado.Regreso) && Time.time >= proximoResoplido)
        {
            voz.Sonar(sResoplido, volResoplido, 0.08f);
            proximoResoplido = Time.time + Random.Range(intervaloResoplido.x, intervaloResoplido.y);
        }
    }

    // Ruge al descubrir al jugador (no cada vez que lo vuelve a ver)
    void Alertar()
    {
        if (Time.time - ultimoRugido < esperaEntreRugidos) return;
        ultimoRugido = Time.time;
        voz.Sonar(sRugido, volRugido, 0.04f);
    }

    void FixedUpdate()
    {
        switch (estado)
        {
            case Estado.Patrulla:
                Patrullar();
                if (VeAlJugador()) { estado = Estado.Persecucion; Alertar(); }
                break;

            case Estado.Persecucion:
                Perseguir();
                break;

            case Estado.Preparacion:
                rb.linearVelocity = Vector2.zero;
                MirarAlJugador();
                if ((temporizador -= Time.fixedDeltaTime) <= 0f) IniciarFuego();
                break;

            case Estado.Fuego:
                rb.linearVelocity = Vector2.zero;
                // Si el fuego dura más que la llamarada, el loop la sostiene
                if (duracionFuego - temporizador > 1.2f && !voz.CanalSonando(sFuegoLoop))
                    voz.Canal(sFuegoLoop, volFuego * 0.85f, 0f, true);
                if ((temporizador -= Time.fixedDeltaTime) <= 0f) TerminarFuego();
                break;

            case Estado.Recuperacion:
                rb.linearVelocity = Vector2.zero;
                if ((temporizador -= Time.fixedDeltaTime) <= 0f) Volver(Estado.Persecucion);
                break;

            case Estado.Aturdido:
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, 8f * Time.fixedDeltaTime);
                if ((temporizador -= Time.fixedDeltaTime) <= 0f) Volver(Estado.Persecucion);
                break;

            case Estado.Regreso:
                Regresar();
                break;
        }
    }

    // ---------------- Detección ----------------

    bool VeAlJugador()
    {
        if (jugador == null) return false;
        if (Vector2.Distance(nido, jugador.position) > rangoDeteccion) return false;
        return !HayObstaculo();
    }

    // Paredes o suelo entre el dragón y el jugador. Ignora al propio jugador
    // (por si está en una capa de suelo, como PlayerPrueba en MecanicasEnemigos).
    bool HayObstaculo()
    {
        foreach (var golpe in Physics2D.LinecastAll(transform.position, jugador.position, capaSuelo))
        {
            if (golpe.transform == jugador || golpe.transform.IsChildOf(jugador)) continue;
            if (golpe.transform == transform || golpe.transform.IsChildOf(transform)) continue;
            return true;
        }
        return false;
    }

    bool JugadorFueraDeTerritorio() =>
        jugador == null || Vector2.Distance(nido, jugador.position) > rangoPerdida;

    // ---------------- Movimiento ----------------

    void Patrullar()
    {
        fase += Time.fixedDeltaTime;
        float x = Mathf.Sin(fase * velocidadPatrulla) * radioPatrulla;
        float y = Mathf.Sin(fase * velocidadFlote) * amplitudFlote;
        Vector2 objetivo = nido + new Vector2(x, y);
        Vector2 paso = objetivo - rb.position;
        rb.linearVelocity = paso / Time.fixedDeltaTime * 0.2f;
        Mirar(Mathf.Cos(fase * velocidadPatrulla)); // mira hacia donde avanza
    }

    void Perseguir()
    {
        if (JugadorFueraDeTerritorio()) { estado = Estado.Regreso; return; }

        // Se queda del lado en que ya está, a la distancia justa para que el fuego alcance
        float lado = Mathf.Sign(rb.position.x - jugador.position.x);
        if (lado == 0f) lado = 1f;
        float flote = Mathf.Sin(Time.time * velocidadFlote) * amplitudFlote * 0.5f;
        Vector2 objetivo = (Vector2)jugador.position + new Vector2(lado * distanciaAtaque, alturaSobreJugador + flote);

        Vector2 hacia = objetivo - rb.position;
        rb.linearVelocity = hacia.magnitude > 0.05f ? Vector2.ClampMagnitude(hacia * 4f, velocidadVuelo) : Vector2.zero;
        MirarAlJugador();

        bool enPosicion = hacia.magnitude <= toleranciaPosicion;
        bool listo = Time.time - tiempoUltimoFuego >= cooldown;
        if (enPosicion && listo && VeAlJugador()) IniciarPreparacion();
    }

    void Regresar()
    {
        if (VeAlJugador()) { estado = Estado.Persecucion; Alertar(); return; }

        Vector2 hacia = nido - rb.position;
        if (hacia.magnitude < 0.15f)
        {
            rb.linearVelocity = Vector2.zero;
            fase = 0f;
            estado = Estado.Patrulla;
            return;
        }
        rb.linearVelocity = hacia.normalized * velocidadVuelo * 0.7f;
        Mirar(hacia.x);
    }

    void MirarAlJugador()
    {
        if (jugador != null) Mirar(jugador.position.x - rb.position.x);
    }

    // El sprite mira a la derecha; volteamos toda la escala para que el fuego también gire
    void Mirar(float direccionX)
    {
        if (Mathf.Abs(direccionX) < 0.01f) return;
        var s = transform.localScale;
        s.x = escalaX * Mathf.Sign(direccionX);
        transform.localScale = s;
    }

    // ---------------- Ataque ----------------

    void IniciarPreparacion()
    {
        estado = Estado.Preparacion;
        temporizador = tiempoPreparacion;
        rb.linearVelocity = Vector2.zero;
        Animar(framesPreparar, false);
        // La inhalación crece hasta el final del clip: arranca a mitad para que el pico caiga justo al escupir
        if (sInhalar != null) voz.Canal(sInhalar, volInhalar, sInhalar.length - tiempoPreparacion);
    }

    void IniciarFuego()
    {
        estado = Estado.Fuego;
        temporizador = duracionFuego;
        Animar(framesFuego, false);
        if (hitboxFuego != null) hitboxFuego.SetActive(true);
        voz.Canal(sFuego, volFuego);
    }

    void TerminarFuego()
    {
        if (hitboxFuego != null) hitboxFuego.SetActive(false);
        tiempoUltimoFuego = Time.time;
        voz.ApagarCanal(0.4f);
        estado = Estado.Recuperacion;
        temporizador = tiempoRecuperacion;
        Animar(framesRecuperar, false);
    }

    void Volver(Estado nuevo)
    {
        estado = nuevo;
        Animar(framesVuelo, true);
    }

    // ---------------- Daño ----------------

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (estado == Estado.Muerto || amount <= 0) return;

        vida -= amount;
        if (parpadeo != null) StopCoroutine(parpadeo);
        parpadeo = StartCoroutine(Parpadear());

        if (vida <= 0) { StartCoroutine(Morir()); return; }

        voz.Sonar(sDolor, volDolor);
        // Un golpe interrumpe el fuego
        if (hitboxFuego != null) hitboxFuego.SetActive(false);
        voz.ApagarCanal(0.15f);
        if (estado == Estado.Preparacion || estado == Estado.Fuego) tiempoUltimoFuego = Time.time;
        estado = Estado.Aturdido;
        temporizador = tiempoAturdido;
        rb.linearVelocity = hitDirection.normalized * empujeAlRecibirGolpe;
        Animar(framesVuelo, true);
    }

    IEnumerator Parpadear()
    {
        if (sr == null) yield break;
        sr.color = colorGolpe;
        yield return new WaitForSeconds(tiempoParpadeo);
        sr.color = Color.white;
        parpadeo = null;
    }

    IEnumerator Morir()
    {
        estado = Estado.Muerto;
        SonidosMundo.EnemigoMuerto();
        voz.ApagarFondo(0.3f);
        voz.ApagarCanal(0.1f);
        voz.SonarDesprendido(sMuerte, volMuerte);
        if (hitboxFuego != null) hitboxFuego.SetActive(false);
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        rb.gravityScale = 1.5f; // cae
        rb.freezeRotation = false;
        rb.angularVelocity = 180f * Mathf.Sign(transform.localScale.x);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ---------------- Animación por cuadros ----------------

    void Animar(Sprite[] cuadros, bool bucle)
    {
        if (cuadros == null || cuadros.Length == 0) return;
        animActual = cuadros;
        animEnBucle = bucle;
        tiempoAnim = 0f;
        if (sr != null) sr.sprite = cuadros[0];
    }

    void ActualizarAnimacion()
    {
        if (sr == null || animActual == null || animActual.Length == 0) return;
        tiempoAnim += Time.deltaTime;
        int i = Mathf.FloorToInt(tiempoAnim * fps);
        i = animEnBucle ? i % animActual.Length : Mathf.Min(i, animActual.Length - 1);
        sr.sprite = animActual[i];
    }

    // ---------------- Gizmos ----------------

    void OnDrawGizmosSelected()
    {
        Vector2 centro = Application.isPlaying ? nido : (Vector2)transform.position;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centro, rangoDeteccion);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(centro, rangoPerdida);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(centro + Vector2.left * radioPatrulla, centro + Vector2.right * radioPatrulla);

        // Alcance del fuego
        if (hitboxFuego != null && hitboxFuego.TryGetComponent(out BoxCollider2D caja))
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
            Gizmos.matrix = hitboxFuego.transform.localToWorldMatrix;
            Gizmos.DrawCube(caja.offset, caja.size);
        }
    }
}
