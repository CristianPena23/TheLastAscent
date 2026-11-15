using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Feedback de opción de menú: al seleccionarse (teclado, gamepad o mouse) el texto se aclara
// y aparece la llama a su izquierda. El hover del mouse también selecciona, así nunca hay dos resaltados.
// Si el Selectable no es interactuable (p. ej. "Mapa" aún sin implementar) se ve apagado y se salta.
[RequireComponent(typeof(Selectable))]
public class EfectoBotonUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
{
    [SerializeField] private TemaUI tema; // vacío = tema del proyecto
    [SerializeField] private TMP_Text etiqueta;
    [SerializeField] private RectTransform marcador;
    [Tooltip("Coloca la llama justo a la izquierda del texto (botones centrados)")]
    [SerializeField] private bool marcadorJuntoAlTexto = true;
    [SerializeField] private float separacionMarcador = 22f;
    [SerializeField] private float escalaSeleccionado = 1.04f;
    [SerializeField] private float velocidad = 1f;

    private Selectable seleccionable;
    private bool seleccionado;
    private float escala = 1f;

    private TemaUI T => TemaUI.O(tema);

    private void Awake()
    {
        seleccionable = GetComponent<Selectable>();
    }

    private void OnEnable()
    {
        seleccionado = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        escala = seleccionado ? escalaSeleccionado : 1f;
        transform.localScale = Vector3.one * escala;
        Aplicar();
    }

    private void Update()
    {
        float objetivo = seleccionado ? escalaSeleccionado : 1f;
        if (Mathf.Approximately(escala, objetivo)) return;
        escala = Mathf.MoveTowards(escala, objetivo, velocidad * Time.unscaledDeltaTime);
        transform.localScale = Vector3.one * escala;
    }

    public void OnSelect(BaseEventData e)
    {
        seleccionado = true;
        Aplicar();
        SonidosUI.Navegar();
    }

    public void OnDeselect(BaseEventData e)
    {
        seleccionado = false;
        Aplicar();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (seleccionable.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSubmit(BaseEventData e) => SonidosUI.Aceptar();
    public void OnPointerClick(PointerEventData e)
    {
        if (seleccionable.IsInteractable()) SonidosUI.Aceptar();
    }

    private void Aplicar()
    {
        var t = T;
        bool activo = seleccionable == null || seleccionable.IsInteractable();
        if (etiqueta != null && t != null)
            etiqueta.color = !activo ? t.textoDeshabilitado : seleccionado ? t.textoSeleccionado : t.textoNormal;

        if (marcador == null) return;
        marcador.gameObject.SetActive(seleccionado && activo);
        if (marcadorJuntoAlTexto && etiqueta != null)
        {
            float ancho = Mathf.Min(etiqueta.preferredWidth, etiqueta.rectTransform.rect.width);
            marcador.anchoredPosition = new Vector2(-ancho * 0.5f - separacionMarcador - marcador.rect.width * 0.5f, marcador.anchoredPosition.y);
        }
    }
}
