using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Panel de menú con fade (en tiempo real, funciona en pausa) y selección inicial para teclado/gamepad.
[RequireComponent(typeof(CanvasGroup))]
public class PanelUI : MonoBehaviour
{
    [SerializeField] private Selectable primerSeleccionado;
    [SerializeField] private float tiempoFade = 0.2f;
    [SerializeField] private bool empiezaAbierto;

    private CanvasGroup grupo;
    private Coroutine fade;
    private bool inicializado; // evita que Awake cierre un panel que se activó con Abrir()

    public bool EstaAbierto { get; private set; }

    protected virtual void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        if (inicializado) return;
        inicializado = true;
        if (empiezaAbierto) Abrir(true);
        else Cerrar(true);
    }

    public void Abrir(bool instantaneo = false)
    {
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        inicializado = true;
        EstaAbierto = true;
        gameObject.SetActive(true);
        grupo.interactable = true;
        grupo.blocksRaycasts = true;
        IniciarFade(1f, instantaneo);
        Seleccionar(primerSeleccionado);
    }

    public void Cerrar(bool instantaneo = false)
    {
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        inicializado = true;
        EstaAbierto = false;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
        if (instantaneo || !gameObject.activeInHierarchy)
        {
            grupo.alpha = 0f;
            gameObject.SetActive(false);
            return;
        }
        IniciarFade(0f, false);
    }

    public static void Seleccionar(Selectable s)
    {
        if (s == null || EventSystem.current == null) return;
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(s.gameObject);
    }

    private void IniciarFade(float objetivo, bool instantaneo)
    {
        if (fade != null) StopCoroutine(fade);
        fade = null;
        if (instantaneo || tiempoFade <= 0f)
        {
            grupo.alpha = objetivo;
            if (objetivo <= 0f) gameObject.SetActive(false);
            return;
        }
        fade = StartCoroutine(Fade(objetivo));
    }

    private IEnumerator Fade(float objetivo)
    {
        float inicio = grupo.alpha, t = 0f;
        while (t < tiempoFade)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(inicio, objetivo, t / tiempoFade);
            yield return null;
        }
        grupo.alpha = objetivo;
        fade = null;
        if (objetivo <= 0f) gameObject.SetActive(false);
    }
}
