using TMPro;
using UnityEngine;

// Paleta, fuentes y arte compartidos por toda la interfaz (documento IEEE, sección "Interfaz de usuario":
// marcos de hueso, tipografía serif clásica, fondos oscuros y acentos en rojo brasa).
// Vive en Assets/_Project/Resources/UI_Tema.asset y se carga solo: no hace falta asignarlo a mano.
[CreateAssetMenu(menuName = "Ultimo Descenso/Tema UI", fileName = "UI_Tema")]
public class TemaUI : ScriptableObject
{
    private const string RutaResources = "UI_Tema";
    private static TemaUI cache;

    // El tema del proyecto (Resources/UI_Tema)
    public static TemaUI Predeterminado
    {
        get
        {
            if (cache == null) cache = Resources.Load<TemaUI>(RutaResources);
            return cache;
        }
    }

    // Devuelve el tema asignado o, si está vacío, el del proyecto
    public static TemaUI O(TemaUI asignado) => asignado != null ? asignado : Predeterminado;

    [Header("Fondos")]
    public Color fondo = new Color(0.05f, 0.043f, 0.035f, 0.96f);
    public Color velo = new Color(0f, 0f, 0f, 0.72f);

    [Header("Texto (hueso / pergamino)")]
    public Color texto = new Color(0.91f, 0.86f, 0.78f, 1f);
    public Color textoNormal = new Color(0.80f, 0.75f, 0.68f, 1f);
    public Color textoSeleccionado = new Color(0.98f, 0.86f, 0.74f, 1f);
    public Color textoDeshabilitado = new Color(0.55f, 0.50f, 0.45f, 0.45f);

    [Header("Acentos")]
    [Tooltip("Rojo brasa: selección, detalles")]
    public Color brasa = new Color(0.85f, 0.27f, 0.12f, 1f);
    public Color vida = new Color(0.66f, 0.06f, 0.07f, 1f);
    [Tooltip("Violeta: barra de energía")]
    public Color energia = new Color(0.36f, 0.26f, 0.62f, 1f);
    public Color energiaLlena = new Color(0.62f, 0.50f, 1f, 1f);
    public Color vidaJefe = new Color(0.66f, 0.13f, 0.02f, 1f);
    public Color vidaJefeRetraso = new Color(0.95f, 0.55f, 0.25f, 1f);

    [Header("Fuentes")]
    [Tooltip("Títulos (Cinzel)")]
    public TMP_FontAsset fuenteTitulo;
    public Material materialTitulo;
    [Tooltip("Texto y botones (EB Garamond)")]
    public TMP_FontAsset fuenteTexto;
    public Material materialTexto;

    [Header("Arte (temporal: recortes de los mockups)")]
    [Tooltip("Llama que marca la opción seleccionada")]
    public Sprite marcador;
    public Sprite separador;
    [Tooltip("Marco de hueso con la máscara (pausa y ranuras de guardado)")]
    public Sprite marcoPanel;
    public Sprite marcoBarraJefe;
    public Sprite placaJefe;

    [Header("Sonidos (opcional)")]
    public AudioClip sonidoNavegar;
    public AudioClip sonidoAceptar;
}
