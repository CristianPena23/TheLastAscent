using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D), typeof(PlayerController))]
public class PlayerAttack : MonoBehaviour
{
    private enum AttackDir { Forward = 0, Up = 1, Down = 2 }

    // Cada ataque (true si golpeo algo). Lo escucha SonidosJugador
    public event System.Action<bool> AlAtacar;

    [Header("Ataque")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float cooldown = 0.35f;
    [SerializeField] private float verticalThreshold = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Hitboxes (relativas al jugador)")]
    [SerializeField] private Vector2 forwardOffset = new Vector2(1.2f, 0f);
    [SerializeField] private Vector2 forwardSize = new Vector2(1.4f, 1.2f);
    [SerializeField] private Vector2 upOffset = new Vector2(0f, 1.5f);
    [SerializeField] private Vector2 upSize = new Vector2(1.4f, 1.4f);
    [SerializeField] private Vector2 downOffset = new Vector2(0f, -1.5f);
    [SerializeField] private Vector2 downSize = new Vector2(1.4f, 1.4f);

    [Header("Rebote (golpe hacia abajo)")]
    [SerializeField] private float pogoForce = 13f;

    [Header("Input")]
    [SerializeField] private InputActionReference attackAction;
    [SerializeField] private InputActionReference moveAction;

    [Header("Feedback y animación (opcional)")]
    [SerializeField] private Transform attackVfx;
    [SerializeField] private float vfxDuration = 0.08f;
    [SerializeField] private Animator animator;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int AttackDirHash = Animator.StringToHash("AttackDirection");

    private Rigidbody2D rb;
    private PlayerController player;
    private PlayerEnergy energy;
    private float cooldownTimer;
    private Coroutine vfxRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
        if (attackVfx != null) attackVfx.gameObject.SetActive(false);
        energy = GetComponent<PlayerEnergy>();
    }

    private void OnEnable()
    {
        attackAction.action.Enable();
        moveAction.action.Enable();
        attackAction.action.performed += OnAttackPressed;
    }

    private void OnDisable()
    {
        attackAction.action.performed -= OnAttackPressed;
    }

    private void Update() => cooldownTimer -= Time.deltaTime;

    private void OnAttackPressed(InputAction.CallbackContext ctx)
    {
        if (cooldownTimer > 0f || player.InputLocked) return;
        Attack();
    }

    private void Attack()
    {
        cooldownTimer = cooldown;

        float vertical = moveAction.action.ReadValue<Vector2>().y;
        AttackDir dir = AttackDir.Forward;
        if (vertical > verticalThreshold) dir = AttackDir.Up;
        else if (vertical < -verticalThreshold && !player.IsGrounded) dir = AttackDir.Down;

        GetHitbox(dir, out Vector2 center, out Vector2 size);

        bool hitSomething = false;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, 0f, enemyLayer);
        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null) continue;

            Vector2 hitDirection = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
            target.TakeDamage(damage, hitDirection);
            hitSomething = true;
        }

        // Rebote: golpear algo hacia abajo impulsa al jugador hacia arriba
        if (hitSomething && dir == AttackDir.Down)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, pogoForce);
        if (hitSomething && energy != null) energy.GainFromAttack();
        AlAtacar?.Invoke(hitSomething);

        if (attackVfx != null)
        {
            if (vfxRoutine != null) StopCoroutine(vfxRoutine);
            vfxRoutine = StartCoroutine(ShowVfx(center, size));
        }

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            int animDir = (int)dir;
            if (dir == AttackDir.Forward && !player.IsGrounded) animDir = 3; // ataque horizontal en el aire
            animator.SetInteger(AttackDirHash, animDir);
            animator.SetTrigger(AttackHash);
        }
    }

    private void GetHitbox(AttackDir dir, out Vector2 center, out Vector2 size)
    {
        Vector2 pos = transform.position;
        float facing = Application.isPlaying && player != null ? player.FacingDirection : 1f;

        switch (dir)
        {
            case AttackDir.Up:
                center = pos + upOffset;
                size = upSize;
                break;
            case AttackDir.Down:
                center = pos + downOffset;
                size = downSize;
                break;
            default:
                center = pos + new Vector2(forwardOffset.x * facing, forwardOffset.y);
                size = forwardSize;
                break;
        }
    }

    private IEnumerator ShowVfx(Vector2 center, Vector2 size)
    {
        attackVfx.position = center;
        attackVfx.localScale = new Vector3(size.x, size.y, 1f);
        attackVfx.gameObject.SetActive(true);
        yield return new WaitForSeconds(vfxDuration);
        attackVfx.gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (AttackDir dir in new[] { AttackDir.Forward, AttackDir.Up, AttackDir.Down })
        {
            GetHitbox(dir, out Vector2 center, out Vector2 size);
            Gizmos.DrawWireCube(center, size);
        }
    }
}