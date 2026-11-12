using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

// Nombre del círculo de la escena (HUD, minimapa y pausa).
// Opcional: si la escena no tiene este componente, se deduce del nombre (SC_Circulo9 -> "Círculo IX").
public class DatosCirculo : MonoBehaviour
{
    [SerializeField] private string nombre = "Círculo IX";

    public static string NombreActual()
    {
        var datos = FindFirstObjectByType<DatosCirculo>();
        if (datos != null) return datos.nombre;

        Match m = Regex.Match(SceneManager.GetActiveScene().name, @"Circulo(\d+)", RegexOptions.IgnoreCase);
        return m.Success ? "Círculo " + Romano(int.Parse(m.Groups[1].Value)) : "";
    }

    private static string Romano(int n)
    {
        string[] romanos = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
        return n >= 1 && n <= 9 ? romanos[n] : n.ToString();
    }
}
