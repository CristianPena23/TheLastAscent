using System.Collections;
using UnityEngine;

// Voz y cuerpo del personaje: pasos, salto, aterrizaje, ataque, golpe a enemigos, dash, parry, daño,
// curación, latido con poca vida, muerte, reaparición y el poder especial.
// PlayerController lo agrega solo al jugador (no hay que ponerlo en el prefab). Los clips se cargan
// de Resources/Audio/Jugador y Resources/Audio/Historia. Casi todo se detecta observando el estado
// del jugador; salto, ataque y parry avisan con eventos (AlSaltar, AlAtacar, OnParrySuccess/OnParryFail).
// Los sonidos se pausan con el juego (AudioListener.pause) y respetan el volumen de Opciones.
[RequireComponent(typeof(PlayerController), typeof(Rigidbody2D))]
public class SonidosJugador : MonoBehaviour
{
    const string Carpeta = "Audio/Jugador/";

    [Header("Volumen por tipo")]
    [Range(0f, 1f)] [SerializeField] float volPasos = 0.55f;
    [Range(0f, 1f)] [SerializeField] float volSalto = 0.7f;
    [Range(0f, 1f)] [SerializeField] float volAterrizaje = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volAtaque = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volDash = 0.75f;
    [Range(0f, 1f)] [SerializeField] float volDanio = 0.9f;
    [Range(0f, 1f)] [SerializeField] float volMuerte = 0.9f;
    [Range(0f, 1f)] [SerializeField] float volEspecial = 0.85f;
    [Range(0f, 1f)] [SerializeField] float volGolpeEnemigo = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volParryExito = 0.9f;
    [Range(0f, 1f)] [SerializeField] float volParryFallo = 0.6f;
    [Range(0f, 1f)] [SerializeField] float volCurar = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volReaparecer = 0.7f;
    [Range(0f, 1f)] [SerializeField] float volLatido = 0.6f;

    [Header("Vida baja")]
    [Tooltip("Con esta vida o menos suena el latido")]
    [SerializeField] int vidaBaja = 1;

    [Header("Pasos")]
    [Tooltip("Velocidad horizontal mínima para que suenen pasos")]
    [SerializeField] float velocidadMinPasos = 1.5f;
    [Tooltip("Segundos entre pasos a velocidad máxima (8)")]
    [SerializeField] float intervaloPasos = 0.3f;

    [Header("Aterrizaje")]
    [Tooltip("Velocidad de caída mínima para que suene (evita sonar en escalones)")]
    [SerializeField] float caidaMinAterrizaje = 4f;
    [Tooltip("Caída a la que el aterrizaje suena a todo volumen")]
    [SerializeField] float caidaMaxAterrizaje = 20f;

    [Header("Poder especial")]
    [Tooltip("Cuándo entra el loop tras el arranque (el arranque crece y se apaga hacia los 2.5 s)")]
    [SerializeField] float entradaLoop = 1.2f;
    [SerializeField] float fadeLoop = 1f;

    [Header("Variación")]
    [SerializeField] Vector2 rangoTono = new Vector2(0.93f, 1.07f);

    AudioClip[] pasos, saltos, ataques, dashes, danios;
    AudioClip aterrizaje, muerte, especialInicio, especialLoop, especialFin;
    AudioClip[] golpesEnemigo;
    AudioClip parryExito, parryFallo, curar, reaparecer, latido;

    // Varias fuentes para que cada sonido tenga su propio tono sin pisarse
    AudioSource[] fuentes;
    int siguienteFuente;
    AudioSource fuenteMuerte, fuenteLoop, fuenteEspecial, fuenteLatido;
    readonly int[] ultimo = new int[6];

    PlayerController jugador;
    PlayerAttack ataque;
    PlayerDash dash;
    PlayerHealth salud;
    PoderInmunidad poder;
    PlayerParry parry;
    Rigidbody2D rb;

    bool estabaEnSuelo = true;
    float caidaMaxima;
    float temporizadorPasos;
    bool estabaDasheando, estabaMuerto, poderActivo;
    int vidaPrevia;
    Coroutine rutinaLoop, rutinaMuerte;

    void Awake()
    {
        jugador = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        ataque = GetComponent<PlayerAttack>();
        dash = GetComponent<PlayerDash>();
        salud = GetComponent<PlayerHealth>();
        poder = GetComponent<PoderInmunidad>();
        parry = GetComponent<PlayerParry>();

        pasos = CargarVarios("player_step_0", 6);
        saltos = CargarVarios("player_jump_0", 2);
        ataques = CargarVarios("player_attack_0", 3);
        dashes = CargarVarios("player_dash_0", 2);
        danios = CargarVarios("player_damage_0", 2);
        aterrizaje = Cargar("player_land");
        muerte = Cargar("player_death");
        especialInicio = Cargar("special_start");
        especialLoop = Cargar("special_loop");
        especialFin = Cargar("special_end");

        golpesEnemigo = new[] { SonidosMundo.Cargar("hit_enemy_01"), SonidosMundo.Cargar("hit_enemy_02"), SonidosMundo.Cargar("hit_enemy_03") };
        parryExito = SonidosMundo.Cargar("player_parry_success");
        parryFallo = SonidosMundo.Cargar("player_parry_fail");
        curar = SonidosMundo.Cargar("player_heal");
        reaparecer = SonidosMundo.Cargar("respawn");
        latido = SonidosMundo.Cargar("low_health_heartbeat_loop");

        fuentes = new AudioSource[4];
        for (int i = 0; i < fuentes.Length; i++) fuentes[i] = CrearFuente();
        fuenteMuerte = CrearFuente();
        fuenteLoop = CrearFuente();
        fuenteEspecial = CrearFuente();
        fuenteLatido = CrearFuente();
        fuenteLatido.loop = true;
        fuenteLatido.clip = latido;
        fuenteLatido.volume = 0f;
        fuenteLoop.loop = true;
        fuenteLoop.clip = especialLoop;
    }

