using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Voz posicional de un enemigo: se oye fuerte de cerca, se apaga de lejos y se ubica a izquierda o
// derecha según dónde esté respecto a la cámara. Así varios enemigos en el nivel no se vuelven ruido.
// Cada enemigo lo agrega solo (EnemigoFuego, EnemigoBase) y lo usa con:
//   voz.Fondo(clip, vol)                   loop de fondo (respiración, susurro), empieza en un punto al azar
//   voz.Sonar(clip, vol)                   efecto corto (alerta, golpe recibido)
//   voz.Canal(clip, vol, desde, loop)      sonido que luego se puede cortar con fade (fuego, inhalación)
//   voz.ApagarCanal(tiempo)
//   voz.SonarDesprendido(clip, vol)        sigue sonando aunque el enemigo se destruya (muerte)
// Los clips se cargan de Resources/Audio/Enemigos. Se pausan con el juego.
public class VozEnemigo : MonoBehaviour
{
    const string Carpeta = "Audio/Enemigos/";
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static AudioListener oyente;

    [Tooltip("Hasta esta distancia (del centro de la cámara) suena a todo volumen")]
    [SerializeField] float distanciaPlena = 5f;
    [Tooltip("Desde esta distancia ya no se oye")]
    [SerializeField] float distanciaMaxima = 16f;
    [Tooltip("Cuánto se ubica a izquierda/derecha (0 = centrado)")]
    [Range(0f, 1f)] [SerializeField] float paneo = 0.6f;

    AudioSource fondo, canal;
    AudioSource[] efectos;
    int siguiente;
    float volFondo, volCanal, fadeFondo = 1f, fadeCanal = 1f;
    Coroutine rutinaCanal, rutinaFondo;
    float atenuacion, pan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { cache.Clear(); oyente = null; }

    public static AudioClip Cargar(string nombre)
    {
        if (cache.TryGetValue(nombre, out var c) && c != null) return c;
        // "Jefe/portal_idle_loop" = ruta dentro de Resources/Audio; sin barra = Resources/Audio/Enemigos
        string ruta = nombre.Contains("/") ? "Audio/" + nombre : Carpeta + nombre;
        c = Resources.Load<AudioClip>(ruta);
        if (c == null) Debug.LogWarning($"VozEnemigo: falta Resources/{ruta}");
        cache[nombre] = c;
        return c;
    }

    void Awake()
    {
        fondo = CrearFuente();
        fondo.loop = true;
        canal = CrearFuente();
        efectos = new[] { CrearFuente(), CrearFuente() };
        Calcular();
    }

    AudioSource CrearFuente()
    {
        var f = gameObject.AddComponent<AudioSource>();
        f.playOnAwake = false;
        f.spatialBlend = 0f; // la distancia la calculamos nosotros en 2D (la cámara está en z = -10)
        f.volume = 0f;
        return f;
    }

    void Update()
    {
        Calcular();
        fondo.volume = volFondo * fadeFondo * atenuacion;
        canal.volume = volCanal * fadeCanal * atenuacion;
        fondo.panStereo = canal.panStereo = pan;
        foreach (var e in efectos) { e.volume = atenuacion; e.panStereo = pan; }
    }

    void Calcular()
    {
        if (oyente == null) oyente = FindAnyObjectByType<AudioListener>();
        if (oyente == null) { atenuacion = 1f; pan = 0f; return; }
        Vector2 d = (Vector2)(transform.position - oyente.transform.position);
        float k = Mathf.InverseLerp(distanciaMaxima, distanciaPlena, d.magnitude);
        atenuacion = k * k; // cae más natural que lineal
        pan = Mathf.Clamp(d.x / distanciaMaxima, -1f, 1f) * paneo;
    }

    // ---------------- API ----------------

    public void Fondo(AudioClip clip, float volumen)
    {
        if (clip == null) return;
        volFondo = volumen;
        fadeFondo = 1f;
        fondo.clip = clip;
        fondo.time = Random.Range(0f, clip.length * 0.9f); // que dos enemigos iguales no respiren a la vez
        fondo.Play();
    }

    public void ApagarFondo(float tiempo)
    {
        if (!fondo.isPlaying) return;
        if (rutinaFondo != null) StopCoroutine(rutinaFondo);
        rutinaFondo = StartCoroutine(Fade(v => fadeFondo = v, fadeFondo, tiempo, fondo));
    }

    public void Sonar(AudioClip clip, float volumen, float variacionTono = 0.05f)
    {
        if (clip == null) return;
        var f = efectos[siguiente];
        siguiente = (siguiente + 1) % efectos.Length;
        f.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        f.PlayOneShot(clip, volumen);
    }

    public void Canal(AudioClip clip, float volumen, float desde = 0f, bool loop = false)
    {
        if (clip == null) return;
        if (rutinaCanal != null) { StopCoroutine(rutinaCanal); rutinaCanal = null; }
        volCanal = volumen;
        fadeCanal = 1f;
        canal.clip = clip;
        canal.loop = loop;
        canal.time = Mathf.Clamp(desde, 0f, clip.length - 0.01f);
        canal.Play();
    }

    public bool CanalSonando(AudioClip clip) => canal.isPlaying && canal.clip == clip;

    public void ApagarCanal(float tiempo)
    {
        if (!canal.isPlaying) return;
        if (rutinaCanal != null) StopCoroutine(rutinaCanal);
        rutinaCanal = StartCoroutine(Fade(v => fadeCanal = v, fadeCanal, tiempo, canal));
    }

    IEnumerator Fade(System.Action<float> poner, float inicio, float tiempo, AudioSource fuente)
    {
        float t = 0f;
        while (t < tiempo)
        {
            t += Time.deltaTime;
            poner(Mathf.Lerp(inicio, 0f, t / tiempo));
            yield return null;
        }
        poner(0f);
        fuente.Stop();
    }

    // Para la muerte: un objeto aparte en el lugar, que se borra solo al terminar el sonido.
    // sobrevivirEscena: sigue sonando aunque se cargue otra escena (p. ej. al cruzar un portal)
    public void SonarDesprendido(AudioClip clip, float volumen, bool sobrevivirEscena = false)
    {
        if (clip == null) return;
        Calcular();
        var go = new GameObject("Sonido_" + clip.name);
        go.transform.position = transform.position;
        if (sobrevivirEscena) DontDestroyOnLoad(go);
        var f = go.AddComponent<AudioSource>();
        f.spatialBlend = 0f;
        f.panStereo = pan;
        f.PlayOneShot(clip, volumen * atenuacion);
        Destroy(go, clip.length + 0.1f);
    }
}
