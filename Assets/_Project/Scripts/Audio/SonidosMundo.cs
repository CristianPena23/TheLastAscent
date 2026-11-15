using UnityEngine;

// Sonidos del mundo y de la historia: checkpoints, enemigos que mueren, objetos que se recogen
// y los momentos de la luciérnaga. Se crea solo al iniciar el juego (no hay que ponerlo en escenas)
// y carga los clips de Resources/Audio/Historia. Se pausan con el juego.
// Uso: SonidosMundo.Checkpoint(); SonidosMundo.EnemigoMuerto(); SonidosMundo.Recoger(); ...
public class SonidosMundo : MonoBehaviour
{
    public const string Carpeta = "Audio/Historia/";

    static SonidosMundo instancia;

    [Range(0f, 1f)] [SerializeField] float volCheckpoint = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volMuerteEnemigo = 0.8f;
    [Range(0f, 1f)] [SerializeField] float volRecoger = 0.85f;
    [Range(0f, 1f)] [SerializeField] float volLuciernaga = 0.7f;
    [SerializeField] Vector2 rangoTono = new Vector2(0.95f, 1.05f);

    AudioClip checkpoint, muerteEnemigo, recoger, llave, luciernagaBrillo, luciernagaTomada;
    AudioSource[] fuentes;
    int siguiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("SonidosMundo");
        DontDestroyOnLoad(go);
        go.AddComponent<SonidosMundo>();
    }

    void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;

        checkpoint = Cargar("checkpoint");
        muerteEnemigo = Cargar("hit_enemy_kill");
        recoger = Cargar("memory_pickup");
        llave = Resources.Load<AudioClip>("Audio/Jefe/key_pickup");
        luciernagaBrillo = Cargar("firefly_hint_glow");
        luciernagaTomada = Cargar("firefly_taken");

        fuentes = new AudioSource[4];
        for (int i = 0; i < fuentes.Length; i++)
        {
            fuentes[i] = gameObject.AddComponent<AudioSource>();
            fuentes[i].playOnAwake = false;
            fuentes[i].spatialBlend = 0f;
        }
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    public static AudioClip Cargar(string nombre)
    {
        var clip = Resources.Load<AudioClip>(Carpeta + nombre);
        if (clip == null) Debug.LogWarning($"Sonidos: falta Resources/{Carpeta}{nombre}");
        return clip;
    }

    void Sonar(AudioClip clip, float volumen, bool variarTono)
    {
        if (clip == null) return;
        var f = fuentes[siguiente];
        siguiente = (siguiente + 1) % fuentes.Length;
        f.pitch = variarTono ? Random.Range(rangoTono.x, rangoTono.y) : 1f;
        f.PlayOneShot(clip, volumen);
    }

    // ---------------- API ----------------

    public static void Checkpoint() { if (instancia != null) instancia.Sonar(instancia.checkpoint, instancia.volCheckpoint, false); }
    public static void EnemigoMuerto() { if (instancia != null) instancia.Sonar(instancia.muerteEnemigo, instancia.volMuerteEnemigo, true); }
    // Llave del jefe (KeyPickup). Recoger() queda para memorias / coleccionables
    public static void Llave() { if (instancia != null) instancia.Sonar(instancia.llave != null ? instancia.llave : instancia.recoger, instancia.volRecoger, false); }
    public static void Recoger() { if (instancia != null) instancia.Sonar(instancia.recoger, instancia.volRecoger, false); }
    public static void LuciernagaBrillo() { if (instancia != null) instancia.Sonar(instancia.luciernagaBrillo, instancia.volLuciernaga, false); }
    // Listo para el momento de la historia en que se llevan a la luciérnaga
    public static void LuciernagaTomada() { if (instancia != null) instancia.Sonar(instancia.luciernagaTomada, instancia.volLuciernaga, false); }
}
