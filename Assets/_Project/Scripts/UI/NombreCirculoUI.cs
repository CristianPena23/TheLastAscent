using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Escribe el nombre del círculo actual. Si la escena no es un círculo, oculta el objeto indicado.
public class NombreCirculoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text texto;
    [Tooltip("Se oculta si no hay círculo (p. ej. la línea decorativa). Vacío = no oculta nada")]
    [SerializeField] private GameObject ocultarSiVacio;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += AlCargar;
        Actualizar();
    }

    private void OnDisable() => SceneManager.sceneLoaded -= AlCargar;

    private void AlCargar(Scene s, LoadSceneMode m) => Actualizar();

    private void Actualizar()
    {
        string nombre = DatosCirculo.NombreActual();
        if (texto != null) texto.text = nombre;
        if (ocultarSiVacio != null) ocultarSiVacio.SetActive(!string.IsNullOrEmpty(nombre));
    }
}
