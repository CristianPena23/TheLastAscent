using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

// Cinemática de inicio: sale al pulsar "Nueva partida", antes de entrar al Círculo 9.
// El video va en Assets/_Project/Resources/Cinematicas/ (.mp4, .webm o .mov). Si se llama
// "CinematicaInicio" se usa ese; si no, el primero de la carpeta. Sin video, la partida empieza directo. Se puede saltar pulsando dos veces
// (Esc, Enter, Espacio, clic, A/Start del mando): la primera muestra el aviso.
// Uso: CinematicaInicio.Reproducir(clip, () => SceneTransition.Go("SC_Circulo9", ""));
public class CinematicaInicio : MonoBehaviour
{
    public const string CarpetaResources = "Cinematicas";
    public const string RutaResources = CarpetaResources + "/CinematicaInicio";

    private const float FundidoEntrada = 0.8f;  // menú -> negro
    private const float FundidoVideo = 0.6f;    // negro -> video
    private const float FundidoFinal = 0.8f;    // video -> negro
    private const float DuracionAviso = 3f;

    private VideoClip clip;
    private Action alTerminar;

    private CanvasGroup grupoNegro;
    private CanvasGroup grupoVideo;
    private CanvasGroup grupoAviso;
    private RawImage pantalla;
    private AspectRatioFitter ajuste;
    private VideoPlayer reproductor;
    private RenderTexture textura;

    private bool saltar;
    private float avisoHasta = -1f;
    private bool terminado;

    // null = el de Resources; si tampoco hay, llama alTerminar enseguida
    public static void Reproducir(VideoClip clip, Action alTerminar)
    {
        if (clip == null) clip = Resources.Load<VideoClip>(RutaResources);
        if (clip == null)
        {
            // Cualquier video de la carpeta (el primero por nombre)
            var todos = Resources.LoadAll<VideoClip>(CarpetaResources);
            Array.Sort(todos, (a, b) => string.CompareOrdinal(a.name, b.name));
            if (todos.Length > 0) clip = todos[0];
        }
        if (clip == null)
        {
            alTerminar?.Invoke();
            return;
        }

        var go = new GameObject("CinematicaInicio");
        var c = go.AddComponent<CinematicaInicio>();
        c.clip = clip;
        c.alTerminar = alTerminar;
    }

    private void Start()
    {
        Construir();
        StartCoroutine(Correr());
    }

    private void OnDestroy()
    {
        if (textura != null)
        {
            textura.Release();
            Destroy(textura);
        }
    }

    private IEnumerator Correr()
    {
        // Que el teclado/mando ya no muevan el menú de fondo
        var eventos = EventSystem.current;
        if (eventos != null)
        {
            eventos.SetSelectedGameObject(null);
            eventos.sendNavigationEvents = false;
        }

        // Menú a negro mientras se baja la música del menú
        var fuentes = new List<(AudioSource fuente, float volumen)>();
        foreach (var a in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (a.gameObject.scene == gameObject.scene && a.isPlaying) fuentes.Add((a, a.volume));

        reproductor.Prepare();
        float t = 0f;
        while (t < FundidoEntrada)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / FundidoEntrada);
            grupoNegro.alpha = k;
            foreach (var (f, v) in fuentes) if (f != null) f.volume = v * (1f - k);
            yield return null;
        }
        grupoNegro.alpha = 1f;
        foreach (var (f, _) in fuentes) if (f != null) f.Stop();

        // Hasta 10 s para preparar el video; si falla, se sigue sin él
        float limite = Time.unscaledTime + 10f;
        while (!reproductor.isPrepared && !terminado && Time.unscaledTime < limite) yield return null;

