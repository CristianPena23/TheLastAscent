using System.Collections;
using UnityEngine;

// Cierra la entrada de la arena del jefe cuando el jugador pasa por la zona de activacion.
// Se vuelve a abrir si el jugador muere (para no quedar atrapado) o si el jefe muere.
// Uso:
//  - Este objeto lleva un Collider2D con Is Trigger (zona justo dentro de la arena, junto a la entrada).
//  - "Blocker" = Collider2D SOLIDO (sin Is Trigger) que cierra el paso; empieza desactivado.
//  - "Visual" (opcional) = objeto con sprite/niebla que aparece al cerrar.
[RequireComponent(typeof(Collider2D))]
public class BossArenaLock : MonoBehaviour
{
    [SerializeField] private BossHealth boss;
    [SerializeField] private Collider2D blocker;
    [SerializeField] private GameObject visual;
    [SerializeField] private float closeDelay = 0.3f;
    [SerializeField] private bool lockOnlyOnce = false;

    private bool locked;
    private bool done;
    private PlayerHealth player;

    private void Reset() => GetComponent<Collider2D>().isTrigger = true;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        SetLocked(false);
    }

    private void Start()
    {
        if (boss == null) boss = FindFirstObjectByType<BossHealth>();
        if (boss != null) boss.Died += OnBossDied;
        player = FindFirstObjectByType<PlayerHealth>();
        if (player != null) player.OnDied += OnPlayerDied;
    }

    private void OnDestroy()
    {
        if (boss != null) boss.Died -= OnBossDied;
        if (player != null) player.OnDied -= OnPlayerDied;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (locked || done) return;
        if (boss != null && boss.IsDead) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        StartCoroutine(CloseRoutine());
    }

    private IEnumerator CloseRoutine()
    {
        locked = true;
        yield return new WaitForSeconds(closeDelay);
        if (locked) SetLocked(true);
    }

    private void OnBossDied()
    {
        done = true;
        locked = false;
        SetLocked(false);
    }

    private void OnPlayerDied()
    {
        StopAllCoroutines();
        locked = false;
        SetLocked(false);
        if (lockOnlyOnce) done = true;
    }

    private void SetLocked(bool on)
    {
        if (blocker != null) blocker.enabled = on;
        if (visual != null) visual.SetActive(on);
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
