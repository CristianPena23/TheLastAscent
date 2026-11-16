using System.Collections;
using UnityEngine;

// Sonidos del jefe: golpes de brazo y combo (sincronizados con el impacto de la animación),
// cristales de hielo que brotan del suelo, dolor, muerte y la fanfarria de nivel completado.
// BossHealth lo agrega solo al jefe. Los clips se cargan de Resources/Audio/Jefe; los nombres
// se pueden cambiar en el Inspector para reutilizarlo con otros jefes.
// No es posicional: el jefe ocupa toda la arena.
public class SonidosJefe : MonoBehaviour
{
    const string Carpeta = "Audio/Jefe/";

    public static SonidosJefe Instancia { get; private set; }

    [Header("Clips (Resources/Audio/Jefe)")]
    [SerializeField] string golpeIzquierdo = "boss_arm_left_attack";
    [SerializeField] string golpeDerecho = "boss_arm_right_attack";
    [SerializeField] string combo = "boss_combo_attack";
    [Tooltip("Prefijo: se cargan _01, _02, _03...")]
    [SerializeField] string cristalPrefijo = "boss_crystal_spawn_0";
    [SerializeField] int cristalVariantes = 3;
    [SerializeField] string dolorPrefijo = "boss_hurt_0";
    [SerializeField] int dolorVariantes = 2;
    [SerializeField] string muerte = "boss_death";
    [SerializeField] string nivelCompleto = "level_complete";

    [Header("Música de pelea")]
    [Tooltip("Ruta dentro de Resources (loop largo, en streaming). Vacío = sin música de pelea")]
    [SerializeField] string musicaPelea = "Audio/boss_battle_theme_loop";
    [Range(0f, 1f)] [SerializeField] float volMusicaPelea = 0.3f;
    [Tooltip("Activado: suena desde que se entra a la escena. Desactivado: espera a que empiece el combate")]
    [SerializeField] bool peleaAlEntrar = true;
    [Tooltip("Lo que tarda en apagarse el ambiente al empezar la pelea")]
    [SerializeField] float fadeAmbiente = 0.5f;
    [Tooltip("Lo que tarda en entrar la música de pelea")]
    [SerializeField] float fadePelea = 0.4f;

    [Header("Sincronía con la animación")]
    [Tooltip("Segundo del clip de brazo donde cae el impacto")]
    [SerializeField] float impactoBrazo = 0.8f;
    [Tooltip("Segundo del clip de combo donde cae el primer impacto")]
    [SerializeField] float impactoCombo = 1.6f;
    [Tooltip("Tras terminar el ataque, cuánto sigue sonando antes de apagarse")]
    [SerializeField] float colaAtaque = 0.6f;

    [Header("Volumen")]
    [Range(0f, 1f)] [SerializeField] float volAtaque = 0.9f;
    [Range(0f, 1f)] [SerializeField] float volCristal = 0.55f;
    [Range(0f, 1f)] [SerializeField] float volDolor = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volMuerte = 1f;
    [Range(0f, 1f)] [SerializeField] float volNivelCompleto = 0.9f;

    [Header("Ritmo")]
    [Tooltip("Mínimo entre dos cristales (un barrido saca varios casi a la vez)")]
    [SerializeField] float pausaEntreCristales = 0.12f;
    [SerializeField] float pausaEntreDolor = 0.25f;
    [Tooltip("Segundos tras la muerte hasta la fanfarria (deja pasar los impactos de la muerte)")]
    [SerializeField] float esperaNivelCompleto = 6f;

    AudioClip sIzq, sDer, sCombo, sMuerte, sNivel, sPelea;
    bool peleando;
    AudioClip[] sCristales, sDolores;
    AudioSource fuenteAtaque, fuenteFinal;
    AudioSource[] efectos;
    int siguiente, ultimoCristal = -1, ultimoDolor = -1;
    float tiempoCristal = -999f, tiempoDolor = -999f;
    Coroutine rutinaAtaque;

    void Awake()
    {
        Instancia = this;
        sIzq = Cargar(golpeIzquierdo);
        sDer = Cargar(golpeDerecho);
        sCombo = Cargar(combo);
        sMuerte = Cargar(muerte);
        sNivel = Cargar(nivelCompleto);
        if (!string.IsNullOrEmpty(musicaPelea))
        {
            sPelea = Resources.Load<AudioClip>(musicaPelea);
            if (sPelea == null) Debug.LogWarning($"SonidosJefe: falta Resources/{musicaPelea}");
        }
        sCristales = CargarVarios(cristalPrefijo, cristalVariantes);
        sDolores = CargarVarios(dolorPrefijo, dolorVariantes);

        fuenteAtaque = CrearFuente();
        fuenteFinal = CrearFuente();
        efectos = new[] { CrearFuente(), CrearFuente(), CrearFuente() };
    }

    void Start()
    {
        if (peleaAlEntrar) EmpezarPelea();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
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
        if (string.IsNullOrEmpty(nombre)) return null;
        var c = Resources.Load<AudioClip>(Carpeta + nombre);
        if (c == null) Debug.LogWarning($"SonidosJefe: falta Resources/{Carpeta}{nombre}");
        return c;
    }

