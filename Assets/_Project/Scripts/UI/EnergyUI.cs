using UnityEngine;
using UnityEngine.UI;

public class EnergyUI : MonoBehaviour
{
    [SerializeField] private PlayerEnergy playerEnergy; // opcional: si está vacío lo busca solo
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Color (vacío = tema del proyecto: violeta)")]
    [SerializeField] private TemaUI tema;
    [SerializeField] private Color colorRelleno = new Color(0.36f, 0.26f, 0.62f, 1f);
    [SerializeField] private Color colorBrilloLlena = new Color(0.62f, 0.5f, 1f, 1f);
    [SerializeField] private float velocidadBrillo = 4f;

    private Image fillImage;
    private bool llena;

    private float targetFill;
    private float currentFill;

    private void Awake()
    {
        if (fillRect == null) return;
        var t = TemaUI.O(tema);
        if (t != null) { colorRelleno = t.energia; colorBrilloLlena = t.energiaLlena; }
        fillImage = fillRect.GetComponent<Image>();
        if (fillImage != null) fillImage.color = colorRelleno;
        fillRect.anchorMin = Vector2.zero;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Apply(0f);
    }

    private void OnEnable()
    {
        if (playerEnergy == null) playerEnergy = FindFirstObjectByType<PlayerEnergy>();
        if (playerEnergy == null) return;

        playerEnergy.OnEnergyChanged += OnChanged;
        OnChanged(playerEnergy.Current, playerEnergy.Max);
    }

    private void OnDisable()
    {
        if (playerEnergy != null) playerEnergy.OnEnergyChanged -= OnChanged;
    }

    private void OnChanged(int current, int max)
    {
        targetFill = max > 0 ? (float)current / max : 0f;
        llena = max > 0 && current >= max;
        if (!llena && fillImage != null) fillImage.color = colorRelleno;
    }

    private void Update()
    {
        // Energía llena: la barra late entre violeta y violeta claro
        if (llena && fillImage != null)
        {
            float k = (Mathf.Sin(Time.unscaledTime * velocidadBrillo) + 1f) * 0.5f;
            fillImage.color = Color.Lerp(colorRelleno, colorBrilloLlena, k);
        }

        if (fillRect == null || Mathf.Approximately(currentFill, targetFill)) return;
        // Tiempo sin escala: la barra sigue moviéndose durante el hit stop del parry
        currentFill = Mathf.MoveTowards(currentFill, targetFill, smoothSpeed * Time.unscaledDeltaTime);
        Apply(currentFill);
    }

    private void Apply(float t) => fillRect.anchorMax = new Vector2(t, 1f);
}