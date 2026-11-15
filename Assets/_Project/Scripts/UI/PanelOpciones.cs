using UnityEngine;
using UnityEngine.UI;

// Opciones compartidas por el menú principal y la pausa. Se guardan en PlayerPrefs.
public class PanelOpciones : PanelUI
{
    private const string ClaveVolumen = "opc_volumen";
    private const string ClavePantallaCompleta = "opc_pantalla_completa";

    [SerializeField] private Slider sliderVolumen;
    [SerializeField] private Toggle togglePantallaCompleta;
    [SerializeField] private Button botonVolver;

    private PanelUI volverA;

    // Aplica lo guardado al iniciar el juego, aunque nunca se abra el panel
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AplicarGuardado()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(ClaveVolumen, 1f);
        if (PlayerPrefs.HasKey(ClavePantallaCompleta))
            Screen.fullScreen = PlayerPrefs.GetInt(ClavePantallaCompleta) == 1;
    }

    protected override void Awake()
    {
        base.Awake();
        if (sliderVolumen != null)
        {
            sliderVolumen.minValue = 0f;
            sliderVolumen.maxValue = 1f;
            sliderVolumen.SetValueWithoutNotify(PlayerPrefs.GetFloat(ClaveVolumen, 1f));
            sliderVolumen.onValueChanged.AddListener(AlCambiarVolumen);
        }
        if (togglePantallaCompleta != null)
        {
            togglePantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreen);
            togglePantallaCompleta.onValueChanged.AddListener(AlCambiarPantallaCompleta);
        }
        if (botonVolver != null) botonVolver.onClick.AddListener(Volver);
    }

    // Abre las opciones ocultando el panel de origen; Volver() regresa a él
    public void AbrirDesde(PanelUI origen)
    {
        volverA = origen;
        if (origen != null) origen.Cerrar(true);
        if (togglePantallaCompleta != null) togglePantallaCompleta.SetIsOnWithoutNotify(Screen.fullScreen);
        Abrir();
    }

    public void Volver()
    {
        SonidosUI.Volver();
        PlayerPrefs.Save();
        Cerrar(true);
        if (volverA != null) volverA.Abrir();
        volverA = null;
    }

    private void AlCambiarVolumen(float v)
    {
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(ClaveVolumen, v);
    }

    private void AlCambiarPantallaCompleta(bool activo)
    {
        Screen.fullScreen = activo;
        PlayerPrefs.SetInt(ClavePantallaCompleta, activo ? 1 : 0);
    }
}
