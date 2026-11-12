using UnityEngine;
using UnityEngine.UI;

public class EnergyUI : MonoBehaviour
{
    [SerializeField] private PlayerEnergy playerEnergy; // opcional: si está vacío lo busca solo
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private float smoothSpeed = 8f;

    [Header("Color (tema opcional: si se asigna, usa su dorado)")]
    [SerializeField] private TemaUI tema;
    [SerializeField] private Color colorRelleno = new Color(1f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color colorBrilloLlena = Color.white;
    [SerializeField] private float velocidadBrillo = 4f;

    private Image fillImage;
    private bool llena;

    private float targetFill;
    private float currentFill;

    private void Awake()
    {
        if (fillRect == null) return;
        if (tema != null) colorRelleno = tema.resaltado;
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
        // Energía llena: la barra late entre dorado y blanco
        if (llena && fillImage != null)
        {
            float k = (Mathf.Sin(Time.unscaledTime * velocidadBrillo) + 1f) * 0.5f;
            fillImage.color = Color.Lerp(colorRelleno, colorBrilloLlena, k * 0.6f);
        }

        if (fillRect == null || Mathf.Approximately(currentFill, targetFill)) return;
        // Tiempo sin escala: la barra sigue moviéndose durante el hit stop del parry
        currentFill = Mathf.MoveTowards(currentFill, targetFill, smoothSpeed * Time.unscaledDeltaTime);
        Apply(currentFill);
    }

    private void Apply(float t) => fillRect.anchorMax = new Vector2(t, 1f);
}