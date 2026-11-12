using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú principal (escena SC_MenuPrincipal): Nueva partida, Continuar, Opciones, Salir.
public class MenuPrincipal : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private PanelUI panelPrincipal;
    [SerializeField] private PanelOpciones panelOpciones;

    [Header("Botones")]
    [SerializeField] private Button botonNuevaPartida;
    [Tooltip("Deshabilitado hasta que exista el sistema de guardado")]
    [SerializeField] private Button botonContinuar;
    [SerializeField] private Button botonOpciones;
    [SerializeField] private Button botonSalir;

    [Header("Inicio de partida")]
    [SerializeField] private string primeraEscena = "SC_Circulo9";
    [Tooltip("Id del SpawnPoint donde aparece el jugador. Vacío = posición del jugador en la escena")]
    [SerializeField] private string idSpawn = "";

    private bool iniciando;

    private void Awake()
    {
        if (botonNuevaPartida != null) botonNuevaPartida.onClick.AddListener(NuevaPartida);
        if (botonContinuar != null) botonContinuar.interactable = false; // TODO: activar cuando haya partida guardada
        if (botonOpciones != null) botonOpciones.onClick.AddListener(AbrirOpciones);
        if (botonSalir != null) botonSalir.onClick.AddListener(Salir);
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
    }

    public void NuevaPartida()
    {
        if (iniciando) return;
        iniciando = true;
        IntroCaida.OlvidarMostrados(); // partida nueva: la luciérnaga vuelve a hablar
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
