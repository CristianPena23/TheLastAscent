using System.Collections;
using UnityEngine;

// Enemigo de suelo: patrulla, persigue al jugador y, al tenerlo cerca, ataca cuerpo a cuerpo.
// El golpe se puede hacer parry (EnemyAttackHitbox tipo Basic): si lo bloquean queda aturdido.
// Las animaciones son opcionales (arreglos de sprites); sin ellas se comporta como antes.
public class EnemigoBase : MonoBehaviour, IDamageable, IParryable
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
    [Tooltip("Pintar de rojo al perseguir (útil con el cuadrado de prueba, no con sprites)")]
    [SerializeField] bool tenirAlPerseguir = true;

    [Header("Ataque cuerpo a cuerpo")]
    [Tooltip("Hijo con BoxCollider2D + EnemyAttackHitbox (desactivado)")]
    [SerializeField] GameObject hitboxAtaque;
    [SerializeField] float distanciaAtaque = 1.6f;
    [SerializeField] float alturaAtaque = 1.5f;
    [Tooltip("Aviso antes del golpe (se encoge y carga)")]
    [SerializeField] float tiempoCarga = 0.5f;
    [SerializeField] float duracionGolpe = 0.35f;
    [SerializeField] float tiempoRecuperacion = 0.4f;
    [SerializeField] float cooldownAtaque = 1.2f;
    [SerializeField] float tiempoAturdidoParry = 1.2f;

    [Header("Vida")]
    [SerializeField] int vida = 3;
    [SerializeField] Color colorGolpe = new Color(1f, 0.5f, 0.5f);
    [SerializeField] Color colorAturdido = new Color(0.6f, 0.9f, 1f);
    [SerializeField] float empujeAlRecibirGolpe = 4f;

    [Header("Animación (opcional)")]
    [SerializeField] Sprite[] framesMover;
    [SerializeField] Sprite[] framesCarga;
    [SerializeField] Sprite[] framesGolpe;
    [SerializeField] Sprite[] framesRecuperar;
    [SerializeField] float fpsMover = 14f;

    [Header("Sonido (Resources/Audio/Enemigos; por defecto, el alma)")]
    [SerializeField] string sonidoFondo = "soul_idle_loop";
    [SerializeField] string sonidoAlerta = "soul_alert";
    [SerializeField] string sonidoAtaque = "soul_attack";
    [SerializeField] string sonidoDolor = "soul_hurt";
    [SerializeField] string sonidoMuerte = "soul_death";
    [Range(0f, 1f)] [SerializeField] float volFondo = 0.55f;
    [Range(0f, 1f)] [SerializeField] float volEfectos = 0.85f;
    [Tooltip("Segundos mínimos entre alertas al ver al jugador")]
    [SerializeField] float esperaEntreAlertas = 6f;

    float temporizadorDescanso;
    bool persiguiendo;

    Transform jugador;
    SpriteRenderer sprite;
    Color colorOriginal;

    int direccion = 1;

    bool atacando;
    bool muerto;
    float aturdido;
    float ultimoAtaque = -999f;
    Coroutine rutinaAtaque;
    Coroutine rutinaColor;

    Sprite[] animActual;
    float tiempoAnim;
    float duracionAnim; // <= 0: bucle a fpsMover
    bool animBucle;

    VozEnemigo voz;
    AudioClip sFondo, sAlerta, sAtaque, sDolor, sMuerte;
    float ultimaAlerta = -999f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject objJugador = GameObject.FindWithTag("Player");
        if (objJugador != null)
        {
            jugador = objJugador.transform;
        }
        else
        {
            var salud = FindFirstObjectByType<PlayerHealth>();
            if (salud != null) jugador = salud.transform;
        }

        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        colorOriginal = sprite.color;
        if (hitboxAtaque != null) hitboxAtaque.SetActive(false);
        Animar(framesMover, 0f, true);

        voz = GetComponent<VozEnemigo>();
        if (voz == null) voz = gameObject.AddComponent<VozEnemigo>();
        sFondo = CargarSonido(sonidoFondo);
        sAlerta = CargarSonido(sonidoAlerta);
        sAtaque = CargarSonido(sonidoAtaque);
        sDolor = CargarSonido(sonidoDolor);
        sMuerte = CargarSonido(sonidoMuerte);
        voz.Fondo(sFondo, volFondo);
    }

    static AudioClip CargarSonido(string nombre) => string.IsNullOrEmpty(nombre) ? null : VozEnemigo.Cargar(nombre);

    void Update() => ActualizarAnimacion();

    void FixedUpdate()
    {
        if (muerto) return;

        if (aturdido > 0f)
        {
            aturdido -= Time.fixedDeltaTime;
            Mover(0f);
            if (aturdido <= 0f) PonerColor(colorOriginal);
            return;
        }

        if (atacando) { Mover(0f); return; }

        ActualizarPersecucion();

        if (persiguiendo)
        {
            if (PuedeAtacar()) IniciarAtaque();
            else Perseguir();
        }
        else
        {
            Patrullar();
        }

        if (tenirAlPerseguir && rutinaColor == null)
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
            if (Time.time - ultimaAlerta >= esperaEntreAlertas)
            {
                ultimaAlerta = Time.time;
                voz.Sonar(sAlerta, volEfectos);
            }
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

        // Ya en rango de golpe (esperando el cooldown): no se le echa encima
        bool enRango = Mathf.Abs(diferenciaX) < distanciaAtaque * 0.8f;
        Mover(enRango ? 0f : velocidadPersecucion);
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

        // Ignora al propio jugador por si está en una capa de suelo
        foreach (var golpe in Physics2D.LinecastAll(transform.position, jugador.position, capaSuelo))
        {
            if (golpe.transform == jugador || golpe.transform.IsChildOf(jugador)) continue;
            if (golpe.transform == transform || golpe.transform.IsChildOf(transform)) continue;
            return false;
        }
        return true;
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

    // ---------------- Ataque ----------------

    bool PuedeAtacar()
    {
        if (hitboxAtaque == null || jugador == null) return false;
        if (Time.time - ultimoAtaque < cooldownAtaque) return false;
        Vector2 d = jugador.position - transform.position;
        return Mathf.Abs(d.x) <= distanciaAtaque && Mathf.Abs(d.y) <= alturaAtaque;
    }

    void IniciarAtaque()
    {
        MirarHacia(jugador.position.x - transform.position.x);
        rutinaAtaque = StartCoroutine(Atacar());
    }

    IEnumerator Atacar()
    {
        atacando = true;
        if (tenirAlPerseguir && rutinaColor == null) sprite.color = colorOriginal;

        Animar(framesCarga, tiempoCarga, false);
        voz.Sonar(sAtaque, volEfectos);
        yield return new WaitForSeconds(tiempoCarga);

        Animar(framesGolpe, duracionGolpe, false);
        hitboxAtaque.SetActive(true);
        yield return new WaitForSeconds(duracionGolpe);
        hitboxAtaque.SetActive(false);

        Animar(framesRecuperar, tiempoRecuperacion, false);
        yield return new WaitForSeconds(tiempoRecuperacion);

        TerminarAtaque();
    }

    void TerminarAtaque()
    {
        if (hitboxAtaque != null) hitboxAtaque.SetActive(false);
        ultimoAtaque = Time.time;
        atacando = false;
        rutinaAtaque = null;
        Animar(framesMover, 0f, true);
    }

    void CancelarAtaque()
    {
        if (rutinaAtaque != null) StopCoroutine(rutinaAtaque);
        if (atacando) TerminarAtaque();
    }

    // Parry del jugador: se interrumpe y queda aturdido
    public void OnParried(Vector2 pushDirection)
    {
        if (muerto) return;
        CancelarAtaque();
        aturdido = tiempoAturdidoParry;
        rb.linearVelocity = new Vector2(pushDirection.x * empujeAlRecibirGolpe, rb.linearVelocity.y);
        PonerColor(colorAturdido);
    }

    // ---------------- Vida ----------------

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (muerto || amount <= 0) return;

        vida -= amount;
        if (vida <= 0) { StartCoroutine(Morir()); return; }

        voz.Sonar(sDolor, volEfectos);
        CancelarAtaque();
        aturdido = Mathf.Max(aturdido, 0.25f);
        rb.linearVelocity = new Vector2(Mathf.Sign(hitDirection.x) * empujeAlRecibirGolpe, rb.linearVelocity.y);
        if (rutinaColor != null) StopCoroutine(rutinaColor);
        rutinaColor = StartCoroutine(Parpadear());
    }

    IEnumerator Parpadear()
    {
        sprite.color = colorGolpe;
        yield return new WaitForSeconds(0.1f);
        sprite.color = aturdido > 0.3f ? colorAturdido : colorOriginal;
        rutinaColor = null;
    }

    void PonerColor(Color c)
    {
        if (rutinaColor != null) { StopCoroutine(rutinaColor); rutinaColor = null; }
        sprite.color = c;
    }

    IEnumerator Morir()
    {
        muerto = true;
        SonidosMundo.EnemigoMuerto();
        voz.ApagarFondo(0.3f);
        voz.SonarDesprendido(sMuerte, volEfectos);
        CancelarAtaque();
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;

        float t = 0f;
        Color c0 = colorOriginal;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            sprite.color = new Color(c0.r, c0.g, c0.b, 1f - t / 0.8f);
            transform.position += Vector3.up * 0.6f * Time.deltaTime; // el fantasma se eleva al disiparse
            yield return null;
        }
        Destroy(gameObject);
    }

    // ---------------- Animación por cuadros ----------------

    void Animar(Sprite[] cuadros, float duracion, bool bucle)
    {
        if (cuadros == null || cuadros.Length == 0) return;
        animActual = cuadros;
        duracionAnim = duracion;
        animBucle = bucle;
        tiempoAnim = 0f;
        sprite.sprite = cuadros[0];
    }

    void ActualizarAnimacion()
    {
        if (sprite == null || animActual == null || animActual.Length == 0) return;
        tiempoAnim += Time.deltaTime;
        int i;
        if (animBucle || duracionAnim <= 0f)
            i = Mathf.FloorToInt(tiempoAnim * fpsMover) % animActual.Length;
        else // reparte los cuadros en la duración de la fase (carga, golpe, recuperación)
            i = Mathf.Min(Mathf.FloorToInt(tiempoAnim / duracionAnim * animActual.Length), animActual.Length - 1);
        sprite.sprite = animActual[i];
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

    void OnDrawGizmosSelected()
    {
        if (hitboxAtaque != null && hitboxAtaque.TryGetComponent(out BoxCollider2D caja))
        {
            Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.5f);
            Gizmos.matrix = hitboxAtaque.transform.localToWorldMatrix;
            Gizmos.DrawCube(caja.offset, caja.size);
        }
    }
}
