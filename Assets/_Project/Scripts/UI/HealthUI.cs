using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth; // opcional: si está vacío lo busca solo
    [SerializeField] private Image iconPrefab;
    [SerializeField] private Transform container;

    [Header("Sprites (se reemplazan con el arte final)")]
    [SerializeField] private Sprite fullSprite;
    [SerializeField] private Sprite emptySprite;

    [Header("Colores (con sprites propios, déjalos en blanco)")]
    [SerializeField] private Color fullColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.2f);

    private readonly List<Image> icons = new List<Image>();

    private void OnEnable()
    {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null) return;

        playerHealth.OnHealthChanged += Refresh;
        Refresh(playerHealth.CurrentHealth, playerHealth.MaxHealth);
    }

    private void OnDisable()
    {
        if (playerHealth != null) playerHealth.OnHealthChanged -= Refresh;
    }

    private void Refresh(int current, int max)
    {
        while (icons.Count < max)
            icons.Add(Instantiate(iconPrefab, container));

        for (int i = 0; i < icons.Count; i++)
        {
            Image icon = icons[i];
            icon.gameObject.SetActive(i < max);

            bool full = i < current;
            icon.sprite = full ? fullSprite : (emptySprite != null ? emptySprite : fullSprite);
            icon.color = full ? fullColor : emptyColor;
        }
    }
}