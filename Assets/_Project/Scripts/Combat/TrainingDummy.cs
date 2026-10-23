using System.Collections;
using UnityEngine;

public class TrainingDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 5;
    [SerializeField] private float resetDelay = 1.5f;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    private int health;
    private Color baseColor = Color.white;
    private Coroutine flashRoutine;

    private void Awake()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;
        health = maxHealth;
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (health <= 0) return;

        health -= amount;
        Debug.Log($"{name} recibió {amount} de daño. Vida: {Mathf.Max(health, 0)}/{maxHealth}");

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());

        if (health <= 0) StartCoroutine(ResetRoutine());
    }

    private IEnumerator Flash()
    {
        if (sprite != null) sprite.color = hitColor;
        yield return new WaitForSeconds(flashDuration);
        if (sprite != null) sprite.color = baseColor;
    }

    private IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(flashDuration + 0.05f);
        if (sprite != null) sprite.color = Color.gray;
        yield return new WaitForSeconds(resetDelay);
        health = maxHealth;
        if (sprite != null) sprite.color = baseColor;
    }
}