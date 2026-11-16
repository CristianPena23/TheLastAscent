using System;
using UnityEngine;

// Contador global de almas (la moneda del HUD). Uso: Almas.Sumar(25); Almas.Gastar(100);
public static class Almas
{
    public static int Cantidad { get; private set; }
    public static event Action<int> AlCambiar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimpiarEstaticos()
    {
        Cantidad = 0;
        AlCambiar = null;
    }

    // Partida nueva
    public static void Reiniciar()
    {
        Cantidad = 0;
        AlCambiar?.Invoke(Cantidad);
    }

    // Cargar partida
    public static void Establecer(int cantidad)
    {
        Cantidad = Mathf.Max(0, cantidad);
        AlCambiar?.Invoke(Cantidad);
    }

    public static void Sumar(int cantidad)
    {
        if (cantidad <= 0) return;
        Cantidad += cantidad;
        AlCambiar?.Invoke(Cantidad);
    }

    public static bool Gastar(int cantidad)
    {
        if (cantidad <= 0 || Cantidad < cantidad) return false;
        Cantidad -= cantidad;
        AlCambiar?.Invoke(Cantidad);
        return true;
    }
}
