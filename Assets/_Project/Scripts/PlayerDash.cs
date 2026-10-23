using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerController))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.4f;
    [SerializeField] private bool oneAirDash = true;

    [Header("Input y animación (opcional)")]
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private Animator animator;

    private static readonly int DashHash = Animator.StringToHash("Dash");

    private Rigidbody2D rb;
    private PlayerController player;
    private bool isDashing;
    private bool airDashUsed;
    private float cooldownTimer;
    private float originalGravity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        dashAction.action.Enable();
        dashAction.action.performed += OnDashPressed;
    }

    private void OnDisable()
    {
        dashAction.action.performed -= OnDashPressed;
        if (isDashing) EndDash();
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (player.IsGrounded && !isDashing) airDashUsed = false;
    }

    private void OnDashPressed(InputAction.CallbackContext ctx)
    {
        if (isDashing || cooldownTimer > 0f) return;
        if (oneAirDash && !player.IsGrounded && airDashUsed) return;
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        if (!player.IsGrounded) airDashUsed = true;

        player.InputLocked = true;
        originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(player.FacingDirection * dashSpeed, 0f);

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(DashHash);

        yield return new WaitForSeconds(dashDuration);
        EndDash();
    }

    private void EndDash()
    {
        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, 0f);
        player.InputLocked = false;
        isDashing = false;
        cooldownTimer = dashCooldown;
    }
}