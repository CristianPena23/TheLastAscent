using UnityEngine;

public enum EnemyAttackType { Basic, Heavy, Ranged }

[RequireComponent(typeof(Collider2D))]
public class EnemyAttackHitbox : MonoBehaviour
{
    [SerializeField] private int damage = 1;
    [SerializeField] private EnemyAttackType attackType = EnemyAttackType.Basic;

    private IParryable parryable;
    private bool hasHit;

    public bool CanBeParried => attackType == EnemyAttackType.Basic;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        parryable = GetComponentInParent<IParryable>();
    }

    // Cada activacion del hitbox puede golpear una sola vez
    private void OnEnable() => hasHit = false;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (hasHit) return;

        PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        hasHit = true;

        if (CanBeParried)
        {
            PlayerParry parry = health.GetComponent<PlayerParry>();
            if (parry != null && parry.TryParry(transform.position, parryable)) return;
        }

        Vector2 direction = (health.transform.position - transform.position).normalized;
        health.TakeDamage(damage, direction);
    }
}