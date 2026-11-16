using System;
using UnityEngine;

public class PlayerEnergy : MonoBehaviour
{
    [SerializeField] private int maxEnergy = 99;
    [SerializeField] private int gainPerHit = 11;
    [SerializeField] private int gainPerParry = 16;

    public event Action<int, int> OnEnergyChanged; // (actual, máxima) para el HUD

    public int Current { get; private set; }
    public int Max => maxEnergy;

    // Lo baja PoderInmunidad mientras el jugador es inmune (0.5 = la mitad de energia por golpe/parry)
    public float MultiplicadorGanancia { get; set; } = 1f;

    // El poder de inmunidad (C) usa esta energia: si el prefab no lo trae, se agrega solo
    private void Awake()
    {
        if (GetComponent<PoderInmunidad>() == null) gameObject.AddComponent<PoderInmunidad>();
    }

    private void Start() => OnEnergyChanged?.Invoke(Current, maxEnergy);

    public void GainFromAttack() => Gain(Mathf.RoundToInt(gainPerHit * MultiplicadorGanancia));
    public void GainFromParry() => Gain(Mathf.RoundToInt(gainPerParry * MultiplicadorGanancia));

    public void Gain(int amount)
    {
        Current = Mathf.Clamp(Current + amount, 0, maxEnergy);
        OnEnergyChanged?.Invoke(Current, maxEnergy);
        Debug.Log($"Energía: {Current}/{maxEnergy}");
    }

    // Devuelve true si realmente recargo (false si ya estaba llena)
    public bool RefillToFull()
    {
        if (Current >= maxEnergy) return false;
        Current = maxEnergy;
        OnEnergyChanged?.Invoke(Current, maxEnergy);
        return true;
    }

    // Cargar partida: deja la energia guardada
    public void Establecer(int energia)
    {
        Current = Mathf.Clamp(energia, 0, maxEnergy);
        OnEnergyChanged?.Invoke(Current, maxEnergy);
    }

    public bool CanSpend(int amount) => Current >= amount;

    public bool TrySpend(int amount)
    {
        if (Current < amount) return false;
        Current -= amount;
        OnEnergyChanged?.Invoke(Current, maxEnergy);
        return true;
    }
}