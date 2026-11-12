using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fade negro entre escenas. Se crea solo al iniciar el juego, no hay que ponerlo en ninguna escena.
// Uso: SceneTransition.Go("SC_Boss1", "Entrada");
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    // Id del SpawnPoint donde debe aparecer el jugador en la escena nueva
    public static string PendingSpawnId { get; private set; } = "";

    // true mientras hay un fade/carga en curso (lo usa el menu de pausa)
    public static bool IsBusy => Instance != null && Instance.busy;

    [SerializeField] private float fadeOutTime = 0.4f;
    [SerializeField] private float fadeInTime = 0.4f;

    private CanvasGroup group;
    private bool busy;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (Instance != null) return;
        var go = new GameObject("SceneTransition");
        DontDestroyOnLoad(go);
        go.AddComponent<SceneTransition>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildCanvas();
    }

    private void BuildCanvas()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var imgGo = new GameObject("Fade");
        imgGo.transform.SetParent(transform, false);
        var img = imgGo.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public static void Go(string sceneName, string spawnId)
    {
        if (Instance == null || Instance.busy) return;
        Instance.StartCoroutine(Instance.Run(sceneName, spawnId));
    }

    // Lo llama el SpawnPoint cuando ya coloco al jugador
    public static void ConsumeSpawn() => PendingSpawnId = "";

    private IEnumerator Run(string sceneName, string spawnId)
    {
        busy = true;
        PendingSpawnId = spawnId ?? "";

        yield return Fade(0f, 1f, fadeOutTime);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        yield return null; // un frame para que la escena nueva inicialice

        yield return Fade(1f, 0f, fadeInTime);
        busy = false;
    }

    private IEnumerator Fade(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / time);
            yield return null;
        }
        group.alpha = to;
    }
}
