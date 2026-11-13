using UnityEngine;

// Zona de vacio: si el jugador entra, muere y reaparece en el ultimo checkpoint.
// Ponla en un objeto vacio con BoxCollider2D (Is Trigger) debajo del hueco.
[RequireComponent(typeof(Collider2D))]
public class VoidKillZone : MonoBehaviour
{
    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var health = other.GetComponentInParent<PlayerHealth>();
        if (health != null) health.KillFromVoid();
    }

    private void OnDrawGizmos()
    {
        var c = GetComponent<Collider2D>();
        if (c == null) return;
        Gizmos.color = new Color(1f, 0f, 0.3f, 0.35f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
    }
}
