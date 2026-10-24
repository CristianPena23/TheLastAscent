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

    private void Start() => OnEnergyChanged?.Invoke(Current, maxEnergy);

    public void GainFromAttack() => Gain(gainPerHit);
    public void GainFromParry() => Gain(gainPerParry);

    public void Gain(int amount)
    {
        Current = Mathf.Clamp(Current + amount, 0, maxEnergy);
        OnEnergyChanged?.Invoke(Current, maxEnergy);
        Debug.Log($"Energía: {Current}/{maxEnergy}");
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