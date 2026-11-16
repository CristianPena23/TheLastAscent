using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerController))]
public class PlayerParry : MonoBehaviour
{
    [Header("Parry")]
    [SerializeField] private float parryWindow = 0.15f;
    [SerializeField] private float missCooldown = 0.6f;
    [SerializeField] private float successCooldown = 0.15f;

    [Header("Feedback")]
    [SerializeField] private float hitStopDuration = 0.08f;
    [SerializeField] private float hitStopTimeScale = 0.05f;
    [SerializeField] private SpriteRenderer parryIndicator;
    [SerializeField] private Color windowColor = new Color(0.3f, 0.9f, 1f, 0.35f);
    [SerializeField] private Color successColor = new Color(1f, 1f, 1f, 0.9f);
    [SerializeField] private Animator animator;

    [Header("Input")]
    [SerializeField] private InputActionReference parryAction;

    private static readonly int ParryHash = Animator.StringToHash("Parry");
    private static readonly int ParrySuccessHash = Animator.StringToHash("ParrySuccess");

    public event Action OnParrySuccess;
    // La ventana de parry termino sin bloquear nada (lo usa SonidosJugador)
    public event Action OnParryFail;
    public bool IsParrying { get; private set; }

    private PlayerController player;
    private PlayerHealth health;
    private PlayerDash dash;
    private PlayerEnergy energy;
    private float cooldownTimer;
    private Coroutine windowRoutine;
    private Coroutine hitStopRoutine;

    private void Awake()
    {
        player = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
        dash = GetComponent<PlayerDash>();
        energy = GetComponent<PlayerEnergy>();
        SetIndicator(false);
    }

    private void OnEnable()
    {
        parryAction.action.Enable();
        parryAction.action.performed += OnParryPressed;
    }

    private void OnDisable()
    {
        parryAction.action.performed -= OnParryPressed;
        IsParrying = false;
        SetIndicator(false);
        if (hitStopRoutine != null)
        {
            Time.timeScale = 1f;
            hitStopRoutine = null;
        }
    }

    private void Update() => cooldownTimer -= Time.deltaTime;

    private void OnParryPressed(InputAction.CallbackContext ctx)
    {
        if (cooldownTimer > 0f || IsParrying || player.InputLocked) return;
        if (health != null && health.IsDead) return;
        windowRoutine = StartCoroutine(WindowRoutine());
    }

    private IEnumerator WindowRoutine()
    {
        IsParrying = true;
        cooldownTimer = missCooldown;
        SetIndicator(true, windowColor);
        Trigger(ParryHash);

        yield return new WaitForSeconds(parryWindow);

        IsParrying = false;
        SetIndicator(false);
        OnParryFail?.Invoke();
    }

    // Lo llama EnemyAttackHitbox. Devuelve true si el golpe fue bloqueado.
    public bool TryParry(Vector2 attackerPosition, IParryable attacker)
    {
        if (!IsParrying) return false;
        if (dash != null && dash.IsDashing) return false;

        if (windowRoutine != null) StopCoroutine(windowRoutine);
        IsParrying = false;
        cooldownTimer = successCooldown;

        Vector2 pushDirection = (attackerPosition - (Vector2)transform.position).normalized;
        if (attacker != null) attacker.OnParried(pushDirection);

        if (energy != null) energy.GainFromParry();
        OnParrySuccess?.Invoke();
        Trigger(ParrySuccessHash);
        Debug.Log("¡Parry!");

        if (hitStopRoutine != null) StopCoroutine(hitStopRoutine);
        hitStopRoutine = StartCoroutine(HitStopRoutine());
        return true;
    }

    private IEnumerator HitStopRoutine()
    {
        SetIndicator(true, successColor);
        Time.timeScale = hitStopTimeScale;
        yield return new WaitForSecondsRealtime(hitStopDuration);
        Time.timeScale = 1f;
        SetIndicator(false);
        hitStopRoutine = null;
    }

    private void SetIndicator(bool visible, Color? color = null)
    {
        if (parryIndicator == null) return;
        parryIndicator.enabled = visible;
        if (color.HasValue) parryIndicator.color = color.Value;
    }

    private void Trigger(int hash)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(hash);
    }
}