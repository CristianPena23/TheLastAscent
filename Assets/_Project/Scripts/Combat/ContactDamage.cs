using UnityEngine;

public class ContactDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision);
    private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision);

    private void TryDamage(Collision2D collision)
    {
        PlayerHealth health = collision.collider.GetComponentInParent<PlayerHealth>();
        if (health == null) return;

        Vector2 direction = (health.transform.position - transform.position).normalized;
        health.TakeDamage(damage, direction);
    }
}