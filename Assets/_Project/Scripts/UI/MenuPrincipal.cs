using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú principal (escena SC_MenuPrincipal): Jugar, Opciones, Salir.
public class MenuPrincipal : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private PanelUI panelPrincipal;
    [SerializeField] private PanelOpciones panelOpciones;

    [Header("Botones")]
    [SerializeField] private Button botonJugar;
    [SerializeField] private Button botonOpciones;
    [SerializeField] private Button botonSalir;

    [Header("Inicio de partida")]
    [SerializeField] private string primeraEscena = "SC_Circulo9";
    [Tooltip("Id del SpawnPoint donde aparece el jugador. Vacío = posición del jugador en la escena")]
    [SerializeField] private string idSpawn = "";

    private bool iniciando;

    private void Awake()
    {
        if (botonJugar != null) botonJugar.onClick.AddListener(Jugar);
        if (botonOpciones != null) botonOpciones.onClick.AddListener(AbrirOpciones);
        if (botonSalir != null) botonSalir.onClick.AddListener(Salir);
    }

    private void Start()
    {
        PausaJuego.Reanudar();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (panelPrincipal != null) panelPrincipal.Abrir(true);
    }

    private void Update()
    {
        if (panelOpciones != null && panelOpciones.EstaAbierto && EntradaUI.AtrasPresionado())
            panelOpciones.Volver();
    }

    public void Jugar()
    {
        if (iniciando) return;
        iniciando = true;
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
