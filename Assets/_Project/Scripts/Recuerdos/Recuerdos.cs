using System;
using System.Collections.Generic;

// Recuerdos del accidente ("fragmentos de luz", documento IEEE) que sueltan los jefes.
// Guarda cuáles ya recuperó el jugador (entra en las partidas guardadas) y el texto de cada uno.
// Uso: Recuerdos.Tiene("recuerdo_1"); Recuerdos.Agregar("recuerdo_1"); Recuerdos.DeJefe("Gaki");
public static class Recuerdos
{
    private static readonly HashSet<string> recuperados = new HashSet<string>();

    public static event Action<DatosRecuerdo> AlRecuperar;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimpiarEstaticos()
    {
        recuperados.Clear();
        AlRecuperar = null;
    }

    public static bool Tiene(string id) => !string.IsNullOrEmpty(id) && recuperados.Contains(id);
    public static int Cantidad => recuperados.Count;
    public static List<string> Todos() => new List<string>(recuperados);

    public static void Agregar(DatosRecuerdo datos)
    {
        if (datos == null || string.IsNullOrEmpty(datos.id)) return;
        if (recuperados.Add(datos.id)) AlRecuperar?.Invoke(datos);
    }

    public static void Olvidar() => recuperados.Clear();

    public static void Marcar(IEnumerable<string> ids)
    {
        if (ids == null) return;
        foreach (var id in ids) if (!string.IsNullOrEmpty(id)) recuperados.Add(id);
    }

    // ---------- Catálogo ----------
    // Un recuerdo por jefe, del Círculo IX (Gaki) al I (Oni). Por ahora solo el primero.

    private static readonly DatosRecuerdo[] Catalogo =
    {
        new DatosRecuerdo
        {
            id = "recuerdo_1",
            jefe = "Gaki",
            numero = 1,
            circulo = 9,
            titulo = "El balón",
            lineas = new[]
            {
                "Jugábamos fútbol en la acera, como todas las tardes.",
                "Él tenía el balón y no me lo quería soltar.",
                "Lo empujé para quitárselo. Solo quería ganar.",
                "Trastabilló hacia la calle.",
                "Después, los frenos... y el golpe."
            }
        },
    };

    public static DatosRecuerdo DeJefe(string jefe)
    {
        foreach (var r in Catalogo)
            if (string.Equals(r.jefe, jefe, StringComparison.OrdinalIgnoreCase)) return r;
        return null;
    }

    public static DatosRecuerdo PorId(string id)
    {
        foreach (var r in Catalogo)
            if (r.id == id) return r;
        return null;
    }
}

[Serializable]
public class DatosRecuerdo
{
    public string id = "recuerdo_1";
    public string jefe = "";
    public int numero = 1;  // primer, segundo... recuerdo
    public int circulo = 9; // círculo donde se recupera
    public string titulo = "";
    [UnityEngine.TextArea] public string[] lineas = new string[0];

    public string Ordinal()
    {
        string[] o = { "", "Primer", "Segundo", "Tercer", "Cuarto", "Quinto", "Sexto", "Séptimo", "Octavo", "Noveno" };
        return numero >= 1 && numero < o.Length ? o[numero] + " recuerdo" : "Recuerdo " + numero;
    }
}
