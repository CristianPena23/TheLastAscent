using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Aviso que explica el poder de inmunidad la primera vez que la barra de energía se llena.
// Se crea por código (no hay que ponerlo en ninguna escena) con el estilo del tema de la UI.
// Se queda en pantalla hasta que el jugador usa el poder (o pasan unos segundos).
public class AvisoPoderUI : MonoBehaviour
{
    const float TiempoMaximo = 12f;
    static AvisoPoderUI instancia;

    CanvasGroup grupo;
    TMP_Text cuerpo;
    float duracionPoder;

    public static void Mostrar(float duracionPoder)
    {
        if (instancia == null) instancia = Crear();
        instancia.duracionPoder = duracionPoder;
        instancia.StopAllCoroutines();
        instancia.StartCoroutine(instancia.Ciclo());
    }

    public static void Ocultar()
    {
        if (instancia == null) return;
        instancia.StopAllCoroutines();
        instancia.StartCoroutine(instancia.Fade(0f, 0.3f));
    }

    IEnumerator Ciclo()
    {
        float t = 0f;
        yield return Fade(1f, 0.35f);
        while (t < TiempoMaximo)
        {
            t += Time.unscaledDeltaTime;
            ActualizarTexto();
            yield return null;
        }
        yield return Fade(0f, 0.5f);
    }

    void ActualizarTexto()
    {
        var teclado = Keyboard.current;
        var mando = Gamepad.current;
        bool usaMando = mando != null && (teclado == null || mando.lastUpdateTime > teclado.lastUpdateTime);
        string tecla = usaMando ? "Y" : "C";
        cuerpo.text = $"Pulsa <b>{tecla}</b> para volverte inmune a los ataques durante {duracionPoder:0} segundos.";
    }

    IEnumerator Fade(float objetivo, float tiempo)
    {
        float inicio = grupo.alpha, t = 0f;
        while (t < tiempo)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(inicio, objetivo, t / tiempo);
            yield return null;
        }
        grupo.alpha = objetivo;
    }

    static AvisoPoderUI Crear()
    {
        var tema = TemaUI.Predeterminado;
        var go = new GameObject("AvisoPoder_UI");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var escala = go.AddComponent<CanvasScaler>();
        escala.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escala.referenceResolution = new Vector2(2400, 1350);
        escala.matchWidthOrHeight = 0.5f;
        var aviso = go.AddComponent<AvisoPoderUI>();
        aviso.grupo = go.AddComponent<CanvasGroup>();
        aviso.grupo.alpha = 0f;
        aviso.grupo.blocksRaycasts = false;
        aviso.grupo.interactable = false;

        // Caja oscura con borde violeta, arriba al centro
        var caja = new GameObject("Caja", typeof(RectTransform));
        caja.transform.SetParent(go.transform, false);
        var rc = (RectTransform)caja.transform;
        rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 1f);
        rc.anchoredPosition = new Vector2(0f, -150f);
        rc.sizeDelta = new Vector2(820f, 150f);
        var fondo = caja.AddComponent<Image>();
        fondo.color = tema != null ? tema.fondo : new Color(0.05f, 0.04f, 0.035f, 0.95f);
        fondo.raycastTarget = false;
        var borde = caja.AddComponent<Outline>();
        Color cBorde = tema != null ? tema.energiaLlena : new Color(0.62f, 0.5f, 1f);
        borde.effectColor = new Color(cBorde.r, cBorde.g, cBorde.b, 0.8f);
        borde.effectDistance = new Vector2(2f, -2f);

        var titulo = Texto(caja.transform, "Titulo", "ENERGÍA AL MÁXIMO", 40,
            tema != null ? tema.energiaLlena : new Color(0.62f, 0.5f, 1f), tema != null ? tema.fuenteTitulo : null,
            new Vector2(0f, -18f), new Vector2(780f, 50f));
        titulo.characterSpacing = 6;
        aviso.cuerpo = Texto(caja.transform, "Cuerpo", "", 34,
            tema != null ? tema.texto : Color.white, tema != null ? tema.fuenteTexto : null,
            new Vector2(0f, -78f), new Vector2(780f, 56f));
        aviso.ActualizarTexto();
        return aviso;
    }

    static TMP_Text Texto(Transform padre, string nombre, string texto, float tamano, Color color, TMP_FontAsset fuente, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        var r = t.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
        return t;
    }
}
