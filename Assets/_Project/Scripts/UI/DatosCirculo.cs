using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

// Nombre del círculo de la escena (pausa, cartel de entrada y ranuras de guardado).
// Opcional: si la escena no tiene este componente, se deduce del nombre (SC_Circulo9 -> "Círculo IX", "Traición").
public class DatosCirculo : MonoBehaviour
{
    [SerializeField] private string nombre = "Círculo IX";
    [Tooltip("Nombre que sale en grande al entrar (p. ej. \"Traición\"). Vacío = el de la Tabla I del documento IEEE según el número")]
    [SerializeField] private string titulo = "";

    // Nombres de los círculos del I al IX (documento IEEE, Tabla I "Estructura de los círculos")
    private static readonly string[] Titulos =
    {
        "", "Limbo", "Lujuria", "Gula", "Avaricia", "Ira", "Herejía", "Violencia", "Fraude", "Traición"
    };

    // "Círculo IX"
    public static string NombreActual()
    {
        var datos = FindFirstObjectByType<DatosCirculo>();
        if (datos != null) return datos.nombre;

        int n = NumeroActual();
        return n > 0 ? "Círculo " + Romano(n) : "";
    }

    // "Traición"
    public static string TituloActual()
    {
        var datos = FindFirstObjectByType<DatosCirculo>();
        if (datos != null && !string.IsNullOrEmpty(datos.titulo)) return datos.titulo;

        int n = NumeroActual();
        return n >= 1 && n < Titulos.Length ? Titulos[n] : "";
    }

    // 9 en SC_Circulo9; 0 si la escena no es un círculo (menú, arena del jefe, pruebas)
    public static int NumeroActual() => NumeroDeEscena(SceneManager.GetActiveScene().name);

    public static int NumeroDeEscena(string escena)
    {
        Match m = Regex.Match(escena ?? "", @"Circulo(\d+)", RegexOptions.IgnoreCase);
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    public static string Romano(int n)
    {
        string[] romanos = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
        return n >= 1 && n <= 9 ? romanos[n] : n.ToString();
    }
}
