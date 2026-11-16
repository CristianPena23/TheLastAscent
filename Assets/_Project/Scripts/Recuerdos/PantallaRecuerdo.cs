using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pantalla que se abre al recoger un recuerdo: destello dorado, "RECUERDO RECUPERADO",
// el título en grande y el texto del recuerdo apareciendo línea a línea. El juego queda
// congelado mientras se lee. Pulsar muestra todo de golpe; pulsar otra vez cierra.
// Uso: PantallaRecuerdo.Mostrar(datos, () => { /* al cerrar */ });
public class PantallaRecuerdo : MonoBehaviour
{
    public static bool Abierta { get; private set; }

    private const float Destello = 0.45f;
    private const float EntreLineas = 1.6f;
    private const float AparecerLinea = 1.2f;

    private DatosRecuerdo datos;
    private Action alCerrar;

    private CanvasGroup grupo;
    private Image destello;
    private RawImage resplandor;
    private RectTransform linea;
    private CanvasGroup grupoCabecera;
    private readonly List<CanvasGroup> lineas = new List<CanvasGroup>();
    private CanvasGroup grupoPie;
    private RectTransform icono;

    private bool mostrarTodo;
    private bool cerrar;
    private bool restaurado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimpiarEstaticos() => Abierta = false;

    public static void Mostrar(DatosRecuerdo datos, Action alCerrar)
    {
        if (datos == null) { alCerrar?.Invoke(); return; }
        var go = new GameObject("PantallaRecuerdo");
        var p = go.AddComponent<PantallaRecuerdo>();
        p.datos = datos;
        p.alCerrar = alCerrar;
        Abierta = true;
    }

    private void Start()
    {
        Construir();
        StartCoroutine(Correr());
    }

    private void OnDestroy()
    {
        // Si la escena se descarga a mitad, el juego no queda congelado
        Restaurar();
    }

    private void Restaurar()
    {
        if (restaurado) return;
        restaurado = true;
        Abierta = false;
        if (!PausaJuego.EstaPausado) Time.timeScale = 1f;
        var c = alCerrar;
        alCerrar = null;
        c?.Invoke();
    }

