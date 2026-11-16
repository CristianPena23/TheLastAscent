using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

// Menú principal (escena SC_MenuPrincipal): Nueva partida (con la cinemática de inicio), Continuar, Opciones, Salir.
// "Continuar" abre las ranuras de guardado para cargar una partida (se apaga si no hay ninguna).
public class MenuPrincipal : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private PanelUI panelPrincipal;
    [SerializeField] private PanelOpciones panelOpciones;

    [Header("Botones")]
    [SerializeField] private Button botonNuevaPartida;
    [Tooltip("Abre las ranuras para cargar. Deshabilitado si no hay partidas guardadas")]
    [SerializeField] private Button botonContinuar;
    [SerializeField] private Button botonOpciones;
    [SerializeField] private Button botonSalir;

    [Header("Inicio de partida")]
    [SerializeField] private string primeraEscena = "SC_Circulo9";
    [Tooltip("Id del SpawnPoint donde aparece el jugador. Vacío = posición del jugador en la escena")]
    [SerializeField] private string idSpawn = "";
    [Tooltip("Video antes de la primera escena. Vacío = el video de Resources/Cinematicas; si tampoco hay, se entra directo")]
    [SerializeField] private VideoClip cinematicaInicio;

    private bool iniciando;
    private PanelRanuras panelCargar;

    private void Awake()
    {
        if (botonNuevaPartida != null) botonNuevaPartida.onClick.AddListener(NuevaPartida);
        if (botonOpciones != null) botonOpciones.onClick.AddListener(AbrirOpciones);
        if (botonSalir != null) botonSalir.onClick.AddListener(Salir);

        Button plantilla = botonOpciones != null ? botonOpciones : botonNuevaPartida;
        if (plantilla != null)
        {
            panelCargar = PanelRanuras.Crear(transform, plantilla, PanelRanuras.Modo.Cargar);
            panelCargar.AlCargarPartida += () => iniciando = true;
        }
        if (botonContinuar != null)
        {
            botonContinuar.onClick.AddListener(AbrirCargar);
            botonContinuar.interactable = panelCargar != null && SistemaGuardado.HayPartidas();
        }
    }

    private void Start()
    {
        PausaJuego.Reanudar();
        Almas.Reiniciar();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (panelPrincipal != null) panelPrincipal.Abrir(true);
    }

    private void Update()
    {
        if (panelOpciones != null && panelOpciones.EstaAbierto && EntradaUI.AtrasPresionado())
            panelOpciones.Volver();
        else if (panelCargar != null && panelCargar.EstaAbierto && EntradaUI.AtrasPresionado())
            panelCargar.Volver();
    }

    public void AbrirCargar()
    {
        if (iniciando || panelCargar == null) return;
        panelCargar.AbrirDesde(panelPrincipal);
    }

    public void NuevaPartida()
    {
        if (iniciando) return;
        iniciando = true;
        // Partida nueva: almas, llaves y tiempo a cero; la luciérnaga vuelve a hablar
        // y el aviso del poder vuelve a salir en el Círculo 9
        SistemaGuardado.NuevaPartida();
        // Primero la cinemática de inicio (cuenta la historia) y luego el Círculo 9
        CinematicaInicio.Reproducir(cinematicaInicio, EntrarAPrimeraEscena);
    }

    private void EntrarAPrimeraEscena()
    {
        if (SceneTransition.Instance != null) SceneTransition.Go(primeraEscena, idSpawn);
        else SceneManager.LoadScene(primeraEscena);
    }

    public void AbrirOpciones()
    {
        if (panelOpciones != null) panelOpciones.AbrirDesde(panelPrincipal);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
