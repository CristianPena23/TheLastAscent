using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Vida de cualquier jefe. Reutilizable: ponerlo en el objeto raiz del jefe.
// Recibe dano del jugador igual que los demas enemigos (IDamageable + capa Enemy en un collider hijo).
public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private string bossName = "JEFE";
    [SerializeField] private int maxHealth = 30;

    [Header("Referencias (opcional, se buscan solas)")]
    [SerializeField] private SpriteRenderer bossSprite;
    [SerializeField] private Animator animator;
    [SerializeField] private BossAttackController attackController;

    [Header("Feedback al recibir dano")]
    [SerializeField] private Color flashColor = new Color(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private float flashTime = 0.08f;

    [Header("Al morir")]
    [Tooltip("Trigger del Animator (opcional). Si no existe en el Animator se ignora")]
    [SerializeField] private string dieTrigger = "Die";
    [SerializeField] private float deathFadeTime = 1.5f;
    [Tooltip("Escena a cargar despues de morir. Vacio = ninguna")]
    [SerializeField] private string nextScene = "";
    [SerializeField] private string nextSpawnId = "";
    [SerializeField] private float sceneDelay = 2.5f;
    [SerializeField] private UnityEvent onDied;

    public event Action<int, int> Changed;   // (vida actual, vida maxima)
    public event Action Died;

    public string BossName => bossName;
    public int Current { get; private set; }
    public int Max => maxHealth;
    public bool IsDead { get; private set; }

    private Coroutine flashRoutine;

    private void Awake()
    {
        Current = maxHealth;
        if (bossSprite == null) bossSprite = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (attackController == null) attackController = GetComponentInChildren<BossAttackController>();
    }

    public void TakeDamage(int amount, Vector2 hitDirection)
    {
        if (IsDead || amount <= 0) return;

        Current = Mathf.Max(0, Current - amount);
        Changed?.Invoke(Current, maxHealth);

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(Flash());

        if (Current <= 0) StartCoroutine(Die());
    }

    private IEnumerator Flash()
    {
        if (bossSprite == null) yield break;
        bossSprite.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        bossSprite.color = Color.white;
        flashRoutine = null;
    }

    private bool HasParam(string n)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(n)) return false;
        foreach (var p in animator.parameters)
            if (p.name == n) return true;
        return false;
    }

    private IEnumerator Die()
    {
        IsDead = true;
        if (attackController != null) attackController.Active = false;
        foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
        if (HasParam(dieTrigger)) animator.SetTrigger(dieTrigger);

        Died?.Invoke();
        onDied?.Invoke();

        // Se desvanece
        if (bossSprite != null)
        {
            yield return null;
            float t = 0f;
            while (t < deathFadeTime)
            {
                t += Time.deltaTime;
                Color c = Color.white;
                c.a = 1f - Mathf.Clamp01(t / deathFadeTime);
                bossSprite.color = c;
                yield return null;
            }
        }

        if (!string.IsNullOrEmpty(nextScene))
        {
            yield return new WaitForSeconds(sceneDelay);
            SceneTransition.Go(nextScene, nextSpawnId);
        }
    }
}