    static AudioClip[] CargarVarios(string prefijo, int cantidad)
    {
        var clips = new AudioClip[Mathf.Max(0, cantidad)];
        for (int i = 0; i < clips.Length; i++) clips[i] = Cargar(prefijo + (i + 1));
        return clips;
    }

    void Efecto(AudioClip clip, float volumen, float variacionTono)
    {
        if (clip == null) return;
        var f = efectos[siguiente];
        siguiente = (siguiente + 1) % efectos.Length;
        f.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        f.PlayOneShot(clip, volumen);
    }

    AudioClip AlAzar(AudioClip[] clips, ref int ultimo)
    {
        if (clips == null || clips.Length == 0) return null;
        int i = Random.Range(0, clips.Length);
        if (clips.Length > 1 && i == ultimo) i = (i + 1) % clips.Length;
        ultimo = i;
        return clips[i];
    }

    // ---------------- API ----------------

    // Al entrar a la escena (peleaAlEntrar) o cuando arranca el combate (BossAttackController)
    public void EmpezarPelea()
    {
        if (peleando || sPelea == null) return;
        peleando = true;
        MusicaAmbiente.Cambiar(sPelea, volMusicaPelea, fadeAmbiente, fadePelea);
    }

    // Lo llama BossAttackController al empezar un ataque. clipForzado: nombre opcional del ataque.
    // tiempoImpacto y duracion ya vienen divididos por la velocidad (fase 2).
    public void Ataque(string trigger, bool espejo, string clipForzado, float tiempoImpacto, float duracion)
    {
        AudioClip clip;
        float impacto;
        if (!string.IsNullOrEmpty(clipForzado)) { clip = Cargar(clipForzado); impacto = impactoBrazo; }
        else if (trigger == "AttackCombo") { clip = sCombo; impacto = impactoCombo; }
        else if (trigger == "AttackRight" || espejo) { clip = sDer; impacto = impactoBrazo; }
        else { clip = sIzq; impacto = impactoBrazo; }
        if (clip == null) return;

        if (rutinaAtaque != null) StopCoroutine(rutinaAtaque);
        // Arranca adelantado para que el impacto del clip caiga justo cuando el golpe toca el suelo
        float desde = Mathf.Max(0f, impacto - tiempoImpacto);
        fuenteAtaque.clip = clip;
        fuenteAtaque.volume = volAtaque;
        fuenteAtaque.time = Mathf.Min(desde, clip.length - 0.01f);
        fuenteAtaque.Play();
        rutinaAtaque = StartCoroutine(CortarAtaque(duracion + colaAtaque));
    }

    // Los clips duran más que la animación: al terminar el ataque se apagan para no pisar al siguiente
    IEnumerator CortarAtaque(float despues)
    {
        yield return new WaitForSeconds(despues);
        float inicio = fuenteAtaque.volume, t = 0f;
        while (t < 0.6f && fuenteAtaque.isPlaying)
        {
            t += Time.deltaTime;
            fuenteAtaque.volume = Mathf.Lerp(inicio, 0f, t / 0.6f);
            yield return null;
        }
        fuenteAtaque.Stop();
        rutinaAtaque = null;
    }

    // Lo llama HazardZone cuando el pico de hielo brota del suelo
    public static void Cristal()
    {
        var j = Instancia;
        if (j == null || Time.time - j.tiempoCristal < j.pausaEntreCristales) return;
        j.tiempoCristal = Time.time;
        j.Efecto(j.AlAzar(j.sCristales, ref j.ultimoCristal), j.volCristal, 0.08f);
    }

    public void Dolor()
    {
        if (Time.time - tiempoDolor < pausaEntreDolor) return;
        tiempoDolor = Time.time;
        Efecto(AlAzar(sDolores, ref ultimoDolor), volDolor, 0.04f);
    }

    public void Muerte()
    {
        if (rutinaAtaque != null) StopCoroutine(rutinaAtaque);
        fuenteAtaque.Stop();
        if (sMuerte != null) fuenteFinal.PlayOneShot(sMuerte, volMuerte);
        // Se acaba la pelea: la música se apaga, suenan la muerte y la fanfarria, y vuelve el ambiente
        if (peleando) MusicaAmbiente.Detener();
        else MusicaAmbiente.Atenuar(0.25f, esperaNivelCompleto + (sNivel != null ? sNivel.length : 0f));
        peleando = false;
        StartCoroutine(Fanfarria());
    }

    IEnumerator Fanfarria()
    {
        yield return new WaitForSeconds(esperaNivelCompleto);
        if (sNivel != null)
        {
            fuenteFinal.PlayOneShot(sNivel, volNivelCompleto);
            yield return new WaitForSeconds(sNivel.length * 0.8f); // vuelve mientras la fanfarria se apaga
        }
        MusicaAmbiente.VolverAlAmbiente();
    }
}
