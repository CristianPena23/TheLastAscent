using UnityEngine;

// Puerta / salida de zona. Al entrar el jugador hace fade y carga otra escena.
// Poner en un objeto con Collider2D (se marca como Trigger solo).
[RequireComponent(typeof(Collider2D))]
public class SceneDoor : MonoBehaviour
{
    [SerializeField] private string targetScene = "SC_Boss1";
    [SerializeField] private string targetSpawnId = "Entrada";

    private bool used;

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used) return;
        var player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        used = true;
        player.InputLocked = true;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        SceneTransition.Go(targetScene, targetSpawnId);
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.6f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
