// Menu: Tools > Ultimo Descenso > Configurar Arena Boss 2
// Arma la arena del jefe 2 con la estetica de castillo infernal. Abre SC_Boss2 y presiona Configurar.
//  - Importa los PNG de Assets/_Project/Art/ArenaBoss2
//  - Crea el tile "Pared" (TilePared) y reemplaza los tiles de Tilemap_Suelo
//  - Fondo del castillo con parallax y franja de demonios al frente
//  - "Regenerar bordes": mira donde hay tiles y pone solo la orilla de roca en el suelo,
//    el borde irregular en las paredes y techo, y las esquinas redondeadas.
//    Pintas con el tile y luego presionas "Regenerar bordes".
// Se puede repetir sin duplicar nada.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class ConfigurarArenaBoss2 : EditorWindow
{
    const string RootName = "ArenaBoss2_Visual";
    const string BordesName = "ArenaBoss2_Bordes";

    string carpeta = "Assets/_Project/Art/ArenaBoss2";
    string carpetaTiles = "Assets/_Project/Tiles/ArenaBoss2";

    float sueloY = -1f;                 // superficie del suelo
    float arenaMinX = -18f;             // borde interior izquierdo
    float arenaMaxX = 19f;              // borde interior derecho
    float arenaTechoY = 18f;            // borde interior del techo

    float fondoAncho = 55f;
    float fondoParallax = 0.6f;

    float demoniosBaseY = -8.7f;        // sube este numero para ver mas los demonios
    float demoniosAmplitud = 1.8f;      // cuanto se mueve de lado a lado
    float demoniosPeriodo = 14f;        // segundos por ida y vuelta
    float demoniosAncho = 70f;
    float demoniosParallax = 1.1f;

    bool apagarDecorViejo = true;
    bool ponerAmbienteInfierno = true;

    const float PPU_TILE = 256f;
    const float PPU_CAP = 200f;
    const float CAP_PIVOT_Y = 216f / 361f;
    const float PPU_DEMONIOS = 125f;
    const float PPU_BORDE = 256f;
    const float CORNER_SIZE = 1.5f;

    const int ORDEN_TILEMAP = -3;
    const int ORDEN_BORDES = -2;
    const int ORDEN_CAP = -1;

    [MenuItem("Tools/Ultimo Descenso/Configurar Arena Boss 2")]
    static void Open() => GetWindow<ConfigurarArenaBoss2>("Arena Boss 2");

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Configurar: arma todo (fondo, tiles, demonios, bordes).\nRegenerar bordes: solo rehace orillas y esquinas despues de pintar con el tile Pared.", MessageType.Info);
        carpeta = EditorGUILayout.TextField("Carpeta de arte", carpeta);
        carpetaTiles = EditorGUILayout.TextField("Carpeta de tiles", carpetaTiles);
        sueloY = EditorGUILayout.FloatField("Superficie del suelo (Y)", sueloY);
        arenaMinX = EditorGUILayout.FloatField("Borde interior izquierdo (X)", arenaMinX);
        arenaMaxX = EditorGUILayout.FloatField("Borde interior derecho (X)", arenaMaxX);
        arenaTechoY = EditorGUILayout.FloatField("Borde interior del techo (Y)", arenaTechoY);
        fondoAncho = EditorGUILayout.FloatField("Ancho del fondo", fondoAncho);
        fondoParallax = EditorGUILayout.Slider("Parallax del fondo", fondoParallax, 0f, 1.5f);
        demoniosBaseY = EditorGUILayout.FloatField("Demonios: Y de la base", demoniosBaseY);
        demoniosAncho = EditorGUILayout.FloatField("Demonios: ancho", demoniosAncho);
        demoniosParallax = EditorGUILayout.Slider("Demonios: parallax", demoniosParallax, 0f, 1.5f);
        demoniosAmplitud = EditorGUILayout.FloatField("Demonios: movimiento (unidades)", demoniosAmplitud);
        demoniosPeriodo = EditorGUILayout.FloatField("Demonios: segundos por ciclo", demoniosPeriodo);
        apagarDecorViejo = EditorGUILayout.Toggle("Apagar decorado de hielo", apagarDecorViejo);
        ponerAmbienteInfierno = EditorGUILayout.Toggle("Poner ambiente de infierno", ponerAmbienteInfierno);

        if (GUILayout.Button("Configurar", GUILayout.Height(32))) Configurar();
        if (GUILayout.Button("Regenerar bordes", GUILayout.Height(26))) RegenerarBordes(true);
    }

    // ------------------------------------------------------------------ CONFIGURAR
    void Configurar()
    {
        string pFondo = carpeta + "/fondo.png";
        string pDem = carpeta + "/demonios.png";
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(pFondo) == null)
        {
            ShowNotification(new GUIContent("No encuentro " + pFondo));
            return;
        }

        ImportarTodo();
        AssetDatabase.Refresh();

        Sprite sFondo = AssetDatabase.LoadAssetAtPath<Sprite>(pFondo);
        Sprite sDem = AssetDatabase.LoadAssetAtPath<Sprite>(pDem);
        if (sFondo == null || sDem == null)
        {
            ShowNotification(new GUIContent("Faltan PNG (fondo o demonios)"));
            return;
        }

        TilePared tilePared = CrearTilePared();
        TilePared tileSuelo = CrearTileSuelo();

        var old = GameObject.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Arena Boss 2");

        Material unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        float centroX = (arenaMinX + arenaMaxX) * 0.5f;

        // Fondo
        {
            var go = new GameObject("Fondo_Castillo");
            go.transform.SetParent(root.transform, false);
            float escala = fondoAncho / sFondo.bounds.size.x;
            float alto = sFondo.bounds.size.y * escala;
            float baseY = sueloY - 0.2f * alto;
            go.transform.position = new Vector3(centroX, baseY + alto * 0.5f, 10f);
            go.transform.localScale = new Vector3(escala, escala, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sFondo;
            sr.sortingLayerName = "Background";
            sr.sortingOrder = -50;
            if (unlit != null) sr.sharedMaterial = unlit;
            var p = go.AddComponent<CapaParallax>();
            p.factor = fondoParallax;
            p.alinearEnCentro = false;
        }

        // Demonios al frente (el padre lleva el parallax, el hijo se balancea)
        {
            var go = new GameObject("Demonios_Frente");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(centroX, demoniosBaseY, 0f);
            var p = go.AddComponent<CapaParallax>();
            p.factor = demoniosParallax;
            p.tambienVertical = false;
            p.alinearEnCentro = false;

            var hijo = new GameObject("Demonios_Sprite");
            hijo.transform.SetParent(go.transform, false);
            var sr = hijo.AddComponent<SpriteRenderer>();
            sr.sprite = sDem;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(demoniosAncho, sDem.bounds.size.y);
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 50;
            if (unlit != null) sr.sharedMaterial = unlit;
            var osc = hijo.AddComponent<OscilacionSuave>();
            osc.amplitud = new Vector2(demoniosAmplitud, 0.06f);
            osc.periodo = new Vector2(demoniosPeriodo, 5f);
        }

        // Tilemaps
        int tilesCambiados = 0;
        var goSuelo = GameObject.Find("Tilemap_Suelo");
        var tm = goSuelo != null ? goSuelo.GetComponent<Tilemap>() : null;
        if (tm != null)
        {
            Undo.RegisterCompleteObjectUndo(tm, "Tiles arena boss 2");
            tm.CompressBounds();
            foreach (var pos in tm.cellBounds.allPositionsWithin)
            {
                if (!tm.HasTile(pos)) continue;
                // debajo de la superficie (en toda la zona, incluidos los pasillos de entrada y salida) = roca del suelo; lo demas = ladrillo
                bool esSuelo = pos.y < Mathf.RoundToInt(sueloY);
                tm.SetTile(pos, esSuelo && tileSuelo != null ? tileSuelo : tilePared);
                tilesCambiados++;
            }
            var rend = goSuelo.GetComponent<TilemapRenderer>();
            if (rend != null) { Undo.RecordObject(rend, "Orden"); rend.sortingOrder = ORDEN_TILEMAP; }
            EditorUtility.SetDirty(tm);
        }
        else Debug.LogWarning("No encontre Tilemap_Suelo");

        var goEsq = GameObject.Find("Tilemap_Esquinas");
        if (goEsq != null)
        {
            var tmE = goEsq.GetComponent<Tilemap>();
            if (tmE != null) { Undo.RegisterCompleteObjectUndo(tmE, "Esquinas"); tmE.ClearAllTiles(); }
        }

        if (apagarDecorViejo)
        {
            string[] nombres = { "BG_Far", "Decor_rocas_nieve", "decor_rocas_nieve_1", "decor_roca_1", "Cristales_Brillantes", "PF_Ambiente" };
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                foreach (string n in nombres)
                    if (t.name == n && t.gameObject.activeSelf)
                    {
                        Undo.RecordObject(t.gameObject, "Apagar decorado");
                        t.gameObject.SetActive(false);
                    }
        }
        if (ponerAmbienteInfierno)
        {
            bool yaHay = false;
            foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (t.name.Contains("AmbienteInfierno")) { yaHay = true; break; }
            if (!yaHay)
            {
                var pf = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PF_AmbienteInfierno.prefab");
                if (pf != null) PrefabUtility.InstantiatePrefab(pf, SceneManager.GetActiveScene());
                else Debug.LogWarning("No encontre PF_AmbienteInfierno.prefab");
            }
        }

        // El resplandor y el humo "de atras" del ambiente van detras del suelo, asi no tiñen solo una parte
        var amb = FindFirstObjectByType<AmbienteInfierno>();
        if (amb != null)
        {
            var so = new SerializedObject(amb);
            foreach (string campo in new[] { "glowBackOrder", "smokeBackOrder", "farOrder" })
            {
                var prop = so.FindProperty(campo);
                if (prop != null) prop.intValue = -5;
            }
            so.ApplyModifiedProperties();
        }

        // Limpiar tiles viejos de la version anterior de la herramienta
        string[] viejos = new string[17];
        int k = 0;
        for (int c = 0; c < 4; c++) for (int r = 0; r < 4; r++) viejos[k++] = carpetaTiles + "/Pared_" + c + "_" + r + ".asset";
        viejos[k] = carpetaTiles + "/Suelo_Colision.asset";
        foreach (var v in viejos)
            if (AssetDatabase.LoadAssetAtPath<Object>(v) != null) AssetDatabase.DeleteAsset(v);

        for (int c = 0; c < 4; c++)
        {
            string viejo = carpeta + "/suelo_" + c + ".png";
            if (AssetDatabase.LoadAssetAtPath<Object>(viejo) != null) AssetDatabase.DeleteAsset(viejo);
        }

        RegenerarBordes(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        ShowNotification(new GUIContent("Listo: " + tilesCambiados + " tiles"));
    }

    // ------------------------------------------------------------------ BORDES
    void RegenerarBordes(bool avisar)
    {
        ImportarTodo();
        AssetDatabase.Refresh();

        Sprite sCap = AssetDatabase.LoadAssetAtPath<Sprite>(carpeta + "/suelo_cap.png");
        Sprite sLipPared = AssetDatabase.LoadAssetAtPath<Sprite>(carpeta + "/lip_pared.png");
        Sprite sLipTecho = AssetDatabase.LoadAssetAtPath<Sprite>(carpeta + "/lip_techo.png");
        Sprite sCorner = AssetDatabase.LoadAssetAtPath<Sprite>(carpeta + "/corner.png");
        if (sCap == null || sLipPared == null || sLipTecho == null || sCorner == null)
        {
            ShowNotification(new GUIContent("Faltan PNG (suelo_cap, lip_pared, lip_techo o corner)"));
            return;
        }

        var goSuelo = GameObject.Find("Tilemap_Suelo");
        var tm = goSuelo != null ? goSuelo.GetComponent<Tilemap>() : null;
        if (tm == null) { ShowNotification(new GUIContent("No encontre Tilemap_Suelo")); return; }

        var old = GameObject.Find(BordesName);
        if (old != null) Undo.DestroyObjectImmediate(old);
        var root = new GameObject(BordesName);
        Undo.RegisterCreatedObjectUndo(root, "Bordes arena boss 2");

        tm.RefreshAllTiles();
        tm.CompressBounds();
        BoundsInt b = tm.cellBounds;
        int minX = Mathf.RoundToInt(arenaMinX), maxX = Mathf.RoundToInt(arenaMaxX) - 1;
        int minY = Mathf.RoundToInt(sueloY), maxY = Mathf.RoundToInt(arenaTechoY) - 1;

        bool Solido(int x, int y) => tm.HasTile(new Vector3Int(x, y, 0));
        // zona decorada: todo lo pintado a lo ancho (arena y pasillos de entrada/salida), desde la superficie del suelo hasta el techo
        bool Dentro(int x, int y) => x >= b.xMin && x < b.xMax && y >= minY && y <= maxY;

        float lipW = sLipPared.bounds.size.x;
        float lipH = sLipTecho.bounds.size.y;
        int cuenta = 0;

        // Orilla de roca (cara superior expuesta)
        for (int y = b.yMin; y < b.yMax; y++)
        {
            int x = b.xMin;
            while (x < b.xMax)
            {
                if (Solido(x, y) && !Solido(x, y + 1) && Dentro(x, y + 1))
                {
                    int x0 = x;
                    while (x < b.xMax && Solido(x, y) && !Solido(x, y + 1) && Dentro(x, y + 1)) x++;
                    float len = x - x0;
                    var go = Nuevo(root, "Cap");
                    go.transform.position = new Vector3(x0 + len * 0.5f, y + 1f, 0f);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = sCap;
                    sr.drawMode = SpriteDrawMode.Tiled;
                    sr.tileMode = SpriteTileMode.Continuous;
                    sr.size = new Vector2(len, sCap.bounds.size.y);
                    sr.sortingLayerName = "Default";
                    sr.sortingOrder = ORDEN_CAP;
                    cuenta++;
                }
                else x++;
            }
        }

        // Bordes verticales (cara derecha e izquierda expuestas)
        for (int x = b.xMin; x < b.xMax; x++)
        {
            for (int lado = 0; lado < 2; lado++)
            {
                int dx = lado == 0 ? 1 : -1;       // 0: vecino a la derecha vacio, 1: a la izquierda
                int y = b.yMin;
                while (y < b.yMax)
                {
                    if (Solido(x, y) && !Solido(x + dx, y) && Dentro(x + dx, y))
                    {
                        int y0 = y;
                        while (y < b.yMax && Solido(x, y) && !Solido(x + dx, y) && Dentro(x + dx, y)) y++;
                        float len = y - y0;
                        float xo = lado == 0 ? x + 1f : x - lipW;
                        ColocarTiled(root, "Borde_Pared", sLipPared, xo, y0, lipW, len, lado == 1, false, ORDEN_BORDES);
                        cuenta++;
                    }
                    else y++;
                }
            }
        }

        // Borde del techo (cara inferior expuesta)
        for (int y = b.yMin; y < b.yMax; y++)
        {
            int x = b.xMin;
            while (x < b.xMax)
            {
                if (Solido(x, y) && !Solido(x, y - 1) && Dentro(x, y - 1))
                {
                    int x0 = x;
                    while (x < b.xMax && Solido(x, y) && !Solido(x, y - 1) && Dentro(x, y - 1)) x++;
                    float len = x - x0;
                    ColocarTiled(root, "Borde_Techo", sLipTecho, x0, y - lipH, len, lipH, false, false, ORDEN_BORDES);
                    cuenta++;
                }
                else x++;
            }
        }

        // Esquinas redondeadas (celda vacia con dos lados solidos)
        for (int y = minY; y <= maxY; y++)
            for (int x = b.xMin; x < b.xMax; x++)
            {
                if (Solido(x, y)) continue;
                bool izq = Solido(x - 1, y), der = Solido(x + 1, y), arr = Solido(x, y + 1), aba = Solido(x, y - 1);
                float s = CORNER_SIZE;
                if (izq && arr) { Esquina(root, sCorner, x, y + 1f - s, s, false, false); cuenta++; }
                if (der && arr) { Esquina(root, sCorner, x + 1f - s, y + 1f - s, s, true, false); cuenta++; }
                if (izq && aba) { Esquina(root, sCorner, x, y, s, false, true); cuenta++; }
                if (der && aba) { Esquina(root, sCorner, x + 1f - s, y, s, true, true); cuenta++; }
            }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        if (avisar) ShowNotification(new GUIContent("Bordes listos: " + cuenta + " piezas"));
    }

    static GameObject Nuevo(GameObject root, string nombre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(root.transform, false);
        return go;
    }

    // Pivote del sprite en la esquina inferior izquierda. Con flip el sprite crece hacia el lado contrario del pivote.
    static void ColocarTiled(GameObject root, string nombre, Sprite sp, float x0, float y0, float w, float h, bool flipX, bool flipY, int orden)
    {
        var go = Nuevo(root, nombre);
        go.transform.position = new Vector3(flipX ? x0 + w : x0, flipY ? y0 + h : y0, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(w, h);
        sr.flipX = flipX;
        sr.flipY = flipY;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = orden;
    }

    static void Esquina(GameObject root, Sprite sp, float x0, float y0, float s, bool flipX, bool flipY)
    {
        var go = Nuevo(root, "Esquina");
        go.transform.position = new Vector3(flipX ? x0 + s : x0, flipY ? y0 + s : y0, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.flipX = flipX;
        sr.flipY = flipY;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = ORDEN_BORDES;
    }

    // ------------------------------------------------------------------ ASSETS
    string PiezaPath(int c, int r) => carpeta + "/pared_" + c + "_" + r + ".png";

    void ImportarTodo()
    {
        Importar(carpeta + "/fondo.png", 100f, new Vector2(0.5f, 0.5f), false, 4096, true);
        Importar(carpeta + "/suelo_cap.png", PPU_CAP, new Vector2(0.5f, CAP_PIVOT_Y), true, 4096, false);
        Importar(carpeta + "/demonios.png", PPU_DEMONIOS, new Vector2(0.5f, 0f), true, 8192, false);
        Importar(carpeta + "/lip_pared.png", PPU_BORDE, new Vector2(0f, 0f), true, 2048, false);
        Importar(carpeta + "/lip_techo.png", PPU_BORDE, new Vector2(0f, 0f), true, 2048, false);
        Importar(carpeta + "/corner.png", PPU_BORDE, new Vector2(0f, 0f), false, 1024, false);
        for (int c = 0; c < 4; c++)
            for (int r = 0; r < 4; r++)
                Importar(PiezaPath(c, r), PPU_TILE, new Vector2(0.5f, 0.5f), false, 512, false);
        for (int c = 0; c < 4; c++)
            Importar(carpeta + "/suelo_" + c + "_0.png", 171f, new Vector2(0.5f, 0.5f), false, 512, false);
    }

    TilePared CrearTilePared()
    {
        EnsureFolder(carpetaTiles);
        string path = carpetaTiles + "/Pared.asset";
        var t = AssetDatabase.LoadAssetAtPath<TilePared>(path);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<TilePared>();
            AssetDatabase.CreateAsset(t, path);
        }
        t.piezas = new Sprite[16];
        for (int c = 0; c < 4; c++)
            for (int r = 0; r < 4; r++)
                t.piezas[c * 4 + r] = AssetDatabase.LoadAssetAtPath<Sprite>(PiezaPath(c, r));
        t.colliderType = Tile.ColliderType.Grid;
        t.ocultarSiLibreArriba = false;
        EditorUtility.SetDirty(t);
        AssetDatabase.SaveAssets();
        return t;
    }

    TilePared CrearTileSuelo()
    {
        EnsureFolder(carpetaTiles);
        string path = carpetaTiles + "/Suelo.asset";
        var t = AssetDatabase.LoadAssetAtPath<TilePared>(path);
        if (t == null)
        {
            t = ScriptableObject.CreateInstance<TilePared>();
            AssetDatabase.CreateAsset(t, path);
        }
        t.piezas = new Sprite[16];
        for (int c = 0; c < 4; c++)
        {
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(carpeta + "/suelo_" + c + "_0.png");
            for (int r = 0; r < 4; r++) t.piezas[c * 4 + r] = sp;
        }
        t.colliderType = Tile.ColliderType.Grid;
        t.ocultarSiLibreArriba = true;   // la fila de arriba la dibuja la orilla de roca (Suelo_Cap)
        EditorUtility.SetDirty(t);
        AssetDatabase.SaveAssets();
        return t;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    static void Importar(string path, float ppu, Vector2 pivot, bool repetir, int maxSize, bool comprimir)
    {
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;

        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;

        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.spritePivot = pivot;

        imp.wrapMode = repetir ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        imp.maxTextureSize = maxSize;
        imp.textureCompression = comprimir ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.filterMode = FilterMode.Bilinear;
        imp.isReadable = false;
        imp.SaveAndReimport();
    }
}
