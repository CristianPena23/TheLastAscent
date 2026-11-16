using System.Collections;
using UnityEngine;

// Volador que embiste. Recibe daño del jugador (IDamageable): cada golpe le da energía al jugador.
public class EnemigoVolador : MonoBehaviour, IDamageable
{
    [Header("Patrulla")]
    [SerializeField] float amplitud = 0.5f;
    [SerializeField] float velocidadOscilacion = 2f;

    [Header("Detección")]
    [SerializeField] float rangoDeteccion = 6f;
    [SerializeField] LayerMask capaSuelo;

    [Header("Preparación")]
    [SerializeField] float tiempoPreparacion = 0.7f;

    [Header("Embestida")]
    [SerializeField] float velocidadEmbestida = 14f;
    [SerializeField] float distanciaMaxima = 8f;
    [Tooltip("Segundos de espera entre el final de una embestida y la siguiente. 0 = sin cooldown.")]
    [SerializeField] float cooldown = 1.5f;

    [Header("Aturdimiento")]
    [SerializeField] float tiempoAturdido = 1.2f;

    [Header("Regreso")]
    [SerializeField] float velocidadRegreso = 4f;

    [Header("Vida")]
    [SerializeField] int vida = 2;
    [SerializeField] Color colorGolpe = new Color(1f, 0.5f, 0.5f);
    [SerializeField] float empujeAlRecibirGolpe = 5f;

    [Header("Ojo")]
    [SerializeField] Transform ojo;
    [SerializeField] float distanciaOjo = 0.25f;

    enum Estado { Patrulla, Preparacion, Embestida, Aturdido, Regreso }
    Estado estado = Estado.Patrulla;

    Rigidbody2D rb;
    Transform jugador;
    Vector2 nido;
    Vector2 direccionEmbestida;
    Vector2 posicionInicioEmbestida;
    float temporizador;
    float fase;
    float tiempoUltimoAtaque = -999f;
    bool muerto;
    SpriteRenderer sprite;
    Color colorOriginal;
    Coroutine rutinaColor;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        nido = transform.position;
        sprite = GetComponent<SpriteRenderer>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) colorOriginal = sprite.color;

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
    }

    void FixedUpdate()
    {
        if (muerto) return;
        switch (estado)
        {
            case Estado.Patrulla:
                Patrullar();
                if (PuedeAtacar() && PuedeVerAlJugador()) IniciarPreparacion();
                break;

            case Estado.Preparacion:
                Preparar();
                break;

            case Estado.Embestida:
                Embestir();
                break;

            case Estado.Aturdido:
                Aturdirse();
                break;

            case Estado.Regreso:
                Regresar();
                if (PuedeAtacar() && PuedeVerAlJugador()) IniciarPreparacion();
                break;
        }
    }

    // ---------------- Detección ----------------

    bool PuedeVerAlJugador()
    {
        if (jugador == null) return false;

        // El territorio se mide desde el nido, no desde la criatura
        float distanciaAlNido = Vector2.Distance(nido, jugador.position);
        if (distanciaAlNido > rangoDeteccion) return false;

        return !Physics2D.Linecast(transform.position, jugador.position, capaSuelo);
    }

    bool PuedeAtacar()
    {
        return Time.time - tiempoUltimoAtaque >= cooldown;
    }

    // ---------------- Patrulla ----------------

    void Patrullar()
    {
        fase += Time.fixedDeltaTime * velocidadOscilacion;
        float desplazamiento = Mathf.Sin(fase) * amplitud;
        rb.MovePosition(nido + Vector2.up * desplazamiento);
    }

    // ---------------- Preparación ----------------

    void IniciarPreparacion()
    {
        estado = Estado.Preparacion;
        temporizador = tiempoPreparacion;
        rb.linearVelocity = Vector2.zero;
        direccionEmbestida = ((Vector2)jugador.position - rb.position).normalized;

        if (ojo != null) ojo.localPosition = direccionEmbestida * distanciaOjo;
    }

    void Preparar()
    {
        temporizador -= Time.fixedDeltaTime;

        if (temporizador <= 0f) IniciarEmbestida();
    }

    // ---------------- Embestida ----------------

    void IniciarEmbestida()
    {
        estado = Estado.Embestida;
        posicionInicioEmbestida = rb.position;
    }

    void Embestir()
    {
        rb.linearVelocity = direccionEmbestida * velocidadEmbestida;

        float recorrido = Vector2.Distance(rb.position, posicionInicioEmbestida);
        if (recorrido >= distanciaMaxima)
        {
            rb.linearVelocity = Vector2.zero;
            tiempoUltimoAtaque = Time.time;
            estado = Estado.Regreso;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (estado != Estado.Embestida) return;
        if ((capaSuelo.value & (1 << col.gameObject.layer)) == 0) return;

        IniciarAturdimiento();
    }

    // ---------------- Aturdimiento ----------------

    void IniciarAturdimiento()
    {
        estado = Estado.Aturdido;
        temporizador = tiempoAturdido;
        rb.linearVelocity = Vector2.zero;
        tiempoUltimoAtaque = Time.time;
    }

    void Aturdirse()
    {
        temporizador -= Time.fixedDeltaTime;

        if (temporizador <= 0f) estado = Estado.Regreso;
    }

    // ---------------- Regreso ----------------

    void Regresar()
    {
        Vector2 haciaNido = nido - rb.position;

        if (haciaNido.magnitude < 0.1f)
        {
            rb.linearVelocity = Vector2.zero;
            fase = 0f;
            if (ojo != null) ojo.localPosition = Vector2.zero;
            estado = Estado.Patrulla;
            return;
        }

        rb.linearVelocity = haciaNido.normalized * velocidadRegreso;
    }

    // ---------------- Vida ----------------

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (muerto || amount <= 0) return;

        vida -= amount;
        if (rutinaColor != null) StopCoroutine(rutinaColor);
        if (vida <= 0) { StartCoroutine(Morir()); return; }

        rutinaColor = StartCoroutine(Parpadear());
        // El golpe lo frena y lo deja aturdido un momento
        IniciarAturdimiento();
        rb.linearVelocity = hitDirection.normalized * empujeAlRecibirGolpe;
    }

    IEnumerator Parpadear()
    {
        if (sprite == null) yield break;
        sprite.color = colorGolpe;
        yield return new WaitForSeconds(0.1f);
        sprite.color = colorOriginal;
        rutinaColor = null;
    }

    IEnumerator Morir()
    {
        muerto = true;
        SonidosMundo.EnemigoMuerto();
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 1.5f; // cae

        float t = 0f;
        while (t < 0.8f)
        {
            t += Time.deltaTime;
            if (sprite != null) sprite.color = new Color(colorOriginal.r, colorOriginal.g, colorOriginal.b, 1f - t / 0.8f);
            yield return null;
        }
        Destroy(gameObject);
    }

    // ---------------- Gizmos ----------------

    void OnDrawGizmos()
    {
        // En Play el territorio está en el nido; en el editor, en la posición actual
        Vector2 centro = Application.isPlaying ? nido : (Vector2)transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centro, rangoDeteccion);

        if (estado == Estado.Preparacion)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)direccionEmbestida * 3f);
        }
    }
}