using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Feedback de botón de menú: al seleccionarse (teclado, gamepad o mouse) el texto pasa a dorado,
// aparece el marcador y crece un poco. El hover del mouse también selecciona, así nunca hay dos resaltados.
[RequireComponent(typeof(Selectable))]
public class EfectoBotonUI : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
{
    [SerializeField] private TemaUI tema;
    [SerializeField] private TMP_Text etiqueta;
    [SerializeField] private GameObject marcador;
    [SerializeField] private float escalaSeleccionado = 1.08f;
    [SerializeField] private float velocidad = 1.5f;

    [Header("Sin tema asignado")]
    [SerializeField] private Color colorNormal = new Color(0.92f, 0.95f, 1f, 0.5f);
    [SerializeField] private Color colorSeleccionado = new Color(1f, 0.85f, 0.4f, 1f);

    private Selectable seleccionable;
    private AudioSource fuenteAudio;
    private bool seleccionado;
    private float escala = 1f;

    private Color Normal => tema != null ? tema.textoTenue : colorNormal;
    private Color Seleccionado => tema != null ? tema.resaltado : colorSeleccionado;

    private void Awake()
    {
        seleccionable = GetComponent<Selectable>();
        fuenteAudio = GetComponentInParent<AudioSource>();
        if (fuenteAudio != null) fuenteAudio.ignoreListenerPause = true;
    }

    private void OnEnable()
    {
        seleccionado = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
        escala = seleccionado ? escalaSeleccionado : 1f;
        transform.localScale = Vector3.one * escala;
        Aplicar(seleccionado);
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
        Aplicar(true);
        Sonar(tema != null ? tema.sonidoNavegar : null);
    }

    public void OnDeselect(BaseEventData e)
    {
        seleccionado = false;
        Aplicar(false);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (seleccionable.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSubmit(BaseEventData e) => Sonar(tema != null ? tema.sonidoAceptar : null);
    public void OnPointerClick(PointerEventData e) => Sonar(tema != null ? tema.sonidoAceptar : null);

    private void Aplicar(bool activo)
    {
        if (etiqueta != null) etiqueta.color = activo ? Seleccionado : Normal;
        if (marcador != null) marcador.SetActive(activo);
    }

    private void Sonar(AudioClip clip)
    {
        if (clip != null && fuenteAudio != null) fuenteAudio.PlayOneShot(clip);
    }
}
