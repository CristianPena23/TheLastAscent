using System;
using System.Collections.Generic;
using UnityEngine;

// Llaves que lleva el jugador. Es estatico: se conserva al cambiar de escena durante la partida.
public static class KeyInventory
{
    private static readonly HashSet<string> keys = new HashSet<string>();

    public static event Action OnChanged;

    public static bool Has(string id) => string.IsNullOrEmpty(id) || keys.Contains(id);

    public static void Add(string id)
    {
        if (string.IsNullOrEmpty(id) || !keys.Add(id)) return;
        OnChanged?.Invoke();
    }

    public static void Remove(string id)
    {
        if (keys.Remove(id)) OnChanged?.Invoke();
    }

    public static void Clear()
    {
        keys.Clear();
        OnChanged?.Invoke();
    }

    // Al iniciar Play (por si el Domain Reload esta desactivado)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        keys.Clear();
        OnChanged = null;
    }
}
