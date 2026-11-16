using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de vida de jefe, reutilizable. Poner en el mismo objeto que BossHealth.
// Crea su propio Canvas por codigo (sin prefab). Con el tema de UI usa el diseno del documento:
// abajo al centro, placa con el nombre y marco ornamentado. Sin tema: barra simple arriba.
[RequireComponent(typeof(BossHealth))]
public class BossHealthBar : MonoBehaviour
{
    [Header("Texto")]
    [Tooltip("Vacio = usa el nombre de BossHealth")]
    [SerializeField] private string nameOverride = "";
    [SerializeField] private int fontSize = 30;
    [SerializeField] private Color textColor = new Color(0.92f, 0.95f, 1f, 1f);

    [Header("Tamano (resolucion de referencia 1920x1080)")]
    [SerializeField] private float barWidth = 1000f;
    [SerializeField] private float barHeight = 26f;
    [SerializeField] private float topMargin = 40f;

    [Header("Colores")]
    [SerializeField] private Color frameColor = new Color(0.04f, 0.05f, 0.09f, 0.9f);
    [SerializeField] private Color fillColor = new Color(0.55f, 0.82f, 1f, 1f);
    [SerializeField] private Color lagColor = new Color(1f, 0.85f, 0.4f, 1f);

    [Header("Barra de dano retrasada")]
    [SerializeField] private float lagDelay = 0.5f;
    [SerializeField] private float lagSpeed = 0.6f;

    [SerializeField] private bool showOnStart = true;

    [Tooltip("Vacio = tema del proyecto (Resources/UI_Tema)")]
    [SerializeField] private TemaUI tema;
    [Tooltip("Diseno del documento: abajo, con placa y marco (requiere el arte en el tema)")]
    [SerializeField] private bool estiloDocumento = true;
    [Tooltip("Color de la barra en el estilo documento")]
    [SerializeField] private Color colorBarra = Color.white;

    private BossHealth boss;
    private GameObject canvasGo;
    private CanvasGroup group;
    private RectTransform fillRect, lagRect;
    private float lagPct = 1f, fillPct = 1f, lagTimer;

    private void Start()
    {
        boss = GetComponent<BossHealth>();
        tema = TemaUI.O(tema);
        if (tema != null)
        {
            fillColor = tema.vidaJefe;
            lagColor = tema.vidaJefeRetraso;
            textColor = tema.texto;
        }
        bool documento = estiloDocumento && tema != null && tema.marcoBarraJefe != null;
        if (documento) BuildDocumento();
        else Build();
        fillPct = lagPct = boss.Max > 0 ? (float)boss.Current / boss.Max : 1f;
        SetAnchor(fillRect, fillPct);
        SetAnchor(lagRect, lagPct);
        boss.Changed += OnChanged;
        boss.Died += OnDied;
        if (showOnStart) Show();
    }

    private void OnDestroy()
    {
        if (boss != null) { boss.Changed -= OnChanged; boss.Died -= OnDied; }
        if (canvasGo != null) Destroy(canvasGo);
    }

    public void Show() { if (group != null) StartCoroutine(Fade(1f, 0.5f)); }
    public void Hide() { if (group != null) StartCoroutine(Fade(0f, 0.5f)); }

    private void OnChanged(int cur, int max)
    {
        fillPct = max > 0 ? (float)cur / max : 0f;
        SetAnchor(fillRect, fillPct);
        if (fillPct > lagPct) { lagPct = fillPct; SetAnchor(lagRect, lagPct); }
        lagTimer = lagDelay;
    }

    private void OnDied() => StartCoroutine(HideAfter(1.2f));

    private IEnumerator HideAfter(float s)
    {
        yield return new WaitForSeconds(s);
        yield return Fade(0f, 0.6f);
    }

