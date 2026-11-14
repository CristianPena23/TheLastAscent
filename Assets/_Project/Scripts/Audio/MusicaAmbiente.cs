using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Ambiente sombrío de fondo para los círculos y las pantallas de interfaz.
// Se crea solo al iniciar el juego (no hay que ponerlo en ninguna escena) y sobrevive a los
// cambios de escena, así el loop no se corta ni vuelve a empezar al pasar de una a otra.
// - Respeta el slider de volumen de Opciones (usa AudioListener.volume como todo el juego).
// - En pausa sigue sonando, pero más bajo (el menú de pausa también es interfaz). En diálogos también baja.
// - Las escenas de la lista "escenasEnSilencio" lo apagan con un fade (p. ej. una pelea con música propia).
// - También lleva la música de pelea: MusicaAmbiente.Cambiar(clipPelea, vol) y MusicaAmbiente.VolverAlAmbiente().
//   Al cargar otra escena se vuelve solo al ambiente.
// Uso desde código: MusicaAmbiente.Detener(); MusicaAmbiente.Reproducir(); MusicaAmbiente.Cambiar(clip, vol);
public class MusicaAmbiente : MonoBehaviour
{
    const string RutaClip = "Audio/menu_ambient_loop"; // Assets/_Project/Resources/Audio/

    public static MusicaAmbiente Instancia { get; private set; }

    [Tooltip("Volumen del ambiente. Bajo a propósito: es fondo, no debe tapar clics, diálogos ni golpes")]
    [Range(0f, 1f)]
    [SerializeField] float volumen = 0.3f;
    [Tooltip("Multiplicador del volumen mientras el juego está en pausa")]
    [Range(0f, 1f)]
    [SerializeField] float volumenEnPausa = 0.45f;
    [Tooltip("Multiplicador del volumen mientras hay un diálogo (para que se oiga la escritura)")]
    [Range(0f, 1f)]
    [SerializeField] float volumenEnDialogo = 0.5f;
    [SerializeField] float fadeEntrada = 3f;
    [SerializeField] float fadeCambio = 1.5f;
    [Tooltip("Escenas donde el ambiente se apaga (vacío: suena en todas)")]
    [SerializeField] string[] escenasEnSilencio = new string[0];

