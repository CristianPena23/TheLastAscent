using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Poder especial: con la barra de energía llena, C (o Y en el mando) gasta toda la energía
// y vuelve al jugador inmune a los ataques durante unos segundos. Mientras dura, los golpes y
// los parrys recargan la mitad de energía. La primera vez que la barra se llena en el Círculo 9
// se muestra un aviso explicando el poder; después solo titila la barra (EnergyUI).
// Va en el jugador junto a PlayerEnergy (si falta, PlayerEnergy lo agrega solo).
[RequireComponent(typeof(PlayerEnergy))]
public class PoderInmunidad : MonoBehaviour
{
    [Header("Inmunidad")]
    [SerializeField] float duracion = 5f;
    [Tooltip("Multiplicador de energía ganada mientras es inmune")]
    [Range(0f, 1f)]
    [SerializeField] float energiaDuranteInmunidad = 0.5f;

    [Header("Aspecto del jugador")]
    [SerializeField] Color colorInmune = new Color(0.78f, 0.62f, 1f, 1f);
    [SerializeField] float velocidadBrillo = 6f;
    [Tooltip("Último tramo: parpadea más rápido para avisar que se acaba")]
    [SerializeField] float avisoFinal = 1f;

    [Header("Aviso la primera vez")]
    [SerializeField] string escenaTutorial = "SC_Circulo9";

    static bool avisoMostrado;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() => avisoMostrado = false;

    // Partida nueva: el aviso vuelve a salir
    public static void OlvidarAviso() => avisoMostrado = false;

    // Guardado: si el aviso ya salió en esta partida
    public static bool AvisoMostrado
    {
        get => avisoMostrado;
        set => avisoMostrado = value;
    }

    public bool Activa { get; private set; }
    public float Duracion => duracion;

    PlayerEnergy energia;
    PlayerHealth salud;
    SpriteRenderer[] sprites;
    Color[] coloresBase;
    InputAction accion;
    int energiaPrevia;

    void Awake()
    {
        energia = GetComponent<PlayerEnergy>();
        salud = GetComponent<PlayerHealth>();
        accion = new InputAction("Inmunidad", InputActionType.Button);
        accion.AddBinding("<Keyboard>/c");
        accion.AddBinding("<Gamepad>/buttonNorth");
    }

    void OnEnable()
    {
        accion.Enable();
        accion.performed += AlPresionar;
        energia.OnEnergyChanged += AlCambiarEnergia;
        energiaPrevia = energia.Current;
    }

    void OnDisable()
    {
        accion.performed -= AlPresionar;
        accion.Disable();
        energia.OnEnergyChanged -= AlCambiarEnergia;
        if (Activa) Terminar();
    }

    void OnDestroy() => accion.Dispose();

    void AlCambiarEnergia(int actual, int maxima)
    {
        bool seLleno = actual >= maxima && energiaPrevia < maxima;
        energiaPrevia = actual;
        if (!seLleno || avisoMostrado) return;
        if (SceneManager.GetActiveScene().name != escenaTutorial) return;

        avisoMostrado = true;
        AvisoPoderUI.Mostrar(duracion);
    }

    void AlPresionar(InputAction.CallbackContext ctx)
    {
        if (!PuedeActivar()) return;
        energia.TrySpend(energia.Max);
        StartCoroutine(Inmunidad());
    }

    bool PuedeActivar()
    {
        if (Activa || energia.Current < energia.Max) return false;
        if (salud != null && salud.IsDead) return false;
        // Sin control del jugador (pausa, diálogo, caída de la intro) no se activa
        var mapa = InputSystem.actions != null ? InputSystem.actions.FindActionMap("Player") : null;
        return mapa == null || mapa.enabled;
    }

    IEnumerator Inmunidad()
    {
        Activa = true;
        energia.MultiplicadorGanancia = energiaDuranteInmunidad;
        AvisoPoderUI.Ocultar();

        sprites = GetComponentsInChildren<SpriteRenderer>();
        coloresBase = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) coloresBase[i] = sprites[i].color;

        float t = 0f;
        while (t < duracion && !(salud != null && salud.IsDead))
        {
            t += Time.deltaTime;
            bool final = duracion - t <= avisoFinal;
            float vel = final ? velocidadBrillo * 3f : velocidadBrillo;
            float k = (Mathf.Sin(Time.time * vel) + 1f) * 0.5f;
            for (int i = 0; i < sprites.Length; i++)
                if (sprites[i] != null) sprites[i].color = Color.Lerp(coloresBase[i], colorInmune, final ? k : 0.5f + k * 0.5f);
            yield return null;
        }
        Terminar();
    }

    void Terminar()
    {
        StopAllCoroutines();
        Activa = false;
        energia.MultiplicadorGanancia = 1f;
        if (sprites == null) return;
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = coloresBase[i];
    }
}
