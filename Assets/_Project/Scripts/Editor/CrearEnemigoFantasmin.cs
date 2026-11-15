// Menú: Tools > Ultimo Descenso > Crear enemigo fantasmín (melee)
// 1. Corta fantasmin_mover.png (5x5 de 256x256) y fantasmin_ataque.png (5x5 de 768x544).
// 2. Crea el prefab PF_EnemigoFantasmin con EnemigoBase: patrulla, persigue y ataca cuerpo a cuerpo
//    (el golpe se puede hacer parry). Capa Enemy, Rigidbody2D con gravedad, sensores de suelo y pared.
// 3. Si la escena abierta es MecanicasEnemigos, reemplaza "Enemigo-Prueba" por el prefab.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class CrearEnemigoFantasmin
{
    const string Carpeta = "Assets/_Project/Art/Sprites/EnemigoFantasmin/";
    const string RutaMover = Carpeta + "fantasmin_mover.png";
    const string RutaAtaque = Carpeta + "fantasmin_ataque.png";
    const string RutaPrefab = "Assets/_Project/Prefabs/PF_EnemigoFantasmin.prefab";

    // Pivote = centro del cuerpo en cada hoja; PPU elegidos para que ambas hojas midan igual (~1.35 u)
    static readonly Vector2 PivoteMover = new Vector2(118f / 256f, 1f - 134f / 256f);
    static readonly Vector2 PivoteAtaque = new Vector2(240f / 768f, 1f - 382f / 544f);
    const float PPUMover = 134f, PPUAtaque = 120f;

    // Golpe medido sobre los cuadros 15-17: de la cabeza (x≈330) al final del estallido (x≈580)
    static readonly Vector2 OffsetGolpe = new Vector2((452f - 240f) / PPUAtaque, (382f - 340f) / PPUAtaque);
    static readonly Vector2 TamanoGolpe = new Vector2(250f / PPUAtaque, 190f / PPUAtaque);

    [MenuItem("Tools/Ultimo Descenso/Crear enemigo fantasmín (melee)")]
    public static void Crear()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal del modo Play primero."); return; }
        if (!File.Exists(RutaMover) || !File.Exists(RutaAtaque)) { Debug.LogError("Faltan las hojas en " + Carpeta); return; }

        var mover = Cortar(RutaMover, "fantasmin_mover", 5, 5, 256, 256, PPUMover, PivoteMover, 2048);
        var ataque = Cortar(RutaAtaque, "fantasmin_ataque", 5, 5, 768, 544, PPUAtaque, PivoteAtaque, 4096);
        if (mover.Length < 25 || ataque.Length < 25) { Debug.LogError("No se pudieron cortar las hojas."); return; }

        var prefab = CrearPrefab(mover, ataque);
        ReemplazarEnEscena(prefab);
    }

    static Sprite[] Cortar(string ruta, string nombre, int cols, int filas, int w, int h, float ppu, Vector2 pivote, int maxTam)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = ppu;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.maxTextureSize = maxTam; // sin reducir la hoja, si no los cortes no coinciden
        imp.filterMode = FilterMode.Bilinear;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var proveedor = factory.GetSpriteEditorDataProviderFromObject(imp);
        proveedor.InitSpriteEditorDataProvider();

        var rects = new SpriteRect[cols * filas];
        for (int i = 0; i < rects.Length; i++)
        {
            int c = i % cols, f = i / cols;
            rects[i] = new SpriteRect
            {
                name = nombre + "_" + i,
                spriteID = GUID.Generate(),
                rect = new Rect(c * w, (filas - 1 - f) * h, w, h), // Unity cuenta las filas desde abajo
                alignment = SpriteAlignment.Custom,
                pivot = pivote,
            };
        }
        proveedor.SetSpriteRects(rects);
        var nombres = proveedor.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nombres != null) nombres.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        proveedor.Apply();
        imp.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
            .ToArray();
    }

    static GameObject CrearPrefab(Sprite[] mover, Sprite[] ataque)
    {
        var raiz = new GameObject("PF_EnemigoFantasmin");
        raiz.layer = LayerMask.NameToLayer("Enemy");

        var sr = raiz.AddComponent<SpriteRenderer>();
        sr.sprite = mover[0];
        sr.sortingOrder = 3;

        var rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var cuerpo = raiz.AddComponent<CapsuleCollider2D>();
        cuerpo.direction = CapsuleDirection2D.Horizontal;
        cuerpo.size = new Vector2(1.25f, 0.95f);

        var sensorSuelo = Hijo(raiz, "SensorSuelo", new Vector2(0.55f, -0.4f), 0);
        var sensorPared = Hijo(raiz, "SensorPared", new Vector2(0.7f, 0f), 0);

        // Golpe cuerpo a cuerpo: se puede hacer parry (Basic)
        var golpe = Hijo(raiz, "Golpe", Vector2.zero, LayerMask.NameToLayer("EnemyAttack"));
        var caja = golpe.gameObject.AddComponent<BoxCollider2D>();
        caja.isTrigger = true;
        caja.offset = OffsetGolpe;
        caja.size = TamanoGolpe;
        var hit = golpe.gameObject.AddComponent<EnemyAttackHitbox>();
        var soHit = new SerializedObject(hit);
        soHit.FindProperty("attackType").enumValueIndex = (int)EnemyAttackType.Basic;
        soHit.FindProperty("damage").intValue = 1;
        soHit.ApplyModifiedPropertiesWithoutUndo();
        golpe.gameObject.SetActive(false);

        var enemigo = raiz.AddComponent<EnemigoBase>();
        var so = new SerializedObject(enemigo);
        so.FindProperty("velocidadPatrulla").floatValue = 1.5f;
        so.FindProperty("velocidadPersecucion").floatValue = 4f;
        so.FindProperty("sensorSuelo").objectReferenceValue = sensorSuelo;
        so.FindProperty("sensorPared").objectReferenceValue = sensorPared;
        so.FindProperty("capaSuelo").intValue = (1 << 3) | (1 << 6); // capaSuelo y Ground
        so.FindProperty("distanciaSuelo").floatValue = 0.6f;
        so.FindProperty("distanciaPared").floatValue = 0.2f;
        so.FindProperty("rangoDeteccion").floatValue = 7f;
        so.FindProperty("rangoPerdida").floatValue = 9f;
        so.FindProperty("tenirAlPerseguir").boolValue = false;
        so.FindProperty("hitboxAtaque").objectReferenceValue = golpe.gameObject;
        so.FindProperty("distanciaAtaque").floatValue = 2.2f;
        Cuadros(so, "framesMover", mover, 0, 24);
        Cuadros(so, "framesCarga", ataque, 5, 12);
        Cuadros(so, "framesGolpe", ataque, 13, 19);
        Cuadros(so, "framesRecuperar", ataque, 20, 24);
        so.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
        Object.DestroyImmediate(raiz);
        Debug.Log("Creado " + RutaPrefab);
        return prefab;
    }

    static Transform Hijo(GameObject padre, string nombre, Vector2 pos, int capa)
    {
        var go = new GameObject(nombre);
        go.layer = capa;
        go.transform.SetParent(padre.transform, false);
        go.transform.localPosition = pos;
        return go.transform;
    }

    static void Cuadros(SerializedObject so, string campo, Sprite[] s, int desde, int hasta)
    {
        var p = so.FindProperty(campo);
        p.arraySize = hasta - desde + 1;
        for (int i = desde; i <= hasta; i++) p.GetArrayElementAtIndex(i - desde).objectReferenceValue = s[i];
    }

    static void ReemplazarEnEscena(GameObject prefab)
    {
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "MecanicasEnemigos")
        {
            Debug.Log("Prefab listo: arrástralo a la escena (Assets/_Project/Prefabs/PF_EnemigoFantasmin).");
            Selection.activeObject = prefab;
            return;
        }

        var viejo = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "Enemigo-Prueba" && PrefabUtility.GetCorrespondingObjectFromSource(g) != prefab);
        var nuevo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, escena);
        nuevo.name = "Enemigo-Prueba";
        Undo.RegisterCreatedObjectUndo(nuevo, "Crear fantasmín");
        if (viejo != null)
        {
            nuevo.transform.position = viejo.transform.position;
            Undo.DestroyObjectImmediate(viejo);
            Debug.Log("Enemigo-Prueba reemplazado por el fantasmín. Guarda la escena.");
        }
        else
        {
            var vista = SceneView.lastActiveSceneView;
            var pos = vista != null ? vista.pivot : Vector3.zero;
            pos.z = 0f;
            nuevo.transform.position = pos;
            Debug.Log("Fantasmín colocado en el centro de la vista. Guarda la escena.");
        }
        EditorSceneManager.MarkSceneDirty(escena);
        Selection.activeObject = nuevo;
    }
}
