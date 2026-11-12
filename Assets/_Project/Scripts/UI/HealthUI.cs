using System.Collections;
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

    [Header("Tema (opcional: si se asigna, usa sus colores)")]
    [SerializeField] private TemaUI tema;

    [Header("Pulso al perder vida")]
    [SerializeField] private float escalaPulso = 1.45f;
    [SerializeField] private float tiempoPulso = 0.25f;

    private readonly List<Image> icons = new List<Image>();
    private int previo = -1;

    private void Awake()
    {
        if (tema == null) return;
        fullColor = tema.acento;
        emptyColor = new Color(tema.textoTenue.r, tema.textoTenue.g, tema.textoTenue.b, 0.2f);
    }

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

            // Íconos que se acaban de vaciar: pulso para que se note el golpe
            if (previo >= 0 && i >= current && i < previo && icon.gameObject.activeInHierarchy)
                StartCoroutine(Pulso(icon.rectTransform));
        }
        previo = current;
    }

    private IEnumerator Pulso(RectTransform r)
    {
        float t = 0f;
        while (t < tiempoPulso)
        {
            t += Time.unscaledDeltaTime;
            r.localScale = Vector3.one * Mathf.Lerp(escalaPulso, 1f, t / tiempoPulso);
            yield return null;
        }
        r.localScale = Vector3.one;
    }
}