    void OnEnable()
    {
        jugador.AlSaltar += AlSaltar;
        if (ataque != null) ataque.AlAtacar += AlAtacar;
        if (parry != null) { parry.OnParrySuccess += AlParryExito; parry.OnParryFail += AlParryFallo; }
        if (salud != null)
        {
            salud.OnHealthChanged += AlCambiarVida;
            salud.OnDied += AlMorir;
            vidaPrevia = salud.CurrentHealth;
        }
    }

    void OnDisable()
    {
        jugador.AlSaltar -= AlSaltar;
        if (ataque != null) ataque.AlAtacar -= AlAtacar;
        if (parry != null) { parry.OnParrySuccess -= AlParryExito; parry.OnParryFail -= AlParryFallo; }
        if (salud != null)
        {
            salud.OnHealthChanged -= AlCambiarVida;
            salud.OnDied -= AlMorir;
        }
        fuenteLoop.Stop();
        fuenteLatido.Stop();
        fuenteLatido.volume = 0f;
        poderActivo = false;
    }

    AudioSource CrearFuente()
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.spatialBlend = 0f; // 2D: la cámara sigue al jugador, no hace falta posicional
        return f;
    }

    static AudioClip Cargar(string nombre)
    {
        var clip = Resources.Load<AudioClip>(Carpeta + nombre);
        if (clip == null) Debug.LogWarning($"SonidosJugador: falta Resources/{Carpeta}{nombre}");
        return clip;
    }

    static AudioClip[] CargarVarios(string prefijo, int cantidad)
    {
        var clips = new AudioClip[cantidad];
        for (int i = 0; i < cantidad; i++) clips[i] = Cargar(prefijo + (i + 1));
        return clips;
    }

    // ---------------- Observación del estado ----------------

    void Update()
    {
        bool muerto = salud != null && salud.IsDead;
        if (estabaMuerto && !muerto) AlReaparecer();
        estabaMuerto = muerto;
        ObservarLatido(muerto);
        if (muerto) return;

        ObservarSueloYPasos();
        ObservarDash();
        ObservarPoder();
    }

    void ObservarSueloYPasos()
    {
        bool enSuelo = jugador.IsGrounded;
        float vy = rb.linearVelocity.y;

        if (!enSuelo) caidaMaxima = Mathf.Max(caidaMaxima, -vy);

        // Aterrizaje
        if (enSuelo && !estabaEnSuelo)
        {
            if (caidaMaxima >= caidaMinAterrizaje && Time.timeSinceLevelLoad > 0.2f)
            {
                float k = Mathf.InverseLerp(caidaMinAterrizaje, caidaMaxAterrizaje, caidaMaxima);
                Sonar(aterrizaje, volAterrizaje * Mathf.Lerp(0.55f, 1f, k));
                temporizadorPasos = intervaloPasos; // que el primer paso no se pise con el aterrizaje
            }
            caidaMaxima = 0f;
        }
        estabaEnSuelo = enSuelo;

        // Pasos: más rápidos cuanto más rápido corre
        float vx = Mathf.Abs(rb.linearVelocity.x);
        bool caminando = enSuelo && vx > velocidadMinPasos && !(dash != null && dash.IsDashing);
        if (!caminando)
        {
            temporizadorPasos = Mathf.Min(temporizadorPasos, 0.08f); // al arrancar, el primer paso sale enseguida
            return;
        }

        temporizadorPasos -= Time.deltaTime;
        if (temporizadorPasos <= 0f)
        {
            Sonar(AlAzar(pasos, 0), volPasos);
            temporizadorPasos = intervaloPasos * Mathf.Clamp(8f / vx, 1f, 1.6f);
        }
    }

    // Latido con poca vida: entra y sale con fade; se calla al morir
    void ObservarLatido(bool muerto)
    {
        if (latido == null || salud == null) return;
        bool bajo = !muerto && salud.CurrentHealth > 0 && salud.CurrentHealth <= vidaBaja;
        float objetivo = bajo ? volLatido : 0f;
        float velocidad = muerto ? 4f : 1f; // al morir se corta rápido
        fuenteLatido.volume = Mathf.MoveTowards(fuenteLatido.volume, objetivo, velocidad * Time.unscaledDeltaTime);
        if (bajo && !fuenteLatido.isPlaying) fuenteLatido.Play();
        else if (!bajo && fuenteLatido.isPlaying && fuenteLatido.volume <= 0f) fuenteLatido.Stop();
    }

    void ObservarDash()
    {
        bool dasheando = dash != null && dash.IsDashing;
        if (dasheando && !estabaDasheando) Sonar(AlAzar(dashes, 1), volDash);
        estabaDasheando = dasheando;
    }

    void ObservarPoder()
    {
        if (poder == null) poder = GetComponent<PoderInmunidad>(); // PlayerEnergy lo agrega en tiempo de ejecución
        bool activo = poder != null && poder.Activa;
        if (activo && !poderActivo) IniciarEspecial();
        else if (!activo && poderActivo) TerminarEspecial();
        poderActivo = activo;
    }

    // ---------------- Eventos ----------------

    void AlSaltar() => Sonar(AlAzar(saltos, 2), volSalto);

    void AlAtacar(bool acerto)
    {
        Sonar(AlAzar(ataques, 3), volAtaque);
        if (acerto) Sonar(AlAzar(golpesEnemigo, 5), volGolpeEnemigo);
    }

    void AlParryExito() => Sonar(parryExito, volParryExito);
    void AlParryFallo() => Sonar(parryFallo, volParryFallo);

    void AlCambiarVida(int actual, int maxima)
    {
        if (actual < vidaPrevia && actual > 0) Sonar(AlAzar(danios, 4), volDanio);
        // Curación (checkpoint...). Al reaparecer también sube la vida, pero ahí suena 'respawn'
        else if (actual > vidaPrevia && vidaPrevia > 0 && !salud.IsDead) Sonar(curar, volCurar);
        vidaPrevia = actual;
    }

    void AlMorir()
    {
        if (poderActivo) TerminarEspecial();
        poderActivo = false;
        if (rutinaMuerte != null) StopCoroutine(rutinaMuerte);
        fuenteMuerte.volume = volMuerte;
        fuenteMuerte.pitch = 1f;
        fuenteMuerte.clip = muerte;
        if (muerte != null) fuenteMuerte.Play();
    }

    // La muerte dura ~5 s pero el jugador reaparece antes: la cola se apaga suave
    void AlReaparecer()
    {
        if (fuenteMuerte.isPlaying) rutinaMuerte = StartCoroutine(Apagar(fuenteMuerte, 0.6f));
        if (reaparecer != null) fuenteEspecial.PlayOneShot(reaparecer, volReaparecer);
        caidaMaxima = 0f;
        estabaEnSuelo = true;
    }

    // ---------------- Poder especial ----------------

    void IniciarEspecial()
    {
        if (especialInicio != null) fuenteEspecial.PlayOneShot(especialInicio, volEspecial);
        if (rutinaLoop != null) StopCoroutine(rutinaLoop);
        rutinaLoop = StartCoroutine(EntrarLoop());
    }

    IEnumerator EntrarLoop()
    {
        yield return new WaitForSeconds(entradaLoop);
        if (especialLoop == null) yield break;
        fuenteLoop.volume = 0f;
        fuenteLoop.Play();
        float t = 0f;
        while (t < fadeLoop)
        {
            t += Time.deltaTime;
            fuenteLoop.volume = Mathf.Lerp(0f, volEspecial, t / fadeLoop);
            yield return null;
        }
        fuenteLoop.volume = volEspecial;
        rutinaLoop = null;
    }

    void TerminarEspecial()
    {
        if (rutinaLoop != null) StopCoroutine(rutinaLoop);
        rutinaLoop = fuenteLoop.isPlaying ? StartCoroutine(Apagar(fuenteLoop, 0.25f)) : null;
        if (especialFin != null) fuenteEspecial.PlayOneShot(especialFin, volEspecial);
    }

    IEnumerator Apagar(AudioSource fuente, float tiempo)
    {
        float inicio = fuente.volume;
        float t = 0f;
        while (t < tiempo)
        {
            t += Time.unscaledDeltaTime;
            fuente.volume = Mathf.Lerp(inicio, 0f, t / tiempo);
            yield return null;
        }
        fuente.Stop();
        fuente.volume = inicio;
    }

    // ---------------- Utilidades ----------------

    void Sonar(AudioClip clip, float volumen)
    {
        if (clip == null) return;
        var f = fuentes[siguienteFuente];
        siguienteFuente = (siguienteFuente + 1) % fuentes.Length;
        f.pitch = Random.Range(rangoTono.x, rangoTono.y);
        f.PlayOneShot(clip, volumen);
    }

    // Uno al azar sin repetir el anterior de esa categoría
    AudioClip AlAzar(AudioClip[] clips, int categoria)
    {
        if (clips == null || clips.Length == 0) return null;
        int i = Random.Range(0, clips.Length);
        if (clips.Length > 1 && i == ultimo[categoria]) i = (i + 1) % clips.Length;
        ultimo[categoria] = i;
        return clips[i];
    }
}
