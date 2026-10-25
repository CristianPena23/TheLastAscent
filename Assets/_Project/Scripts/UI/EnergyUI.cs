using UnityEngine;

public class EnergyUI : MonoBehaviour
{
    [SerializeField] private PlayerEnergy playerEnergy; // opcional: si está vacío lo busca solo
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private float smoothSpeed = 8f;

    private float targetFill;
    private float currentFill;

    private void Awake()
    {
        if (fillRect == null) return;
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
    }

    private void Update()
    {
        if (fillRect == null || Mathf.Approximately(currentFill, targetFill)) return;
        // Tiempo sin escala: la barra sigue moviéndose durante el hit stop del parry
        currentFill = Mathf.MoveTowards(currentFill, targetFill, smoothSpeed * Time.unscaledDeltaTime);
        Apply(currentFill);
    }

    private void Apply(float t) => fillRect.anchorMax = new Vector2(t, 1f);
}