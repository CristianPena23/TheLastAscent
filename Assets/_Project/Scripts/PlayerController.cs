using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 80f;
    [SerializeField] private float deceleration = 100f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float fallGravityMultiplier = 1.6f;
    [SerializeField] private float maxFallSpeed = 22f;

    [Header("Suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual y animación (opcional)")]
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int VelocityYHash = Animator.StringToHash("VelocityY");

    private Rigidbody2D rb;
    private float moveInput;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private bool isGrounded;

    // Nuevo: lo usan el dash y futuras mecánicas
    public bool InputLocked { get; set; }
    public float FacingDirection { get; private set; } = 1f;
    public bool IsGrounded => isGrounded;
    // Lo escucha SonidosJugador (y cualquier efecto que quiera reaccionar al salto)
    public event System.Action AlSaltar;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Sonidos del jugador: se agregan solos si el prefab no los tiene
        if (GetComponent<SonidosJugador>() == null) gameObject.AddComponent<SonidosJugador>();
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        jumpAction.action.Enable();
        jumpAction.action.performed += OnJumpPressed;
        jumpAction.action.canceled += OnJumpReleased;
    }

    private void OnDisable()
    {
        jumpAction.action.performed -= OnJumpPressed;
        jumpAction.action.canceled -= OnJumpReleased;
    }

    private void OnJumpPressed(InputAction.CallbackContext ctx) => jumpBufferCounter = jumpBufferTime;

    private void OnJumpReleased(InputAction.CallbackContext ctx)
    {
        // Soltar el botón antes corta el salto (salto variable)
        if (rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
    }

    private void Update()
    {
        moveInput = moveAction.action.ReadValue<Vector2>().x;

        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        coyoteCounter = isGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
        jumpBufferCounter -= Time.deltaTime;

        // Cambio: no saltar mientras los controles estén bloqueados
        if (!InputLocked && jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            AlSaltar?.Invoke();
        }

        FlipVisual();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        ApplyFallGravity();


        // Cambio: no tocar la velocidad durante el dash
        if (InputLocked) return;

        float target = moveInput * moveSpeed;
        float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, target, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }
    private void ApplyFallGravity()
    {
        if (rb.gravityScale <= 0f) return; // durante el dash la gravedad es 0

        Vector2 v = rb.linearVelocity;
        if (v.y < 0f)
        {
            v.y += Physics2D.gravity.y * rb.gravityScale * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            v.y = Mathf.Max(v.y, -maxFallSpeed);
            rb.linearVelocity = v;
        }
    }


    private void FlipVisual()
    {
        if (InputLocked || Mathf.Abs(moveInput) < 0.01f) return;

        FacingDirection = Mathf.Sign(moveInput);
        if (visual == null) return;

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x) * FacingDirection;
        visual.localScale = scale;
    }

    private void UpdateAnimator()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;
        animator.SetFloat(SpeedHash, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(GroundedHash, isGrounded);
        animator.SetFloat(VelocityYHash, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}