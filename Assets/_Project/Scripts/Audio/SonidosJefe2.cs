using System.Collections;
using UnityEngine;

// Personalidad sonora del jefe 2 (Jikininki, Círculo VIII). Clips en Resources/Audio/Jefe2.
// Boss2AttackController lo agrega solo; no hay que ponerlo en la escena.
//  - Respira y arrastra cadenas mientras vive (loop + cadenas sueltas de vez en cuando)
//  - Ruge cuando empieza el combate
//  - Tajo de fuego: zarpazo. Manos arriba: cadenas que se levantan y latigazo
//  - Susurra burlas entre ataques y se ríe al pasar a la fase 2 y cuando mata al jugador
//  - Dolor y muerte propios (reemplazan los del jefe 1 en SonidosJefe)
public class SonidosJefe2 : MonoBehaviour
{
    const string Carpeta = "Audio/Jefe2/";

    [Header("Volumen")]
    [Range(0f, 1f)] [SerializeField] float volRespiracion = 0.35f;
    [Range(0f, 1f)] [SerializeField] float volRugido = 0.95f;
    [Range(0f, 1f)] [SerializeField] float volGarra = 0.9f;
    [Range(0f, 1f)] [SerializeField] float volCadenas = 0.6f;
    [Range(0f, 1f)] [SerializeField] float volLatigo = 0.9f;
    [Range(0f, 1f)] [SerializeField] float volSusurro = 0.75f;
    [Range(0f, 1f)] [SerializeField] float volRisa = 0.9f;

    [Header("Ritmo")]
    [Tooltip("Segundos entre cadenas sueltas mientras está quieto")]
    [SerializeField] Vector2 intervaloCadenas = new Vector2(4f, 8f);
    [Tooltip("Segundos entre susurros durante el combate")]
    [SerializeField] Vector2 intervaloSusurro = new Vector2(12f, 20f);

    // Punto del clip donde cae el golpe (medido en cada archivo): se adelanta para que coincida con el ataque
    const float PicoGarra = 0.24f;
    const float PicoLatigo = 0.41f;

    AudioClip sRespiracion, sRugido, sCadenaChica, sCadenaGrande, sLatigo, sSusurro, sRisa;
    AudioClip[] sGarras;
    AudioSource fuenteLoop, fuenteVoz;
    AudioSource[] efectos;
    int siguiente, ultimaGarra = -1;

    bool peleando, muerto;
    float proximaCadena, proximoSusurro;
    float ocupadoHasta; // mientras ataca no hay cadenas sueltas ni susurros

    void Awake()
    {
        sRespiracion = Cargar("boss2_idle_loop");
        sRugido = Cargar("boss2_alert_roar");
        sCadenaChica = Cargar("boss2_chain_rattle_small");
        sCadenaGrande = Cargar("boss2_chain_rattle_big");
        sLatigo = Cargar("boss2_chain_whip");
        sSusurro = Cargar("boss2_whisper_taunt");
        sRisa = Cargar("boss2_laugh");
        sGarras = new[] { Cargar("boss2_claw_swipe_01"), Cargar("boss2_claw_swipe_02") };

        fuenteLoop = CrearFuente();
        fuenteLoop.loop = true;
        fuenteVoz = CrearFuente();
        efectos = new[] { CrearFuente(), CrearFuente(), CrearFuente() };
    }

    void Start()
    {
        // Dolor y muerte del jefe 2 en vez de los del jefe 1
        var sonidosJefe = GetComponent<SonidosJefe>();
        if (sonidosJefe != null) sonidosJefe.Personalizar(Carpeta + "boss2_hurt_0", 2, Carpeta + "boss2_death");

        var vida = GetComponent<BossHealth>();
        if (vida != null) vida.Died += AlMorir;

        if (sRespiracion != null)
        {
            fuenteLoop.clip = sRespiracion;
            fuenteLoop.volume = volRespiracion;
            fuenteLoop.Play();
        }
        proximaCadena = Time.time + Random.Range(intervaloCadenas.x, intervaloCadenas.y);
    }

    void OnDestroy()
    {
        var vida = GetComponent<BossHealth>();
        if (vida != null) vida.Died -= AlMorir;
    }

