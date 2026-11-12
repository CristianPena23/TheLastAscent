using UnityEngine.InputSystem;

// Teclas de menú leídas directo del dispositivo (funcionan aunque el mapa "Player" esté apagado).
public static class EntradaUI
{
    // Esc o Start: abrir/cerrar pausa
    public static bool PausaPresionada()
    {
        var teclado = Keyboard.current;
        var mando = Gamepad.current;
        return (teclado != null && teclado.escapeKey.wasPressedThisFrame)
            || (mando != null && mando.startButton.wasPressedThisFrame);
    }

    // Esc o B/Círculo: volver atrás en un menú
    public static bool AtrasPresionado()
    {
        var teclado = Keyboard.current;
        var mando = Gamepad.current;
        return (teclado != null && teclado.escapeKey.wasPressedThisFrame)
            || (mando != null && mando.buttonEast.wasPressedThisFrame);
    }
}
