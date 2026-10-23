using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private int maxHealth = 5;

    [Header("Golpe recibido")]
    [SerializeField] private float invulnerabilityTime = 1f;
    [SerializeField] private float knockbackForce = 12f;
    [SerializeField] private float knockbackUpForce = 6f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Muerte")]
    [SerializeField] private float respawnDelay = 1.5f;

    [Header("Feedback (opcional)")]
    [SerializeField] private SpriteRenderer blinkSprite;
    [SerializeField] private float blinkInterval = 0.08f;
    [SerializeField] private Animator animator;

    private static readonly int HurtHash = Animator.StringToHash("Hurt");

    public event Action<int, int> OnHealthChanged; // (actual, máxima) para el HUD
    public event Action OnDied;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsInvulnerable { get; private set; }
    public bool IsDead { get; private set; }

    private Rigidbody2D rb;
    private PlayerController player;
    private PlayerDash dash;
    private Vector3 spawnPosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
        dash = GetComponent<PlayerDash>();
        spawnPosition = transform.position;
        CurrentHealth = maxHealth;
    }

    private void Start() => OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

    // hitDirection: dirección del golpe (de quien golpea hacia quien lo recibe)
    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (IsDead || IsInvulnerable) return;
        if (dash != null && dash.IsDashing) return; // el dash esquiva el daño

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        Debug.Log($"Jugador recibió {amount} de daño. Vida: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0)
        {
            StartCoroutine(DieRoutine());
            return;
        }

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(HurtHash);

        StartCoroutine(KnockbackRoutine(hitDirection));
        StartCoroutine(InvulnerabilityRoutine());
    }

    private IEnumerator KnockbackRoutine(Vector2 hitDirection)
    {
        float dirX = Mathf.Abs(hitDirection.x) > 0.1f
            ? Mathf.Sign(hitDirection.x)
            : -player.FacingDirection; // golpe desde arriba o abajo: empuja hacia atrás

        player.InputLocked = true;
        rb.linearVelocity = new Vector2(dirX * knockbackForce, knockbackUpForce);
        yield return new WaitForSeconds(knockbackDuration);
        if (!IsDead) player.InputLocked = false;
    }

    private IEnumerator InvulnerabilityRoutine()
    {
        IsInvulnerable = true;
        float elapsed = 0f;
        while (elapsed < invulnerabilityTime)
        {
            if (blinkSprite != null) blinkSprite.enabled = !blinkSprite.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }
        if (blinkSprite != null) blinkSprite.enabled = true;
        IsInvulnerable = false;
    }

    private IEnumerator DieRoutine()
    {
        IsDead = true;
        player.InputLocked = true;
        rb.linearVelocity = Vector2.zero;
        OnDied?.Invoke();
        Debug.Log("El jugador murió");

        yield return new WaitForSeconds(respawnDelay);

        transform.position = spawnPosition;
        rb.linearVelocity = Vector2.zero;
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        if (blinkSprite != null) blinkSprite.enabled = true;
        IsInvulnerable = false;
        IsDead = false;
        player.InputLocked = false;
    }
}