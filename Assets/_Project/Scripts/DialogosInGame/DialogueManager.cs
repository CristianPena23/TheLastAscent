using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Muestra un DialogueData línea por línea con efecto de máquina de escribir.
// Espacio (o A/Cruz en el mando): completa el texto y luego pasa a la siguiente línea.
// Mantener Espacio: salta todo el diálogo. Mientras se habla, el jugador no se mueve ni ataca.
// Una línea puede disparar una acción en escena (AlAccion); si no tiene texto, la caja se oculta
// mientras ocurre la acción y luego sigue sola.
public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject caja;
    [SerializeField] TMP_Text textoNombre;
    [SerializeField] TMP_Text textoDialogo;
    [SerializeField] Image retrato; // opcional: se oculta si la línea no tiene retrato
    [SerializeField] AudioSource audioVoz;
    [Tooltip("Opcional: texto de ayuda con los controles")]
    [SerializeField] TMP_Text textoAyuda;

    [Header("Escritura")]
    [SerializeField] float segundosPorLetra = 0.03f;

    [Header("Controles")]
    [Tooltip("Segundos manteniendo Espacio para saltar todo el diálogo")]
    [SerializeField] float tiempoParaSaltar = 0.8f;
    [SerializeField] string ayudaTeclado = "Espacio: continuar   ·   Mantén Espacio: saltar";
    [SerializeField] string ayudaMando = "A: continuar   ·   Mantén A: saltar";

    DialogueData actual;
    int indice;
    bool escribiendo;
    Coroutine rutina;
    float tiempoPresionado;
    bool saltoDisparado;

    bool soloAccion;

    public bool EnDialogo { get; private set; }
    // true si cualquier DialogueManager está mostrando un diálogo (lo usa MusicaAmbiente para bajar el volumen)
    public static bool HayDialogo { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstatico() => HayDialogo = false;

    void OnDisable()
    {
        if (EnDialogo) HayDialogo = false;
    }

    // Acciones del guion (la luciérnaga aparece, pulsa, ilumina...). Las escucha quien las ejecuta.
    public static event Action<AccionDialogo> AlAccion;
    // Al cerrar un diálogo (también si se saltó)
    public static event Action AlTerminar;

    public void Iniciar(DialogueData dialogo)
    {
        if (dialogo == null || dialogo.lines == null || dialogo.lines.Length == 0) return;
        actual = dialogo;
        indice = 0;
        EnDialogo = true;
        HayDialogo = true;
        tiempoPresionado = 0f;
        saltoDisparado = true; // ignora una tecla que ya venía presionada
        PausaJuego.InputJugador(false);
        caja.SetActive(true);
        MostrarLinea();
    }

    void Update()
    {
        if (!EnDialogo || PausaJuego.EstaPausado) return;

        var teclado = Keyboard.current;
        var mando = Gamepad.current;
        bool presiono = (teclado != null && teclado.spaceKey.wasPressedThisFrame)
                     || (mando != null && mando.buttonSouth.wasPressedThisFrame);
        bool sostiene = (teclado != null && teclado.spaceKey.isPressed)
                     || (mando != null && mando.buttonSouth.isPressed);

        if (textoAyuda != null)
            textoAyuda.text = mando != null && (teclado == null || mando.lastUpdateTime > teclado.lastUpdateTime) ? ayudaMando : ayudaTeclado;

        if (!sostiene)
        {
            tiempoPresionado = 0f;
            saltoDisparado = false;
            return;
        }

        if (presiono && !saltoDisparado) Avanzar();

        tiempoPresionado += Time.unscaledDeltaTime;
        if (!saltoDisparado && tiempoPresionado >= tiempoParaSaltar)
        {
            saltoDisparado = true;
            Saltar();
        }
    }

    void MostrarLinea()
    {
        DialogueLine linea = actual.lines[indice];
        if (rutina != null) StopCoroutine(rutina);
        if (linea.accion != AccionDialogo.Ninguna) AlAccion?.Invoke(linea.accion);

        // Línea sin texto: solo la acción, sin caja
        if (string.IsNullOrWhiteSpace(linea.text))
        {
            caja.SetActive(false);
            soloAccion = true;
            rutina = StartCoroutine(EsperarAccion(linea.duracion));
            return;
        }

        soloAccion = false;
        caja.SetActive(true);
        textoNombre.text = linea.speaker;
        if (retrato != null)
        {
            retrato.sprite = linea.retrato;
            retrato.enabled = linea.retrato != null;
        }

        if (audioVoz != null)
        {
            audioVoz.Stop();
            if (linea.voice != null)
            {
                audioVoz.clip = linea.voice;
                audioVoz.Play();
            }
        }

        rutina = StartCoroutine(Escribir(linea.text));
    }

    IEnumerator EsperarAccion(float segundos)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, segundos));
        soloAccion = false;
        Siguiente();
    }

    IEnumerator Escribir(string texto)
    {
        escribiendo = true;
        textoDialogo.text = texto;
        textoDialogo.maxVisibleCharacters = 0;
        textoDialogo.ForceMeshUpdate();
        int visibles = textoDialogo.textInfo.characterCount; // sin contar etiquetas como <i>
        SonidosUI.ReiniciarEscritura();

        for (int i = 0; i < visibles; i++)
        {
            textoDialogo.maxVisibleCharacters = i + 1;
            SonidosUI.Tecla(textoDialogo.textInfo.characterInfo[i].character);
            yield return new WaitForSecondsRealtime(segundosPorLetra);
        }

        escribiendo = false;
    }

    public void Avanzar()
    {
        if (!EnDialogo) return;

        if (soloAccion) // Espacio durante una acción: no esperar a que termine
        {
            if (rutina != null) StopCoroutine(rutina);
            soloAccion = false;
            Siguiente();
            return;
        }

        if (escribiendo)
        {
            if (rutina != null) StopCoroutine(rutina);
            textoDialogo.maxVisibleCharacters = int.MaxValue;
            escribiendo = false;
            return;
        }

        Siguiente();
    }

    void Siguiente()
    {
        indice++;
        if (indice >= actual.lines.Length) Terminar();
        else MostrarLinea();
    }

    public void Saltar()
    {
        if (EnDialogo) Terminar();
    }

    void Terminar()
    {
        if (rutina != null) StopCoroutine(rutina);
        if (audioVoz != null) audioVoz.Stop();
        caja.SetActive(false);
        EnDialogo = false;
        HayDialogo = false;
        escribiendo = false;
        soloAccion = false;
        PausaJuego.InputJugador(true);
        AlTerminar?.Invoke();
    }
}
