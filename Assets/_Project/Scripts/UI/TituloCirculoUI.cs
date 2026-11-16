using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Cartel al entrar a un círculo: el nombre en grande ("Traición") y debajo, en letras pequeñas,
// su número ("Círculo IX"). Se crea solo al iniciar el juego, no hay que ponerlo en ninguna escena.
// Sale una vez por círculo: no se repite al volver de la arena del jefe, pero sí al empezar
// una partida nueva o cargar una guardada (al pasar por el menú principal se olvida).
// Uso manual (opcional): TituloCirculoUI.Mostrar("Traición", "Círculo IX");
public class TituloCirculoUI : MonoBehaviour
{
    private static TituloCirculoUI instancia;

    [Header("Tiempos (segundos)")]
    [SerializeField] private float espera = 0.5f;
    [SerializeField] private float entrada = 1.2f;
    [SerializeField] private float visible = 2.6f;
    [SerializeField] private float salida = 1.4f;

    [Header("Aspecto")]
    [SerializeField] private float tamanoNombre = 120f;
    [SerializeField] private float tamanoNumero = 42f;
    [SerializeField] private float altura = 230f; // desde el centro de la pantalla (referencia 2400x1350)
    [SerializeField] private float anchoLinea = 560f;

    private CanvasGroup grupo;
    private TMP_Text textoNombre;
    private TMP_Text textoNumero;
    private RectTransform linea;
    private int ultimoCirculo;
    private Coroutine rutina;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("TituloCirculo_UI");
        DontDestroyOnLoad(go);
        go.AddComponent<TituloCirculoUI>();
    }

    public static void Mostrar(string nombre, string numero)
    {
        if (instancia == null || string.IsNullOrEmpty(nombre)) return;
        instancia.Iniciar(nombre, numero);
    }

    private void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        Construir();
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (instancia != this) return;
        instancia = null;
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Menú principal (sin jugador): la próxima vez que se entre a un círculo vuelve a salir
        if (FindFirstObjectByType<PlayerController>() == null)
        {
            ultimoCirculo = 0;
            Ocultar();
            return;
        }

        int n = DatosCirculo.NumeroActual();
        if (n <= 0 || n == ultimoCirculo) return;
        ultimoCirculo = n;
        Iniciar(DatosCirculo.TituloActual(), DatosCirculo.NombreActual());
    }

    private void Iniciar(string nombre, string numero)
    {
        textoNombre.text = nombre;
        textoNumero.text = numero;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Animar());
    }

    private void Ocultar()
    {
        if (rutina != null) StopCoroutine(rutina);
        rutina = null;
        grupo.alpha = 0f;
    }

    private IEnumerator Animar()
    {
        grupo.alpha = 0f;
        AplicarLinea(0f);

        // Que el fade negro entre escenas termine antes de empezar
        while (SceneTransition.IsBusy) yield return null;

        float t = 0f;
        while (t < espera) { t += Paso(); yield return null; }

        t = 0f;
        while (t < entrada)
        {
            t += Paso();
            float k = Suave(t / entrada);
            Aplicar(k, k);
            yield return null;
        }
        Aplicar(1f, 1f);

        t = 0f;
        while (t < visible) { t += Paso(); Aplicar(1f, 1f); yield return null; }

        t = 0f;
        while (t < salida)
        {
            t += Paso();
            Aplicar(1f - Suave(t / salida), 1f);
            yield return null;
        }
        grupo.alpha = 0f;
        rutina = null;
    }

    // En pausa el cartel se esconde y se detiene; sigue al reanudar
    private float Paso() => PausaJuego.EstaPausado ? 0f : Time.unscaledDeltaTime;

    private void Aplicar(float alfa, float linea01)
    {
        grupo.alpha = PausaJuego.EstaPausado ? 0f : alfa;
        AplicarLinea(linea01);
    }

    private void AplicarLinea(float k) => linea.sizeDelta = new Vector2(anchoLinea * k, linea.sizeDelta.y);

    private static float Suave(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }

    private void Construir()
    {
        var tema = TemaUI.Predeterminado;
        Color cTexto = tema != null ? tema.texto : new Color(0.91f, 0.86f, 0.78f);
        Color cNumero = tema != null ? tema.textoNormal : new Color(0.8f, 0.75f, 0.68f);
        Color cBrasa = tema != null ? tema.brasa : new Color(0.85f, 0.27f, 0.12f);

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // encima del HUD, debajo del fade entre escenas (999)
        var escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(2400, 1350);
        escala.matchWidthOrHeight = 0.5f;

        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        var caja = Nuevo("Cartel", transform);
        caja.anchorMin = caja.anchorMax = caja.pivot = new Vector2(0.5f, 0.5f);
        caja.anchoredPosition = new Vector2(0f, altura);
        caja.sizeDelta = new Vector2(1600f, 320f);

        // Sombra difusa detrás del texto: se lee bien también sobre la nieve del Círculo 9
        var sombra = Nuevo("Sombra", caja).gameObject.AddComponent<RawImage>();
        sombra.texture = TexturaSombra();
        sombra.color = new Color(0f, 0f, 0f, 0.55f);
        sombra.raycastTarget = false;
        Estirar(sombra.rectTransform, new Vector2(-60f, -20f));

        textoNombre = Texto(caja, "Nombre", tamanoNombre, cTexto,
            tema != null ? tema.fuenteTitulo : null, tema != null ? tema.materialTitulo : null,
            new Vector2(0f, 40f), new Vector2(1500f, 160f));
        textoNombre.characterSpacing = 14f;

        linea = Nuevo("Linea", caja);
        linea.anchorMin = linea.anchorMax = linea.pivot = new Vector2(0.5f, 0.5f);
        linea.anchoredPosition = new Vector2(0f, -48f);
        linea.sizeDelta = new Vector2(0f, 3f);
        var imgLinea = linea.gameObject.AddComponent<Image>();
        imgLinea.color = new Color(cBrasa.r, cBrasa.g, cBrasa.b, 0.85f);
        imgLinea.raycastTarget = false;

        textoNumero = Texto(caja, "Numero", tamanoNumero, cNumero,
            tema != null ? tema.fuenteTexto : null, tema != null ? tema.materialTexto : null,
            new Vector2(0f, -95f), new Vector2(1000f, 60f));
        textoNumero.characterSpacing = 8f;
        textoNumero.fontStyle = FontStyles.Italic;
    }

    private static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(padre, false);
        return (RectTransform)go.transform;
    }

    private static void Estirar(RectTransform r, Vector2 margen)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = -margen;
        r.offsetMax = margen;
    }

    private static TMP_Text Texto(Transform padre, string nombre, float tamano, Color color,
        TMP_FontAsset fuente, Material material, Vector2 pos, Vector2 tam)
    {
        var r = Nuevo(nombre, padre);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        if (fuente != null && material != null) t.fontSharedMaterial = material;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
        return t;
    }

    // Óvalo negro que se desvanece hacia los bordes
    private static Texture2D TexturaSombra()
    {
        const int ancho = 128, alto = 32;
        var tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "SombraTituloCirculo"
        };
        var px = new Color32[ancho * alto];
        for (int y = 0; y < alto; y++)
        for (int x = 0; x < ancho; x++)
        {
            float dx = (x + 0.5f) / ancho * 2f - 1f;
            float dy = (y + 0.5f) / alto * 2f - 1f;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(1f - d);
            a = a * a * (3f - 2f * a);
            px[y * ancho + x] = new Color32(255, 255, 255, (byte)(a * 255f));
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }
}
