using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Estado global de pausa. Congela el tiempo, el audio del juego y el input del jugador.
// Uso: PausaJuego.Pausar(); PausaJuego.Reanudar(); if (PausaJuego.EstaPausado) ...
public static class PausaJuego
{
    public static bool EstaPausado { get; private set; }
    public static event Action<bool> AlCambiar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reiniciar()
    {
        EstaPausado = false;
        AlCambiar = null;
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    // Al cambiar de escena nunca seguimos en pausa
    private static void AlCargarEscena(Scene escena, LoadSceneMode modo) => Reanudar();

    public static void Pausar()
    {
        if (EstaPausado) return;
        EstaPausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true; // las fuentes de audio de la UI usan ignoreListenerPause
        InputJugador(false);
        AlCambiar?.Invoke(true);
    }

    public static void Reanudar()
    {
        if (!EstaPausado) return;
        EstaPausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        InputJugador(true);
        AlCambiar?.Invoke(false);
    }

    // El jugador lee sus acciones del asset de acciones del proyecto (InputSystem_Actions).
    // Apagar el mapa "Player" evita que salto/ataque/parry/dash se disparen en pausa.
    private static void InputJugador(bool activo)
    {
        InputActionMap mapa = InputSystem.actions != null ? InputSystem.actions.FindActionMap("Player") : null;
        if (mapa == null) return;
        if (activo) mapa.Enable();
        else mapa.Disable();
    }
}
