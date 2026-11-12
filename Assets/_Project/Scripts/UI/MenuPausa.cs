using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de pausa (Esc / Start): Continuar, Mapa, Habilidades, Opciones, Salir al menú.
// No se abre durante un diálogo, una transición de escena o con el jugador muerto.
public class MenuPausa : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private PanelUI panelPausa;
    [SerializeField] private PanelOpciones panelOpciones;

    [Header("Botones")]
    [SerializeField] private Button botonContinuar;
    [Tooltip("Deshabilitado hasta que exista el mapa")]
    [SerializeField] private Button botonMapa;
    [Tooltip("Deshabilitado hasta que exista la pantalla de habilidades")]
    [SerializeField] private Button botonHabilidades;
    [SerializeField] private Button botonOpciones;
    [SerializeField] private Button botonSalirMenu;

    [SerializeField] private string escenaMenu = "SC_MenuPrincipal";

    private PlayerHealth jugador;
    private DialogueManager dialogos;
    private bool saliendo;

    private void Awake()
    {
        if (botonContinuar != null) botonContinuar.onClick.AddListener(Continuar);
        if (botonMapa != null) botonMapa.interactable = false;               // TODO: abrir el mapa
        if (botonHabilidades != null) botonHabilidades.interactable = false; // TODO: abrir habilidades
        if (botonOpciones != null) botonOpciones.onClick.AddListener(AbrirOpciones);
        if (botonSalirMenu != null) botonSalirMenu.onClick.AddListener(SalirAlMenu);
    }

    private void Start()
    {
        jugador = FindFirstObjectByType<PlayerHealth>();
        dialogos = FindFirstObjectByType<DialogueManager>();
    }

    private void OnDestroy()
    {
        if (PausaJuego.EstaPausado) PausaJuego.Reanudar();
    }

    private void Update()
    {
        if (saliendo) return;

        if (!PausaJuego.EstaPausado)
        {
            if (EntradaUI.PausaPresionada() && PuedePausar()) Pausar();
            return;
        }

        if (panelOpciones != null && panelOpciones.EstaAbierto)
        {
            if (EntradaUI.AtrasPresionado() || EntradaUI.PausaPresionada()) panelOpciones.Volver();
            return;
        }

        if (EntradaUI.AtrasPresionado() || EntradaUI.PausaPresionada()) Continuar();
    }

    private bool PuedePausar()
    {
        if (SceneTransition.IsBusy) return false;
        if (jugador != null && jugador.IsDead) return false;
        if (dialogos != null && dialogos.EnDialogo) return false;
        return true;
    }

    public void Pausar()
    {
        PausaJuego.Pausar();
        if (panelPausa != null) panelPausa.Abrir();
    }

    public void Continuar()
    {
        if (panelOpciones != null && panelOpciones.EstaAbierto) panelOpciones.Cerrar(true);
        if (panelPausa != null) panelPausa.Cerrar();
        PausaJuego.Reanudar();
    }

    public void AbrirOpciones()
    {
        if (panelOpciones != null) panelOpciones.AbrirDesde(panelPausa);
    }

    // Se queda en pausa durante el fade; PausaJuego se reanuda solo al cargar la escena nueva
    public void SalirAlMenu()
    {
        saliendo = true;
        if (SceneTransition.Instance != null) SceneTransition.Go(escenaMenu, "");
        else SceneManager.LoadScene(escenaMenu);
    }
}
