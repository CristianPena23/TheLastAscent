using UnityEngine;

// Sonidos de interfaz y de escritura de diálogos. Se crea solo al iniciar el juego
// (no hay que ponerlo en ninguna escena) y carga los clips de Resources/Audio/UI.
// Si el TemaUI tiene sonidoNavegar / sonidoAceptar asignados, se usan esos en su lugar.
// Uso: SonidosUI.Navegar(); SonidosUI.Aceptar(); SonidosUI.Volver(); SonidosUI.Tecla(caracter);
public class SonidosUI : MonoBehaviour
{
    const string Carpeta = "Audio/UI/";

    static SonidosUI instancia;

    [Range(0f, 1f)] [SerializeField] float volumenClic = 1f;
    [Range(0f, 1f)] [SerializeField] float volumenEscritura = 0.6f;
    [Tooltip("Suena una tecla cada N letras (los espacios suenan siempre)")]
    [SerializeField] int letrasPorTecla = 2;
    [SerializeField] Vector2 rangoTono = new Vector2(0.94f, 1.06f);

    AudioClip[] navegar;
    AudioClip aceptar, volver;
    AudioClip[] teclas;
    AudioClip espacio;

    AudioSource fuenteClic;      // sigue sonando en pausa (el menú de pausa es interfaz)
    AudioSource fuenteEscritura; // se pausa con el juego
    int ultimoClicNavegar = -1;
    int ultimaTecla = -1;
    int contadorLetras;
    int frameVolver = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("SonidosUI");
        DontDestroyOnLoad(go);
        go.AddComponent<SonidosUI>();
    }

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;

        navegar = new[] { Cargar("ui_click_01"), Cargar("ui_click_02"), Cargar("ui_click_03") };
        aceptar = Cargar("ui_click_confirm");
        volver = Cargar("ui_click_back");
        teclas = new AudioClip[6];
        for (int i = 0; i < teclas.Length; i++) teclas[i] = Cargar($"dialog_type_0{i + 1}");
        espacio = Cargar("dialog_type_space");

        fuenteClic = CrearFuente(true);
        fuenteEscritura = CrearFuente(false);
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    AudioSource CrearFuente(bool ignorarPausa)
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.spatialBlend = 0f;
        f.ignoreListenerPause = ignorarPausa;
        return f;
    }

    static AudioClip Cargar(string nombre)
    {
        var clip = Resources.Load<AudioClip>(Carpeta + nombre);
        if (clip == null) Debug.LogWarning($"SonidosUI: falta Resources/{Carpeta}{nombre}");
        return clip;
    }

    static TemaUI Tema => TemaUI.O(null);

    // ---------------- Interfaz ----------------

    public static void Navegar()
    {
        if (instancia == null) return;
        var deTema = Tema != null ? Tema.sonidoNavegar : null;
        instancia.Clic(deTema != null ? deTema : instancia.AlAzar(instancia.navegar, ref instancia.ultimoClicNavegar));
    }

    public static void Aceptar()
    {
        // Un botón "Volver" dispara Aceptar (por el clic) y Volver en el mismo frame: gana Volver
        if (instancia == null || Time.frameCount == instancia.frameVolver) return;
        var deTema = Tema != null ? Tema.sonidoAceptar : null;
        instancia.Clic(deTema != null ? deTema : instancia.aceptar);
    }

    public static void Volver()
    {
        if (instancia == null) return;
        instancia.frameVolver = Time.frameCount;
        instancia.Clic(instancia.volver);
    }

    // Play (no PlayOneShot) para que Volver pueda reemplazar a un Aceptar del mismo frame
    void Clic(AudioClip clip)
    {
        if (clip == null) return;
        fuenteClic.clip = clip;
        fuenteClic.volume = volumenClic;
        fuenteClic.pitch = 1f;
        fuenteClic.Play();
    }

    // ---------------- Diálogos ----------------

    // Al empezar una línea nueva, para que la primera letra siempre suene
    public static void ReiniciarEscritura()
    {
        if (instancia != null) instancia.contadorLetras = 0;
    }

    public static void Tecla(char c)
    {
        if (instancia == null) return;
        instancia.SonarTecla(c);
    }

    void SonarTecla(char c)
    {
        AudioClip clip;
        if (c == ' ') clip = espacio;
        else if (char.IsLetterOrDigit(c))
        {
            if (contadorLetras++ % Mathf.Max(1, letrasPorTecla) != 0) return;
            clip = AlAzar(teclas, ref ultimaTecla);
        }
        else return; // puntuación y saltos de línea: silencio

        if (clip == null) return;
        fuenteEscritura.pitch = Random.Range(rangoTono.x, rangoTono.y);
        fuenteEscritura.PlayOneShot(clip, volumenEscritura);
    }

    // Elige uno al azar sin repetir el anterior
    AudioClip AlAzar(AudioClip[] clips, ref int ultimo)
    {
        if (clips == null || clips.Length == 0) return null;
        int i = Random.Range(0, clips.Length);
        if (clips.Length > 1 && i == ultimo) i = (i + 1) % clips.Length;
        ultimo = i;
        return clips[i];
    }
}