    private IEnumerator Correr()
    {
        float t;

        // Destello blanco dorado que llena la pantalla
        grupo.alpha = 1f;
        t = 0f;
        while (t < Destello)
        {
            t += Time.unscaledDeltaTime;
            destello.color = ConAlfa(destello.color, Suave(t / Destello));
            yield return null;
        }
        Time.timeScale = 0f;

        // El destello se apaga y queda el velo oscuro con el resplandor
        t = 0f;
        while (t < 1.2f)
        {
            t += Time.unscaledDeltaTime;
            float k = Suave(t / 1.2f);
            destello.color = ConAlfa(destello.color, 1f - k);
            grupoCabecera.alpha = Suave((t - 0.3f) / 0.9f);
            AplicarLinea(Suave((t - 0.5f) / 0.7f));
            yield return null;
        }
        destello.color = ConAlfa(destello.color, 0f);
        grupoCabecera.alpha = 1f;
        AplicarLinea(1f);

        // Líneas del recuerdo, una por una
        for (int i = 0; i < lineas.Count; i++)
        {
            float espera = i == 0 ? 0.4f : EntreLineas;
            t = 0f;
            while (t < espera && !mostrarTodo) { t += Time.unscaledDeltaTime; yield return null; }
            StartCoroutine(AparecerLineaRutina(lineas[i]));
        }

        t = 0f;
        while (t < 1f && !mostrarTodo) { t += Time.unscaledDeltaTime; yield return null; }
        mostrarTodo = true;

        // "Pulsa para continuar" y espera
        while (!cerrar)
        {
            grupoPie.alpha = 0.45f + 0.35f * Mathf.Sin(Time.unscaledTime * 2.4f);
            yield return null;
        }
        grupoPie.alpha = 0f;

        // Vuelve al juego
        Time.timeScale = 1f;
        t = 0f;
        while (t < 0.9f)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = 1f - Suave(t / 0.9f);
            yield return null;
        }
        Restaurar();
        Destroy(gameObject);
    }

    private IEnumerator AparecerLineaRutina(CanvasGroup g)
    {
        var r = (RectTransform)g.transform;
        Vector2 final = r.anchoredPosition;
        float t = 0f;
        while (t < AparecerLinea)
        {
            t += Time.unscaledDeltaTime;
            float k = mostrarTodo ? 1f : Suave(t / AparecerLinea);
            g.alpha = k;
            r.anchoredPosition = final + Vector2.down * (14f * (1f - k));
            if (k >= 1f) break;
            yield return null;
        }
        g.alpha = 1f;
        r.anchoredPosition = final;
    }

    private void Update()
    {
        if (resplandor != null)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.3f);
            resplandor.color = ConAlfa(resplandor.color, 0.16f + 0.08f * p);
            resplandor.rectTransform.localScale = Vector3.one * (1f + 0.04f * p);
        }
        if (icono != null)
        {
            float giro = Mathf.Cos(Time.unscaledTime * 1.3f);
            icono.localScale = new Vector3(0.55f + 0.45f * Mathf.Abs(giro), 1f, 1f);
            icono.anchoredPosition = new Vector2(0f, 455f + 8f * Mathf.Sin(Time.unscaledTime * 1.6f));
        }

        if (destello == null || destello.color.a > 0.5f) return; // no se salta el destello
        if (!Pulsado()) return;
        if (!mostrarTodo) mostrarTodo = true;
        else cerrar = true;
    }

    private static bool Pulsado()
    {
        var teclado = Keyboard.current;
        var mando = Gamepad.current;
        var raton = Mouse.current;
        return (teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame
                                    || teclado.escapeKey.wasPressedThisFrame || teclado.eKey.wasPressedThisFrame
                                    || teclado.numpadEnterKey.wasPressedThisFrame))
            || (mando != null && (mando.buttonSouth.wasPressedThisFrame || mando.buttonEast.wasPressedThisFrame
                                  || mando.startButton.wasPressedThisFrame))
            || (raton != null && raton.leftButton.wasPressedThisFrame);
    }

    // ---------- Construcción ----------

    private void Construir()
    {
        var tema = TemaUI.Predeterminado;
        Color cTexto = tema != null ? tema.texto : new Color(0.91f, 0.86f, 0.78f);
        Color cSuave = tema != null ? tema.textoNormal : new Color(0.8f, 0.75f, 0.68f);
        Color oro = FragmentoRecuerdo.Oro;
        TMP_FontAsset fTitulo = tema != null ? tema.fuenteTitulo : null;
        Material mTitulo = tema != null ? tema.materialTitulo : null;
        TMP_FontAsset fTexto = tema != null ? tema.fuenteTexto : null;
        Material mTexto = tema != null ? tema.materialTexto : null;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80; // encima del HUD, del cartel del círculo y de la pausa
        var escala = gameObject.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(2400, 1350);
        escala.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        // Velo oscuro (tapa y bloquea todo lo de atrás)
        var velo = Nuevo("Velo", transform);
        Estirar(velo);
        velo.gameObject.AddComponent<Image>().color = new Color(0.02f, 0.015f, 0.01f, 0.9f);

        // Resplandor cálido al centro
        var rr = Nuevo("Resplandor", transform);
        rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(0.5f, 0.5f);
        rr.sizeDelta = new Vector2(2300f, 1500f);
        resplandor = rr.gameObject.AddComponent<RawImage>();
        resplandor.texture = GlowHalo.GetSprite().texture;
        resplandor.color = ConAlfa(oro, 0.2f);
        resplandor.raycastTarget = false;

        // Cabecera: cristal, "RECUERDO RECUPERADO", título, línea, ordinal y círculo
        var cab = Nuevo("Cabecera", transform);
        Estirar(cab);
        grupoCabecera = cab.gameObject.AddComponent<CanvasGroup>();
        grupoCabecera.alpha = 0f;

        var haloIcono = Nuevo("HaloIcono", cab);
        haloIcono.anchorMin = haloIcono.anchorMax = haloIcono.pivot = new Vector2(0.5f, 0.5f);
        haloIcono.anchoredPosition = new Vector2(0f, 455f);
        haloIcono.sizeDelta = new Vector2(300f, 300f);
        var imgHalo = haloIcono.gameObject.AddComponent<Image>();
        imgHalo.sprite = GlowHalo.GetSprite();
        imgHalo.color = ConAlfa(oro, 0.7f);
        imgHalo.raycastTarget = false;

        icono = Nuevo("Cristal", cab);
        icono.anchorMin = icono.anchorMax = new Vector2(0.5f, 0.5f);
        icono.pivot = new Vector2(0.5f, 0.36f);
        icono.anchoredPosition = new Vector2(0f, 455f);
        icono.sizeDelta = new Vector2(60f, 120f);
        var imgCristal = icono.gameObject.AddComponent<Image>();
        imgCristal.sprite = FragmentoRecuerdo.SpriteCristal();
        imgCristal.color = FragmentoRecuerdo.Blanco;
        imgCristal.raycastTarget = false;

        var etiqueta = Texto(cab, "Etiqueta", "RECUERDO RECUPERADO", 30f, oro, fTitulo, mTitulo, 345f, 1400f);
        etiqueta.characterSpacing = 24f;

        var titulo = Texto(cab, "Titulo", datos.titulo, 100f, cTexto, fTitulo, mTitulo, 240f, 2000f);
        titulo.characterSpacing = 6f;

        linea = Nuevo("Linea", cab);
        linea.anchorMin = linea.anchorMax = linea.pivot = new Vector2(0.5f, 0.5f);
        linea.anchoredPosition = new Vector2(0f, 168f);
        linea.sizeDelta = new Vector2(0f, 3f);
        var imgLinea = linea.gameObject.AddComponent<Image>();
        imgLinea.color = ConAlfa(oro, 0.85f);
        imgLinea.raycastTarget = false;
        // Rombo en el centro de la línea
        var rombo = Nuevo("Rombo", cab);
        rombo.anchorMin = rombo.anchorMax = rombo.pivot = new Vector2(0.5f, 0.5f);
        rombo.anchoredPosition = new Vector2(0f, 168f);
        rombo.sizeDelta = new Vector2(16f, 16f);
        rombo.localRotation = Quaternion.Euler(0f, 0f, 45f);
        rombo.gameObject.AddComponent<Image>().color = oro;

        string sub = datos.Ordinal() + "  ·  Círculo " + DatosCirculo.Romano(datos.circulo);
        var subtitulo = Texto(cab, "Subtitulo", sub, 36f, cSuave, fTexto, mTexto, 118f, 1400f);
        subtitulo.fontStyle = FontStyles.Italic;
        subtitulo.characterSpacing = 4f;

        // Texto del recuerdo
        float y = 10f;
        foreach (var l in datos.lineas ?? new string[0])
        {
            var t = Texto(transform, "Linea", l, 46f, cTexto, fTexto, mTexto, y, 1900f);
            t.fontStyle = FontStyles.Italic;
            var g = t.gameObject.AddComponent<CanvasGroup>();
            g.alpha = 0f;
            lineas.Add(g);
            y -= 76f;
        }

        // Pie
        var pie = Texto(transform, "Pie", "Pulsa para continuar", 30f, cSuave, fTexto, mTexto, -540f, 1000f);
        pie.fontStyle = FontStyles.Italic;
        grupoPie = pie.gameObject.AddComponent<CanvasGroup>();
        grupoPie.alpha = 0f;

        // Destello encima de todo
        var d = Nuevo("Destello", transform);
        Estirar(d);
        destello = d.gameObject.AddComponent<Image>();
        destello.color = new Color(1f, 0.93f, 0.78f, 0f);
        destello.raycastTarget = false;
    }

    private void AplicarLinea(float k) => linea.sizeDelta = new Vector2(820f * Mathf.Clamp01(k), 3f);

    private static TMP_Text Texto(Transform padre, string nombre, string contenido, float tamano, Color color,
        TMP_FontAsset fuente, Material material, float y, float ancho)
    {
        var r = Nuevo(nombre, padre);
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        if (fuente != null && material != null) t.fontSharedMaterial = material;
        t.text = contenido;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(0f, y);
        r.sizeDelta = new Vector2(ancho, tamano * 1.6f);
        return t;
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

    private static Color ConAlfa(Color c, float a) { c.a = Mathf.Clamp01(a); return c; }

    private static float Suave(float x)
    {
        x = Mathf.Clamp01(x);
        return x * x * (3f - 2f * x);
    }
}
