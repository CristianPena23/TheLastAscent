using TMPro;
using UnityEngine;

// Paleta y fuente compartidas por toda la interfaz. Cambiar el arte = editar este asset.
// Crear: clic derecho > Create > Ultimo Descenso > Tema UI (o Tools > Ultimo Descenso > Configurar UI).
[CreateAssetMenu(menuName = "Ultimo Descenso/Tema UI", fileName = "UI_Tema")]
public class TemaUI : ScriptableObject
{
    [Header("Fondos")]
    public Color fondo = new Color(0.04f, 0.05f, 0.09f, 0.92f);
    public Color panel = new Color(0.07f, 0.09f, 0.15f, 0.96f);
    public Color velo = new Color(0f, 0f, 0f, 0.65f);

    [Header("Acentos")]
    [Tooltip("Azul hielo: vida, bordes, barras")]
    public Color acento = new Color(0.55f, 0.82f, 1f, 1f);
    [Tooltip("Dorado: energía, selección, daño retrasado")]
    public Color resaltado = new Color(1f, 0.85f, 0.4f, 1f);
    [Tooltip("Rojo apagado: muerte")]
    public Color peligro = new Color(0.62f, 0.1f, 0.12f, 1f);

    [Header("Texto")]
    public Color texto = new Color(0.92f, 0.95f, 1f, 1f);
    public Color textoTenue = new Color(0.92f, 0.95f, 1f, 0.5f);
    [Tooltip("Vacío = fuente por defecto de TextMeshPro")]
    public TMP_FontAsset fuente;

    [Header("Sonidos (opcional)")]
    public AudioClip sonidoNavegar;
    public AudioClip sonidoAceptar;
}