    private IEnumerator Fade(float target, float time)
    {
        float start = group.alpha, t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, target, t / time);
            yield return null;
        }
        group.alpha = target;
    }

    private void Update()
    {
        if (lagRect == null || lagPct <= fillPct) return;
        lagTimer -= Time.deltaTime;
        if (lagTimer > 0f) return;
        lagPct = Mathf.MoveTowards(lagPct, fillPct, lagSpeed * Time.deltaTime);
        SetAnchor(lagRect, lagPct);
    }

    private static void SetAnchor(RectTransform r, float pct)
    {
        if (r != null) r.anchorMax = new Vector2(Mathf.Clamp01(pct), 1f);
    }

    // Medidas tomadas del mockup del HUD (1400x787) y escaladas a 1920x1080
    private const float K = 1920f / 1400f;

    private void BuildDocumento()
    {
        CrearCanvas();
        var anclaAbajo = new Vector2(0.5f, 0f);

        // Barra abajo al centro: delgada, de un solo color (colorBarra), sin el marco café
        // (x330 y738 740x6 en coordenadas del mockup 1400x787)
        Image marco = NewImage("Marco", canvasGo.transform, new Color(0, 0, 0, 0));
        Colocar(marco.rectTransform, 330, 738, 740, 6, anclaAbajo);

        var inner = NewImage("Inner", marco.transform, new Color(0, 0, 0, 0)).rectTransform;
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
        inner.offsetMin = Vector2.zero; inner.offsetMax = Vector2.zero;
        // Barra de daño retrasado invisible: solo se ve la barra principal
        lagRect = NewImage("Lag", inner, new Color(0, 0, 0, 0)).rectTransform;
        StretchBar(lagRect);
        fillRect = NewImage("Fill", inner, colorBarra).rectTransform;
        StretchBar(fillRect);

        // Placa con el nombre, encima de la barra (x575 y677 250x50)
        Image placa = NewImage("Placa", canvasGo.transform, Color.white);
        if (tema.placaJefe != null) placa.sprite = tema.placaJefe;
        else placa.color = new Color(0, 0, 0, 0);
        Colocar(placa.rectTransform, 575, 677, 250, 50, anclaAbajo);

        var textGo = new GameObject("Nombre", typeof(RectTransform));
        textGo.transform.SetParent(placa.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        if (tema.fuenteTitulo != null) tmp.font = tema.fuenteTitulo;
        if (tema.materialTitulo != null) tmp.fontSharedMaterial = tema.materialTitulo;
        tmp.text = string.IsNullOrEmpty(nameOverride) ? boss.BossName : nameOverride;
        tmp.fontStyle = FontStyles.SmallCaps;
        tmp.fontSize = 44;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        var tr = tmp.rectTransform;
        tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(50 * K, 0); tr.offsetMax = new Vector2(-50 * K, 0);
    }

    // Posiciona con coordenadas del mockup (esquina superior izquierda, en px de 1400x787)
    private static void Colocar(RectTransform r, float x, float y, float w, float h, Vector2 ancla)
    {
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = new Vector2(0.5f, 0.5f);
        float cx = (x + w * 0.5f) * K, cy = (787f - (y + h * 0.5f)) * K;
        r.anchoredPosition = new Vector2(cx - ancla.x * 1920f, cy - ancla.y * 1080f);
        r.sizeDelta = new Vector2(w * K, h * K);
    }

    private void CrearCanvas()
    {
        canvasGo = new GameObject("BossHealthBar_UI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2400, 1350);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void Build()
    {
        CrearCanvas();
        Image frame = NewImage("Frame", canvasGo.transform, frameColor);
        var fr = frame.rectTransform;
        fr.anchorMin = fr.anchorMax = new Vector2(0.5f, 1f);
        fr.pivot = new Vector2(0.5f, 1f);
        fr.sizeDelta = new Vector2(barWidth, barHeight);
        fr.anchoredPosition = new Vector2(0f, -topMargin);

        var inner = NewImage("Inner", frame.transform, new Color(0, 0, 0, 0)).rectTransform;
        inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
        inner.offsetMin = new Vector2(4, 4); inner.offsetMax = new Vector2(-4, -4);

        lagRect = NewImage("Lag", inner, lagColor).rectTransform;
        StretchBar(lagRect);
        fillRect = NewImage("Fill", inner, fillColor).rectTransform;
        StretchBar(fillRect);

        var textGo = new GameObject("Name", typeof(RectTransform));
        textGo.transform.SetParent(frame.transform, false);
        var text = textGo.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = (string.IsNullOrEmpty(nameOverride) ? boss.BossName : nameOverride).ToUpper();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.UpperCenter;
        text.color = textColor;
        text.raycastTarget = false;
        textGo.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
        var tr = text.rectTransform;
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0f);
        tr.pivot = new Vector2(0.5f, 1f);
        tr.sizeDelta = new Vector2(barWidth, 44f);
        tr.anchoredPosition = new Vector2(0f, -4f);
    }

    private static void StretchBar(RectTransform r)
    {
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }
}
