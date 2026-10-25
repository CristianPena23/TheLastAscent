using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerController), typeof(Collider2D))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash")]
    [SerializeField] private float dashSpeed = 22f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.4f;
    [SerializeField] private bool oneAirDash = true;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.25f);

    [Header("Atravesar enemigos")]
    [SerializeField] private string enemyLayerName = "Enemy";

    [Header("Input y animación (opcional)")]
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private Animator animator;

    private static readonly int DashHash = Animator.StringToHash("Dash");

    private Rigidbody2D rb;
    private PlayerController player;
    private Collider2D col;
    private bool isDashing;
    private bool airDashUsed;
    private float cooldownTimer;
    private float originalGravity;
    private int enemyLayer;
    private LayerMask enemyMask;
    private Coroutine restoreRoutine;

    public bool IsDashing => isDashing;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
        col = GetComponent<Collider2D>();

        enemyLayer = LayerMask.NameToLayer(enemyLayerName);
        enemyMask = 1 << enemyLayer;

        if (enemyLayer < 0)
            Debug.LogWarning($"La capa '{enemyLayerName}' no existe.");
        if (gameObject.layer == 0)
            Debug.LogWarning("El Player está en la capa Default. Asígnale la capa 'Player' para que el dash atraviese enemigos sin afectar otros objetos.");
    }

    private void OnEnable()
    {
        dashAction.action.Enable();
        dashAction.action.performed += OnDashPressed;
    }

    private void OnDisable()
    {
        dashAction.action.performed -= OnDashPressed;
        if (isDashing)
        {
            rb.gravityScale = originalGravity;
            player.InputLocked = false;
            isDashing = false;
        }
        SetEnemyCollision(true);
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (player.IsGrounded && !isDashing) airDashUsed = false;
    }

    private void OnDashPressed(InputAction.CallbackContext ctx)
    {
        if (isDashing || cooldownTimer > 0f || player.InputLocked) return;
        if (oneAirDash && !player.IsGrounded && airDashUsed) return;
        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        if (!player.IsGrounded) airDashUsed = true;

        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }
        SetEnemyCollision(false);

        player.InputLocked = true;
        originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        if (animator != null && animator.runtimeAnimatorController != null)
            animator.SetTrigger(DashHash);

        // Arranca a toda velocidad y se frena hacia el final
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            float t = elapsed / dashDuration;
            float speed = dashSpeed * speedCurve.Evaluate(t);
            rb.linearVelocity = new Vector2(player.FacingDirection * speed, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        EndDash();
    }

    private void EndDash()
    {
        rb.gravityScale = originalGravity;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, 0f);
        player.InputLocked = false;
        isDashing = false;
        cooldownTimer = dashCooldown;

        // Si el dash terminó dentro de un enemigo, no se restaura la colisión hasta salir de él
        restoreRoutine = StartCoroutine(RestoreEnemyCollision());
    }

    private IEnumerator RestoreEnemyCollision()
    {
        while (Physics2D.OverlapBox(col.bounds.center, col.bounds.size * 0.95f, 0f, enemyMask) != null)
            yield return null;

        SetEnemyCollision(true);
        restoreRoutine = null;
    }

    private void SetEnemyCollision(bool enabled)
    {
        if (enemyLayer < 0) return;
        Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, !enabled);
    }
}