// Menu: Tools > Ultimo Descenso > Configurar Circulo 8 (capas)
// Lee los PNG del nivel 8 (01_..., 02_..., ... 23_...), los importa bien y arma las capas en la escena:
//  - Sprite Renderer + orden de dibujo (Felipe queda en 0)
//  - CapaParallax con el factor de cada capa
//  - 21 suelo y 22 plataformas: PolygonCollider2D en la capa "Ground" (sin dibujar)
//  - 23 zonas de muerte: PolygonCollider2D trigger + VoidKillZone (sin dibujar)
//  - Limites de camara (CameraBounds) a 0..96 x 0..20 y punto de aparicion "Entrada"
// La capa 24 (guia de tamano de Felipe) se ignora. Se puede repetir: borra y rehace "Nivel8_Capas".
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfigurarCirculo8 : EditorWindow
{
    const string RootName = "Nivel8_Capas";

    string carpeta = "Assets/_Project/Art/Nivel8";
    string capaSueloNombre = "Ground";
    bool materialUnlit = true;
    bool plataformasUnaVia = true;
    bool ajustarCamara = true;
    bool colocarJugador = true;
    Vector2 anchoAlto = new Vector2(96f, 20f);

    static readonly Dictionary<int, float> Factores = new Dictionary<int, float>
    {
        {1, 0.05f}, {2, 0.10f}, {3, 0.15f}, {4, 0.15f}, {5, 0.25f}, {6, 0.30f}, {7, 0.45f},
        {8, 0.55f}, {9, 0.70f}, {10, 0.90f}, {16, 1.15f}, {17, 1.25f}, {18, 1.25f},
        {19, 0.90f}, {20, 1.10f}
    };

    [MenuItem("Tools/Ultimo Descenso/Configurar Circulo 8 (capas)")]
    static void Open() => GetWindow<ConfigurarCirculo8>("Circulo 8");

    void OnGUI()
    {
        EditorGUILayout.HelpBox("1) Descomprime los PNG en la carpeta de abajo.\n2) Abre la escena SC_Circulo8.\n3) Presiona Configurar.", MessageType.Info);
        carpeta = EditorGUILayout.TextField("Carpeta de las capas", carpeta);
        capaSueloNombre = EditorGUILayout.TextField("Layer del suelo", capaSueloNombre);
        materialUnlit = EditorGUILayout.Toggle("Material sin luz (Unlit)", materialUnlit);
        plataformasUnaVia = EditorGUILayout.Toggle("Plataformas de una via", plataformasUnaVia);
        ajustarCamara = EditorGUILayout.Toggle("Ajustar limites de camara", ajustarCamara);
        colocarJugador = EditorGUILayout.Toggle("Colocar jugador al inicio", colocarJugador);
        anchoAlto = EditorGUILayout.Vector2Field("Tamano del nivel (unidades)", anchoAlto);

        if (GUILayout.Button("Configurar", GUILayout.Height(32))) Configurar();
    }

    void Configurar()
    {
        var capas = new SortedDictionary<int, string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { carpeta }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = Regex.Match(Path.GetFileName(path), @"^(\d\d)_");
            if (!m.Success) continue;
            int n = int.Parse(m.Groups[1].Value);
            if (n == 24) continue; // guia de tamano
            capas[n] = path;
        }
        if (capas.Count == 0)
        {
            ShowNotification(new GUIContent("No encontre PNG numerados en " + carpeta));
            return;
        }

        // 1) Importar
        foreach (var kv in capas) ConfigurarImportador(kv.Value);
        AssetDatabase.Refresh();

        // 2) Escena
        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Nivel 8");

        int layerSuelo = LayerMask.NameToLayer(capaSueloNombre);
        if (layerSuelo < 0) { Debug.LogWarning("No existe el layer '" + capaSueloNombre + "'. Se usara Default."); layerSuelo = 0; }
        Material unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");

        int creadas = 0;
        foreach (var kv in capas)
        {
            int n = kv.Key;
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(kv.Value);
            if (sp == null) { Debug.LogWarning("No se pudo cargar el sprite " + kv.Value); continue; }

            var go = new GameObject(Path.GetFileNameWithoutExtension(kv.Value));
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = Vector3.zero;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = n <= 14 ? n - 15 : n - 14; // Felipe en 0: 01..14 detras, 15..20 delante

            if (n >= 21)
            {
                var col = go.AddComponent<PolygonCollider2D>();
                int count = sp.GetPhysicsShapeCount();
                col.pathCount = count;
                var pts = new List<Vector2>();
                for (int i = 0; i < count; i++) { pts.Clear(); sp.GetPhysicsShape(i, pts); col.SetPath(i, pts.ToArray()); }

                if (n == 21 || n == 22) go.layer = layerSuelo;
                if (n == 22 && plataformasUnaVia)
                {
                    col.usedByEffector = true;
                    var eff = go.AddComponent<PlatformEffector2D>();
                    eff.useOneWay = true;
                    eff.surfaceArc = 170f;
                }
                if (n == 23)
                {
                    col.isTrigger = true;
                    go.AddComponent<VoidKillZone>();
                }
                sr.enabled = false; // las colisiones no se dibujan
            }
            else
            {
                if (materialUnlit && unlit != null) sr.sharedMaterial = unlit;
                if (Factores.TryGetValue(n, out float f))
                {
                    var p = go.AddComponent<CapaParallax>();
                    p.factor = f;
                    p.centroDelNivel = anchoAlto * 0.5f;
                }
            }
            creadas++;
        }

        // 3) Camara
        if (ajustarCamara)
        {
            var cb = GameObject.Find("CameraBounds");
            var poly = cb != null ? cb.GetComponent<PolygonCollider2D>() : null;
            if (poly != null)
            {
                Undo.RecordObject(poly, "Limites de camara");
                cb.transform.position = Vector3.zero;
                cb.transform.localScale = Vector3.one;
                poly.offset = Vector2.zero;
                poly.pathCount = 1;
                poly.SetPath(0, new[] { new Vector2(0, 0), new Vector2(0, anchoAlto.y), new Vector2(anchoAlto.x, anchoAlto.y), new Vector2(anchoAlto.x, 0) });
            }
            else Debug.LogWarning("No encontre un objeto 'CameraBounds' con PolygonCollider2D. Ajusta los limites de camara a mano.");
        }

        // 4) Jugador y punto de aparicion
        if (colocarJugador)
        {
            Vector3 inicio = new Vector3(4f, 10f, 0f);
            var sps = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            SpawnPoint spawn = sps.Length > 0 ? sps[0] : new GameObject("Spawn_Entrada").AddComponent<SpawnPoint>();
            spawn.transform.position = inicio;
            var so = new SerializedObject(spawn);
            var idProp = so.FindProperty("id");
            if (idProp != null) { idProp.stringValue = "Entrada"; so.ApplyModifiedProperties(); }

            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) player.transform.position = inicio;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        ShowNotification(new GUIContent("Listo: " + creadas + " capas"));
    }

    static void ConfigurarImportador(string path)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = 100f;

        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.BottomLeft;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.spritePivot = Vector2.zero;

        imp.maxTextureSize = 16384;
        imp.textureCompression = TextureImporterCompression.CompressedHQ;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.isReadable = false;
        imp.SaveAndReimport();
    }
}
