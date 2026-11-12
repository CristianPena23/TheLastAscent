// Menú: Tools > Ultimo Descenso > Configurar diálogo del Círculo 9
// 1. Crea el diálogo de la luciérnaga (Assets/_Project/Data/Dialogos/DLG_Circulo9_Intro) si no existe.
// 2. Agrega a la caja de diálogo el texto de ayuda ("Espacio: continuar · Mantén Espacio: saltar").
// 3. En SC_Circulo9 crea el objeto "IntroCirculo9" (IntroCaida) conectado al DialogueManager y lo guarda.
// Se puede ejecutar varias veces: no duplica nada ni sobrescribe el texto del diálogo.
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConfigurarDialogos
{
    const string CarpetaDatos = "Assets/_Project/Data/Dialogos";
    const string RutaDialogo = CarpetaDatos + "/DLG_Circulo9_Intro.asset";
    const string RutaPrefab = "Assets/_Project/Prefabs/DialgoueSystem.prefab";
    const string RutaEscena = "Assets/_Project/Scenes/SC_Circulo9.unity";
    const string RutaRetratoJugador = "Assets/_Project/Art/UI/Temporal/retrato_jugador.png";
    const float K = 1920f / 1400f;

    [MenuItem("Tools/Ultimo Descenso/Configurar diálogo del Círculo 9")]
    public static void Configurar()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal del modo Play primero."); return; }

        var datos = CrearDialogo();
        AgregarAyudaAlPrefab();
        ConectarEnEscena(datos);
        AssetDatabase.SaveAssets();
    }

    static DialogueData CrearDialogo()
    {
        var datos = AssetDatabase.LoadAssetAtPath<DialogueData>(RutaDialogo);
        if (datos != null) return datos;

        Directory.CreateDirectory(CarpetaDatos);
        var protagonista = AssetDatabase.LoadAssetAtPath<Sprite>(RutaRetratoJugador);
        datos = ScriptableObject.CreateInstance<DialogueData>();
        // Guion de Notion: "Diálogos y narrativa", Escena 0 — Caída (intro).
        // La luciérnaga no habla: sus acotaciones son acciones en escena (líneas sin texto).
        datos.lines = new[]
        {
            Linea("Protagonista", "…¿Dónde…?", protagonista),
            Accion(AccionDialogo.LuciernagaAparece, 1.8f),
            Linea("Protagonista", "Aléjate. No sé qué eres.", protagonista),
            Accion(AccionDialogo.LuciernagaIlumina, 2f),
            Linea("Protagonista", "Arriba. Tiene que haber un arriba.", protagonista),
        };
        AssetDatabase.CreateAsset(datos, RutaDialogo);
        Debug.Log("Creado " + RutaDialogo + " (texto provisional, edítalo en el Inspector)");
        return datos;
    }

    static DialogueLine Linea(string quien, string texto, Sprite retrato) =>
        new DialogueLine { speaker = quien, text = texto, retrato = retrato };

    static DialogueLine Accion(AccionDialogo accion, float duracion) =>
        new DialogueLine { speaker = "", text = "", accion = accion, duracion = duracion };

    static void AgregarAyudaAlPrefab()
    {
        if (!File.Exists(RutaPrefab)) { Debug.LogWarning("No se encontró " + RutaPrefab); return; }
        var raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        var manager = raiz.GetComponentInChildren<DialogueManager>(true);
        var caja = raiz.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Caja");
        if (manager == null || caja == null) { PrefabUtility.UnloadPrefabContents(raiz); return; }

        var so = new SerializedObject(manager);
        var campo = so.FindProperty("textoAyuda");
        if (campo != null && campo.objectReferenceValue == null)
        {
            var go = new GameObject("TextAyuda", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(caja, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            var tema = TemaUI.Predeterminado;
            if (tema != null && tema.fuenteTexto != null) t.font = tema.fuenteTexto;
            t.text = "Espacio: continuar   ·   Mantén Espacio: saltar";
            t.fontSize = 24;
            t.color = tema != null ? new Color(tema.textoNormal.r, tema.textoNormal.g, tema.textoNormal.b, 0.6f) : new Color(1, 1, 1, 0.6f);
            t.alignment = TextAlignmentOptions.Right;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            // Abajo a la derecha de la caja, a la izquierda de la flecha (coordenadas del mockup)
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0.5f, 0.5f);
            float x = 560, y = 232, w = 500, h = 24;
            r.anchoredPosition = new Vector2((x + w * 0.5f) * K, -(y + h * 0.5f) * K);
            r.sizeDelta = new Vector2(w * K, h * K);

            campo.objectReferenceValue = t;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Debug.Log("Texto de ayuda agregado a la caja de diálogo.");
        }
        PrefabUtility.UnloadPrefabContents(raiz);
    }

    static void ConectarEnEscena(DialogueData datos)
    {
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.path != RutaEscena)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            escena = EditorSceneManager.OpenScene(RutaEscena);
        }

        var intro = Object.FindFirstObjectByType<IntroCaida>(FindObjectsInactive.Include);
        if (intro == null)
        {
            intro = new GameObject("IntroCirculo9").AddComponent<IntroCaida>();
            Debug.Log("Creado IntroCirculo9 en " + RutaEscena);
        }

        var so = new SerializedObject(intro);
        so.FindProperty("dialogoIntro").objectReferenceValue = datos;
        var manager = Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (manager != null) so.FindProperty("dialogos").objectReferenceValue = manager;
        else Debug.LogWarning("SC_Circulo9 no tiene DialogueManager: arrastra el prefab DialgoueSystem a la escena.");
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Selection.activeObject = intro.gameObject;
        Debug.Log("Diálogo del Círculo 9 listo: la luciérnaga habla al aterrizar.");
    }
}
