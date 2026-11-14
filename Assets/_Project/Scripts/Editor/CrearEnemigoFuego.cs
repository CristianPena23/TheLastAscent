// Menú: Tools > Ultimo Descenso > Crear enemigo de fuego (dragoncito)
// 1. Corta la hoja dragon_fuego.png (5x5 cuadros de 768x512) en 25 sprites.
// 2. Crea el prefab PF_EnemigoFuego: cuerpo (capa Enemy), Rigidbody2D sin gravedad,
//    EnemigoFuego con sus animaciones y el hitbox del fuego (capa EnemyAttack, no se puede hacer parry).
// 3. Si la escena abierta es MecanicasEnemigos, coloca uno en la vista para probarlo.
// Las medidas del fuego salen de la hoja: la llamarada (cuadro 16) mide ~295 px desde la boca.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class CrearEnemigoFuego
{
    const string RutaHoja = "Assets/_Project/Art/Sprites/EnemigoFuego/dragon_fuego.png";
    const string RutaPrefab = "Assets/_Project/Prefabs/PF_EnemigoFuego.prefab";
    const int Columnas = 5, Filas = 5, AnchoCuadro = 768, AltoCuadro = 512;
    const float PPU = 160f;                       // el dragón queda de ~1.4 unidades de ancho
    static readonly Vector2 Pivote = new Vector2(222f / 768f, 1f - 265f / 512f); // centro del cuerpo

    // Medidas en píxeles de la hoja (desde el pivote): boca en x=340, fuego hasta x=634
    const float BocaX = (340f - 222f) / PPU;
    const float LargoFuego = (634f - 340f) / PPU;
    const float AltoFuego = 0.6f;
    const float AlturaFuego = (265f - 248f) / PPU;

    [MenuItem("Tools/Ultimo Descenso/Crear enemigo de fuego (dragoncito)")]
    public static void Crear()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal del modo Play primero."); return; }
        if (!File.Exists(RutaHoja)) { Debug.LogError("No se encontró " + RutaHoja); return; }

        var sprites = CortarHoja();
        if (sprites.Length < Columnas * Filas) { Debug.LogError("No se pudo cortar la hoja."); return; }

        var prefab = CrearPrefab(sprites);
        ColocarEnEscena(prefab);
    }

    static Sprite[] CortarHoja()
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(RutaHoja);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = PPU;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.maxTextureSize = 4096; // la hoja mide 3840x2560: si se reduce, los cortes no coinciden
        imp.filterMode = FilterMode.Bilinear;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var proveedor = factory.GetSpriteEditorDataProviderFromObject(imp);
        proveedor.InitSpriteEditorDataProvider();

        var rects = new SpriteRect[Columnas * Filas];
        for (int i = 0; i < rects.Length; i++)
        {
            int col = i % Columnas, fila = i / Columnas;
            rects[i] = new SpriteRect
            {
                name = "dragon_fuego_" + i,
                spriteID = GUID.Generate(),
                // Unity cuenta las filas desde abajo
                rect = new Rect(col * AnchoCuadro, (Filas - 1 - fila) * AltoCuadro, AnchoCuadro, AltoCuadro),
                alignment = SpriteAlignment.Custom,
                pivot = Pivote,
            };
        }
        proveedor.SetSpriteRects(rects);

        var nombres = proveedor.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nombres != null)
            nombres.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));

        proveedor.Apply();
        imp.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(RutaHoja).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
    }

    static GameObject CrearPrefab(Sprite[] s)
    {
        var raiz = new GameObject("PF_EnemigoFuego");
        raiz.layer = LayerMask.NameToLayer("Enemy");

        var sr = raiz.AddComponent<SpriteRenderer>();
        sr.sprite = s[0];
        sr.sortingOrder = 3;

        var rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
        cuerpo.direction = CapsuleDirection2D.Horizontal;
        cuerpo.size = new Vector2(1.2f, 0.75f);
        cuerpo.offset = new Vector2(0.15f, 0.12f); // cubre de la cola a la cabeza

        // Hitbox del fuego (hijo): delante de la boca, del largo de la llamarada
        var fuego = new GameObject("Fuego");
        fuego.layer = LayerMask.NameToLayer("EnemyAttack");
        fuego.transform.SetParent(raiz.transform, false);
        var caja = fuego.AddComponent<BoxCollider2D>();
        caja.isTrigger = true;
        caja.offset = new Vector2(BocaX + LargoFuego * 0.5f, AlturaFuego);
        caja.size = new Vector2(LargoFuego, AltoFuego);
        var golpe = fuego.AddComponent<EnemyAttackHitbox>();
        var soGolpe = new SerializedObject(golpe);
        soGolpe.FindProperty("attackType").enumValueIndex = (int)EnemyAttackType.Ranged; // sin parry
        soGolpe.FindProperty("damage").intValue = 1;
        soGolpe.ApplyModifiedPropertiesWithoutUndo();
        fuego.SetActive(false);

        var enemigo = raiz.AddComponent<EnemigoFuego>();
        var so = new SerializedObject(enemigo);
        so.FindProperty("hitboxFuego").objectReferenceValue = fuego;
        Cuadros(so, "framesVuelo", s, 0, 12);
        Cuadros(so, "framesPreparar", s, 13, 14);
        Cuadros(so, "framesFuego", s, 15, 19);
        Cuadros(so, "framesRecuperar", s, 20, 24);
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);
        Debug.Log("Creado " + RutaPrefab);
        return prefab;
    }

    static void Cuadros(SerializedObject so, string campo, Sprite[] s, int desde, int hasta)
    {
        var p = so.FindProperty(campo);
        p.arraySize = hasta - desde + 1;
        for (int i = desde; i <= hasta; i++) p.GetArrayElementAtIndex(i - desde).objectReferenceValue = s[i];
    }

    static void ColocarEnEscena(GameObject prefab)
    {
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "MecanicasEnemigos")
        {
            Debug.Log("Prefab listo. Arrástralo a la escena que quieras (Assets/_Project/Prefabs/PF_EnemigoFuego).");
            Selection.activeObject = prefab;
            return;
        }

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
        go.name = "Enemigo-fuego";
        var vista = SceneView.lastActiveSceneView;
        Vector3 pos = vista != null ? vista.pivot : Vector3.zero;
        pos.z = 0f;
        go.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(go, "Crear enemigo de fuego");
        EditorSceneManager.MarkSceneDirty(escena);
        Selection.activeObject = go;
        Debug.Log("Enemigo de fuego colocado en MecanicasEnemigos (en el centro de la vista). Guarda la escena.");
    }
}
