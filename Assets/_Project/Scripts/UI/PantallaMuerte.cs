using System.Collections;
using TMPro;
using UnityEngine;

// Pantalla de muerte: aparece cuando el jugador muere y se va sola al reaparecer con la vida llena.
// No cambia la lógica de respawn de PlayerHealth (respawnDelay).
[RequireComponent(typeof(CanvasGroup))]
public class PantallaMuerte : MonoBehaviour
{
    [SerializeField] private PlayerHealth jugador; // opcional: si está vacío lo busca solo
    [SerializeField] private TMP_Text textoMensaje;
    [SerializeField] private string[] mensajes = { "La culpa te arrastra de nuevo" };

    [Header("Tiempos (segundos reales)")]
    [SerializeField] private float retraso = 0.25f;
    [SerializeField] private float tiempoEntrada = 0.6f;
    [SerializeField] private float tiempoSalida = 0.5f;

    private CanvasGroup grupo;
    private Coroutine rutina;
    private bool visible;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;
    }

    private void OnEnable()
    {
        if (jugador == null) jugador = FindFirstObjectByType<PlayerHealth>();
        if (jugador == null) return;
        jugador.OnDied += AlMorir;
        jugador.OnHealthChanged += AlCambiarVida;
    }

    private void OnDisable()
    {
        if (jugador == null) return;
        jugador.OnDied -= AlMorir;
        jugador.OnHealthChanged -= AlCambiarVida;
    }

    private void AlMorir()
    {
        if (textoMensaje != null && mensajes != null && mensajes.Length > 0)
            textoMensaje.text = mensajes[Random.Range(0, mensajes.Length)];
        visible = true;
        Iniciar(Fade(1f, tiempoEntrada, retraso));
    }

    private void AlCambiarVida(int actual, int maxima)
    {
        if (visible && actual >= maxima)
        {
            visible = false;
            Iniciar(Fade(0f, tiempoSalida, 0f));
        }
    }

    private void Iniciar(IEnumerator r)
    {
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(r);
    }

    private IEnumerator Fade(float objetivo, float tiempo, float espera)
    {
        if (espera > 0f) yield return new WaitForSecondsRealtime(espera);
        float inicio = grupo.alpha, t = 0f;
        while (t < tiempo)
        {
            t += Time.unscaledDeltaTime;
            grupo.alpha = Mathf.Lerp(inicio, objetivo, t / tiempo);
            yield return null;
        }
        grupo.alpha = objetivo;
        rutina = null;
    }
}
