using System.Collections;
using UnityEngine;

// Estela del dash: deja "copias" semitransparentes del sprite del jugador mientras dura el dash.
// Poner en PF_Player (junto a PlayerDash) y asignar el SpriteRenderer del objeto Visual.
[RequireComponent(typeof(PlayerDash))]
public class DashTrail : MonoBehaviour
{
    [Header("Origen")]
    [SerializeField] private SpriteRenderer source;      // el SpriteRenderer del Visual

    [Header("Estela")]
    [SerializeField] private float spawnInterval = 0.03f; // tiempo entre copias
    [SerializeField] private float fadeTime = 0.25f;      // lo que tarda cada copia en desaparecer
    [SerializeField] private Color tint = new Color(0.55f, 0.8f, 1f, 0.6f); // azul hielo
    [SerializeField] private int orderOffset = -1;        // detras del jugador

    private PlayerDash dash;
    private float timer;

    private void Awake()
    {
        dash = GetComponent<PlayerDash>();

        // Si no se asigno, usa el SpriteRenderer que esta junto al Animator del personaje (el Visual).
        if (source == null)
        {
            Animator anim = GetComponentInChildren<Animator>();
            if (anim != null) source = anim.GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (source == null || !dash.IsDashing)
        {
            timer = 0f;
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnGhost();
            timer = spawnInterval;
        }
    }

    private void SpawnGhost()
    {
        var go = new GameObject("DashGhost");
        go.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        go.transform.localScale = source.transform.lossyScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = source.sprite;
        sr.flipX = source.flipX;
        sr.flipY = source.flipY;
        sr.sharedMaterial = source.sharedMaterial;
        sr.sortingLayerID = source.sortingLayerID;
        sr.sortingOrder = source.sortingOrder + orderOffset;
        sr.color = tint;

        StartCoroutine(Fade(sr));
    }

    private IEnumerator Fade(SpriteRenderer sr)
    {
        float t = 0f;
        Color start = sr.color;
        while (t < fadeTime && sr != null)
        {
            t += Time.deltaTime;
            Color c = start;
            c.a = Mathf.Lerp(start.a, 0f, t / fadeTime);
            sr.color = c;
            yield return null;
        }
        if (sr != null) Destroy(sr.gameObject);
    }
}