    void Update()
    {
        if (muerto || Time.time < ocupadoHasta) return;

        if (Time.time >= proximaCadena)
        {
            proximaCadena = Time.time + Random.Range(intervaloCadenas.x, intervaloCadenas.y);
            Efecto(sCadenaChica, volCadenas * 0.7f, 0.08f);
        }

        if (peleando && Time.time >= proximoSusurro && !fuenteVoz.isPlaying)
        {
            proximoSusurro = Time.time + Random.Range(intervaloSusurro.x, intervaloSusurro.y);
            Voz(sSusurro, volSusurro);
        }
    }

    // ---------------- API (la llama Boss2AttackController) ----------------

    public void EmpezarPelea()
    {
        if (peleando || muerto) return;
        peleando = true;
        Voz(sRugido, volRugido);
        proximoSusurro = Time.time + Random.Range(intervaloSusurro.x, intervaloSusurro.y);
    }

    // golpe: segundos hasta el impacto (ya divididos por la velocidad de la fase 2)
    public void Ataque(string trigger, float golpe, float duracion)
    {
        if (muerto) return;
        ocupadoHasta = Time.time + duracion + 0.5f;
        if (trigger == "RaiseHands")
        {
            Efecto(sCadenaGrande, volCadenas, 0.05f);
            StartCoroutine(Diferido(sLatigo, volLatigo, golpe - PicoLatigo, 0.05f));
        }
        else
        {
            if (Random.value < 0.5f) Efecto(sCadenaChica, volCadenas * 0.8f, 0.08f);
            StartCoroutine(Diferido(AlAzar(sGarras, ref ultimaGarra), volGarra, golpe - PicoGarra, 0.06f));
        }
    }

    public void Fase2()
    {
        if (muerto) return;
        Voz(sRisa, volRisa);
        Efecto(sCadenaGrande, volCadenas, 0f);
    }

    // Mató al jugador: se ríe. La pelea vuelve a empezar (y a rugir) cuando el jugador reaparece
    public void JugadorMuerto()
    {
        if (muerto) return;
        peleando = false;
        StopAllCoroutines();
        StartCoroutine(Diferido(sRisa, volRisa, 0.6f, 0f, true));
    }

    void AlMorir()
    {
        muerto = true;
        StopAllCoroutines();
        fuenteVoz.Stop();
        StartCoroutine(ApagarLoop(1.5f));
    }

    // ---------------- Interno ----------------

    IEnumerator Diferido(AudioClip clip, float volumen, float espera, float variacion, bool comoVoz = false)
    {
        if (espera > 0f) yield return new WaitForSeconds(espera);
        if (muerto) yield break;
        if (comoVoz) Voz(clip, volumen);
        else Efecto(clip, volumen, variacion);
    }

    IEnumerator ApagarLoop(float tiempo)
    {
        float v0 = fuenteLoop.volume, t = 0f;
        while (t < tiempo)
        {
            t += Time.deltaTime;
            fuenteLoop.volume = Mathf.Lerp(v0, 0f, t / tiempo);
            yield return null;
        }
        fuenteLoop.Stop();
    }

    // Una sola voz a la vez (rugido, susurro, risa): no se pisan entre ellas
    void Voz(AudioClip clip, float volumen)
    {
        if (clip == null) return;
        fuenteVoz.Stop();
        fuenteVoz.clip = clip;
        fuenteVoz.volume = volumen;
        fuenteVoz.Play();
    }

    void Efecto(AudioClip clip, float volumen, float variacionTono)
    {
        if (clip == null) return;
        var f = efectos[siguiente];
        siguiente = (siguiente + 1) % efectos.Length;
        f.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        f.PlayOneShot(clip, volumen);
    }

    static AudioClip AlAzar(AudioClip[] clips, ref int ultimo)
    {
        if (clips == null || clips.Length == 0) return null;
        int i = Random.Range(0, clips.Length);
        if (clips.Length > 1 && i == ultimo) i = (i + 1) % clips.Length;
        ultimo = i;
        return clips[i];
    }

    AudioSource CrearFuente()
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.spatialBlend = 0f;
        return f;
    }

    static AudioClip Cargar(string nombre)
    {
        var c = Resources.Load<AudioClip>(Carpeta + nombre);
        if (c == null) Debug.LogWarning($"SonidosJefe2: falta Resources/{Carpeta}{nombre}");
        return c;
    }
}
