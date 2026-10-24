using System.Collections;
using UnityEngine;

public class TestEnemyAttacker : MonoBehaviour, IParryable
{
    [Header("Ataques")]
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private float telegraphTime = 0.5f;
    [SerializeField] private float activeTime = 0.2f;
    [SerializeField] private int heavyEvery = 3;

    [Header("Hitboxes (hijos desactivados)")]
    [SerializeField] private GameObject basicHitbox;
    [SerializeField] private GameObject heavyHitbox;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Color basicTelegraph = Color.yellow;
    [SerializeField] private Color heavyTelegraph = new Color(1f, 0.2f, 0.8f);
    [SerializeField] private Color staggerColor = Color.cyan;
    [SerializeField] private float staggerDuration = 1.2f;

    private Color baseColor = Color.white;

    private void Awake()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) baseColor = sprite.color;
        SetHitboxes(false);
    }

    private void OnEnable() => StartCoroutine(AttackLoop());

    private IEnumerator AttackLoop()
    {
        int count = 0;
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);
            count++;
            bool heavy = heavyEvery > 0 && count % heavyEvery == 0;
            yield return StartCoroutine(Attack(
                heavy ? heavyHitbox : basicHitbox,
                heavy ? heavyTelegraph : basicTelegraph));
        }
    }

    private IEnumerator Attack(GameObject hitbox, Color telegraph)
    {
        if (sprite != null) sprite.color = telegraph;
        yield return new WaitForSeconds(telegraphTime);

        hitbox.SetActive(true);
        yield return new WaitForSeconds(activeTime);

        hitbox.SetActive(false);
        if (sprite != null) sprite.color = baseColor;
    }

    public void OnParried(Vector2 pushDirection)
    {
        StopAllCoroutines();
        SetHitboxes(false);
        StartCoroutine(StaggerRoutine());
    }

    private IEnumerator StaggerRoutine()
    {
        if (sprite != null) sprite.color = staggerColor;
        yield return new WaitForSeconds(staggerDuration);
        if (sprite != null) sprite.color = baseColor;
        StartCoroutine(AttackLoop());
    }

    private void SetHitboxes(bool active)
    {
        if (basicHitbox != null) basicHitbox.SetActive(active);
        if (heavyHitbox != null) heavyHitbox.SetActive(active);
    }
}