        if (reproductor.isPrepared && !terminado)
        {
            if (reproductor.width > 0 && reproductor.height > 0)
                ajuste.aspectRatio = (float)reproductor.width / reproductor.height;
            reproductor.Play();

            t = 0f;
            while (t < FundidoVideo && !saltar && !terminado)
            {
                t += Time.unscaledDeltaTime;
                grupoVideo.alpha = Mathf.Clamp01(t / FundidoVideo);
                yield return null;
            }
            grupoVideo.alpha = 1f;

            while (!saltar && !terminado) yield return null;

            // Video y sonido a negro
            float volumen = reproductor.GetTargetAudioSource(0) != null ? reproductor.GetTargetAudioSource(0).volume : 1f;
            t = 0f;
            while (t < FundidoFinal)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / FundidoFinal);
                grupoVideo.alpha = 1f - k;
                grupoAviso.alpha = Mathf.Min(grupoAviso.alpha, 1f - k);
                var fuente = reproductor.GetTargetAudioSource(0);
                if (fuente != null) fuente.volume = volumen * (1f - k);
                yield return null;
            }
            reproductor.Stop();
        }

        // La pantalla queda negra: el fade de SceneTransition sale de negro a la escena nueva
        alTerminar?.Invoke();
        if (eventos != null) eventos.sendNavigationEvents = true;
    }

    private void Update()
    {
        if (terminado || saltar) return;
        if (!SaltoPresionado()) return;

        if (Time.unscaledTime < avisoHasta) saltar = true;
        else avisoHasta = Time.unscaledTime + DuracionAviso;
    }

    private void LateUpdate()
    {
        if (grupoAviso == null || saltar) return;
        float objetivo = Time.unscaledTime < avisoHasta ? 1f : 0f;
        grupoAviso.alpha = Mathf.MoveTowards(grupoAviso.alpha, objetivo, Time.unscaledDeltaTime * 4f);
    }

    private static bool SaltoPresionado()
    {
        var teclado = Keyboard.current;
        var mando = Gamepad.current;
        var raton = Mouse.current;
        return (teclado != null && (teclado.escapeKey.wasPressedThisFrame || teclado.enterKey.wasPressedThisFrame
                                    || teclado.spaceKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame))
            || (mando != null && (mando.buttonSouth.wasPressedThisFrame || mando.startButton.wasPressedThisFrame))
            || (raton != null && raton.leftButton.wasPressedThisFrame);
    }

    private void AlTerminarVideo(VideoPlayer _) => terminado = true;

    private void AlFallar(VideoPlayer _, string mensaje)
    {
        Debug.LogWarning($"CinematicaInicio: no se pudo reproducir el video: {mensaje}");
        terminado = true;
    }

    // ---------- Construcción ----------

    private void Construir()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900; // encima de todo el menú, debajo del fade entre escenas (999)
        var escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(2400, 1350);
        escala.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Negro a pantalla completa (tapa el menú y bloquea los clics)
        var negro = Nuevo("Negro", transform);
        Estirar(negro);
        grupoNegro = negro.gameObject.AddComponent<CanvasGroup>();
        grupoNegro.alpha = 0f;
        negro.gameObject.AddComponent<Image>().color = Color.black;

        // Video ajustado a la pantalla sin deformarse (franjas negras si la proporción no coincide)
        var marco = Nuevo("Video", transform);
        Estirar(marco);
        grupoVideo = marco.gameObject.AddComponent<CanvasGroup>();
        grupoVideo.alpha = 0f;
        grupoVideo.blocksRaycasts = false;
        var imagen = Nuevo("Imagen", marco);
        pantalla = imagen.gameObject.AddComponent<RawImage>();
        pantalla.raycastTarget = false;
        ajuste = imagen.gameObject.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        ajuste.aspectRatio = clip.height > 0 ? (float)clip.width / clip.height : 16f / 9f;

        int ancho = clip.width > 0 ? (int)clip.width : 1920;
        int alto = clip.height > 0 ? (int)clip.height : 1080;
        textura = new RenderTexture(ancho, alto, 0) { name = "CinematicaInicio" };
        pantalla.texture = textura;

        // Audio por un AudioSource: respeta el volumen general de Opciones (AudioListener.volume)
        var audio = gameObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.ignoreListenerPause = true;

        reproductor = gameObject.AddComponent<VideoPlayer>();
        reproductor.playOnAwake = false;
        reproductor.isLooping = false;
        reproductor.source = VideoSource.VideoClip;
        reproductor.clip = clip;
        reproductor.renderMode = VideoRenderMode.RenderTexture;
        reproductor.targetTexture = textura;
        reproductor.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        reproductor.audioOutputMode = VideoAudioOutputMode.AudioSource;
        reproductor.controlledAudioTrackCount = 1;
        reproductor.EnableAudioTrack(0, true);
        reproductor.SetTargetAudioSource(0, audio);
        reproductor.loopPointReached += AlTerminarVideo;
        reproductor.errorReceived += AlFallar;

        // Aviso para saltar (abajo a la derecha)
        var tema = TemaUI.Predeterminado;
        var aviso = Nuevo("Aviso", transform);
        aviso.anchorMin = aviso.anchorMax = aviso.pivot = new Vector2(1f, 0f);
        aviso.anchoredPosition = new Vector2(-70f, 60f);
        aviso.sizeDelta = new Vector2(900f, 60f);
        grupoAviso = aviso.gameObject.AddComponent<CanvasGroup>();
        grupoAviso.alpha = 0f;
        grupoAviso.blocksRaycasts = false;
        var texto = aviso.gameObject.AddComponent<TextMeshProUGUI>();
        if (tema != null && tema.fuenteTexto != null)
        {
            texto.font = tema.fuenteTexto;
            if (tema.materialTexto != null) texto.fontSharedMaterial = tema.materialTexto;
        }
        texto.text = "Pulsa otra vez para saltar";
        texto.fontSize = 34f;
        texto.fontStyle = FontStyles.Italic;
        texto.color = tema != null ? tema.textoNormal : new Color(0.8f, 0.75f, 0.68f);
        texto.alignment = TextAlignmentOptions.Right;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        texto.raycastTarget = false;
    }

    private static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(padre, false);
        return (RectTransform)go.transform;
    }

    private static void Estirar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
