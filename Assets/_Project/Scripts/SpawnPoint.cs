using UnityEngine;

// Punto donde aparece el jugador al llegar a esta escena desde una puerta.
// El Id debe coincidir con "Target Spawn Id" de la puerta que lleva a esta escena.
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string id = "Entrada";

    private void Start()
    {
        if (SceneTransition.PendingSpawnId != id) return;

        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            player.transform.position = transform.position;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        SceneTransition.ConsumeSpawn();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.2f);
    }
}