    AudioSource fuente;
    float factorPausa = 1f;   // baja en pausa y en diálogos
    float factorFade = 0f;    // fades de entrada / salida
    Coroutine rutinaFade;
    float factorEvento = 1f, objetivoEvento = 1f, finEvento; // Atenuar(): momentos especiales
    AudioClip clipAmbiente;
    float volumenClip;        // volumen base del clip que suena ahora (ambiente o pelea)
    Coroutine rutinaCambio;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (Instancia != null) return;
        var go = new GameObject("MusicaAmbiente");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicaAmbiente>();
    }

    void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;

        fuente = gameObject.AddComponent<AudioSource>();
        clipAmbiente = Resources.Load<AudioClip>(RutaClip);
        fuente.clip = clipAmbiente;
        volumenClip = volumen;
        fuente.loop = true;
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;          // 2D: se oye igual en toda la escena
        fuente.priority = 0;               // que nunca lo corte otro sonido
        fuente.ignoreListenerPause = true; // PausaJuego pausa el AudioListener; el ambiente sigue
        fuente.volume = 0f;

        if (fuente.clip == null)
            Debug.LogWarning($"MusicaAmbiente: no se encontró Resources/{RutaClip}");

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    // Respaldo por si sceneLoaded no llegó para la primera escena
    void Start()
    {
        if (!fuente.isPlaying && rutinaFade == null)
            AlCargarEscena(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (Instancia != this) return;
        SceneManager.sceneLoaded -= AlCargarEscena;
        Instancia = null;
    }

    void Update()
    {
        // Suavizado del cambio de volumen al pausar / hablar (tiempo real: en pausa timeScale = 0)
        float objetivoPausa = PausaJuego.EstaPausado ? volumenEnPausa
                            : DialogueManager.HayDialogo ? volumenEnDialogo : 1f;
        factorPausa = Mathf.MoveTowards(factorPausa, objetivoPausa, Time.unscaledDeltaTime * 2f);
        float objetivoEv = Time.unscaledTime < finEvento ? objetivoEvento : 1f;
        factorEvento = Mathf.MoveTowards(factorEvento, objetivoEv, Time.unscaledDeltaTime * 0.5f);
        AplicarVolumen();
    }

    void AplicarVolumen() => fuente.volume = volumenClip * factorPausa * factorFade * factorEvento;

    // Baja el ambiente durante un momento especial (muerte del jefe, fanfarria) y luego vuelve solo
    public static void Atenuar(float factor, float segundos)
    {
        if (Instancia == null) return;
        Instancia.objetivoEvento = Mathf.Clamp01(factor);
        Instancia.finEvento = Time.unscaledTime + segundos;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (modo != LoadSceneMode.Single) return;
        AsegurarListener();
        bool silencio = DebeSilenciar(escena.name);
        // Quedó sonando una música de pelea (p. ej. se salió al menú desde el jefe): vuelve el ambiente
        if (!silencio && fuente.clip != clipAmbiente) { VolverAlAmbiente(); return; }
        if (silencio) FadeA(0f, fadeCambio, true);
        else FadeA(1f, fuente.isPlaying ? fadeCambio : fadeEntrada, false);
    }

    // Sin AudioListener en la escena no se oye nada (le pasaba a SC_MenuPrincipal).
    // Si falta, se le pone a la cámara principal; si no hay cámara, a un objeto de la escena.
    static void AsegurarListener()
    {
        if (FindAnyObjectByType<AudioListener>() != null) return;
        Camera camara = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        GameObject destino = camara != null ? camara.gameObject : new GameObject("AudioListener");
        destino.AddComponent<AudioListener>();
    }

    bool DebeSilenciar(string escena)
    {
        foreach (var e in escenasEnSilencio)
            if (e == escena) return true;
        return false;
    }

    void FadeA(float objetivo, float tiempo, bool detenerAlFinal)
    {
        if (fuente.clip == null) return;
        if (objetivo > 0f && !fuente.isPlaying) fuente.Play();
        if (rutinaFade != null) StopCoroutine(rutinaFade);
        rutinaFade = StartCoroutine(Fade(objetivo, tiempo, detenerAlFinal));
    }

    IEnumerator Fade(float objetivo, float tiempo, bool detenerAlFinal)
    {
        float inicio = factorFade;
        float t = 0f;
        while (t < tiempo)
        {
            t += Time.unscaledDeltaTime;
            factorFade = Mathf.Lerp(inicio, objetivo, t / tiempo);
            AplicarVolumen();
            yield return null;
        }
        factorFade = objetivo;
        AplicarVolumen();
        if (detenerAlFinal) fuente.Stop();
        rutinaFade = null;
    }

    // ---------------- API ----------------

    public static void Reproducir()
    {
        if (Instancia != null) Instancia.FadeA(1f, Instancia.fadeCambio, false);
    }

    public static void Detener()
    {
        if (Instancia != null) Instancia.FadeA(0f, Instancia.fadeCambio, true);
    }

    // Cambia el loop por otro (p. ej. música del jefe): apaga el actual y entra el nuevo desde el principio.
    // volumen < 0 = el del ambiente. fadeSalida / fadeEntrada < 0 = fadeCambio.
    public static void Cambiar(AudioClip clip, float volumen = -1f, float fadeSalida = -1f, float fadeEntrada = -1f)
    {
        if (Instancia == null || clip == null) return;
        var m = Instancia;
        float vol = volumen >= 0f ? volumen : m.volumen;
        if (m.fuente.clip == clip && m.fuente.isPlaying) { m.volumenClip = vol; return; }
        if (m.rutinaCambio != null) m.StopCoroutine(m.rutinaCambio);
        m.rutinaCambio = m.StartCoroutine(m.CambiarClip(clip, vol,
            fadeSalida >= 0f ? fadeSalida : m.fadeCambio, fadeEntrada >= 0f ? fadeEntrada : m.fadeCambio));
    }

    public static void VolverAlAmbiente()
    {
        if (Instancia == null) return;
        Cambiar(Instancia.clipAmbiente, Instancia.volumen, Instancia.fadeCambio, Instancia.fadeEntrada);
    }

    IEnumerator CambiarClip(AudioClip clip, float vol, float fadeSalida, float fadeEntrada)
    {
        if (fuente.isPlaying)
        {
            FadeA(0f, fadeSalida, true);
            while (rutinaFade != null) yield return null;
        }
        fuente.clip = clip;
        fuente.time = 0f;
        volumenClip = vol;
        FadeA(1f, fadeEntrada, false);
        rutinaCambio = null;
    }
}
