using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Diálogo de entrada a una escena: cuando el jugador cae y toca el suelo, empieza a hablar
// (en el Círculo 9, la luciérnaga). Se configura solo: basta con asignar el DialogueData.
public class IntroCaida : MonoBehaviour
{
    [Header("Diálogo")]
    [SerializeField] DialogueManager dialogos; // vacío = lo busca en la escena
    [SerializeField] DialogueData dialogoIntro;

    [Header("Momento")]
    [Tooltip("Espera a que el jugador toque el suelo antes de hablar")]
    [SerializeField] bool esperarSuelo = true;
    [SerializeField] float esperaInicial = 0.5f;
    [SerializeField] float pausaTrasImpacto = 0.6f;
    [Tooltip("Solo la primera vez que se entra a la escena en la partida")]
    [SerializeField] bool soloUnaVez = true;

    static readonly HashSet<string> yaMostrados = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() => yaMostrados.Clear();

    // Partida nueva: los diálogos de entrada se vuelven a mostrar
    public static void OlvidarMostrados() => yaMostrados.Clear();

    // Guardado: qué diálogos de entrada ya salieron en la partida
    public static List<string> Mostrados() => new List<string>(yaMostrados);

    public static void MarcarMostrados(IEnumerable<string> claves)
    {
        if (claves == null) return;
        foreach (var c in claves) if (!string.IsNullOrEmpty(c)) yaMostrados.Add(c);
    }

    IEnumerator Start()
    {
        string clave = SceneManager.GetActiveScene().name + "/" + (dialogoIntro != null ? dialogoIntro.name : "");
        if (dialogoIntro == null || (soloUnaVez && yaMostrados.Contains(clave))) yield break;

        if (dialogos == null) dialogos = FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (dialogos == null)
        {
            Debug.LogWarning("IntroCaida: no hay DialogueManager en la escena.");
            yield break;
        }

        // Mientras cae ya no se controla al jugador
        PausaJuego.InputJugador(false);

        // Si el guion hace aparecer a la luciérnaga, empieza invisible
        if (System.Array.Exists(dialogoIntro.lines, l => l.accion == AccionDialogo.LuciernagaAparece))
        {
            var luciernaga = FindFirstObjectByType<Luciernaga>();
            if (luciernaga != null) luciernaga.Ocultar();
        }

        yield return new WaitForSeconds(esperaInicial);

        var jugador = FindFirstObjectByType<PlayerController>();
        if (esperarSuelo && jugador != null)
        {
            float limite = 6f; // por si nunca toca suelo, no se queda trabado
            while (!jugador.IsGrounded && limite > 0f)
            {
                limite -= Time.deltaTime;
                yield return null;
            }
        }

        yield return new WaitForSeconds(pausaTrasImpacto);

        yaMostrados.Add(clave);
        dialogos.Iniciar(dialogoIntro); // DialogueManager devuelve el control al terminar
        if (!dialogos.EnDialogo) PausaJuego.InputJugador(true);
    }
}
