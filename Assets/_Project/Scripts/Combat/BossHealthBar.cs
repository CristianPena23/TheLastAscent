using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Barra de vida de jefe, reutilizable. Poner en el mismo objeto que BossHealth.
// Crea su propio Canvas por codigo (sin prefab): arriba en el centro de la pantalla, con nombre.
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

    private BossHealth boss;
    private GameObject canvasGo;
    private CanvasGroup group;
    private RectTransform fillRect, lagRect;
    private float lagPct = 1f, fillPct = 1f, lagTimer;

    private void Start()
    {
        boss = GetComponent<BossHealth>();
        Build();
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

    private void Build()
    {
        canvasGo = new GameObject("BossHealthBar_UI");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasGo.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

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

        fillPct = lagPct = boss.Max > 0 ? (float)boss.Current / boss.Max : 1f;
        SetAnchor(fillRect, fillPct);
        SetAnchor(lagRect, lagPct);
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
