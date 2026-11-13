// Menu: Tools > Ultimo Descenso > Generar Esquinas Interiores
// Busca en el Tilemap del suelo las esquinas interiores (suelo/techo que choca con una pared)
// y coloca una pieza redondeada en una capa aparte (Tilemap_Esquinas). Se puede repetir cuando cambies el mapa.
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GenerarEsquinasInteriores : EditorWindow
{
    const string Folder = "Assets/_Project/Tiles/";
    const string LayerName = "Tilemap_Esquinas";

    Tilemap ground;

    [MenuItem("Tools/Ultimo Descenso/Generar Esquinas Interiores")]
    static void Open() => GetWindow<GenerarEsquinasInteriores>("Esquinas interiores");

    void OnEnable()
    {
        if (Selection.activeGameObject != null)
            ground = Selection.activeGameObject.GetComponent<Tilemap>();
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Arrastra el Tilemap del suelo (Tilemap_Suelo). Se crea o actualiza la capa " + LayerName + ".", MessageType.Info);
        ground = (Tilemap)EditorGUILayout.ObjectField("Tilemap del suelo", ground, typeof(Tilemap), true);

        EditorGUI.BeginDisabledGroup(ground == null);
        if (GUILayout.Button("Generar", GUILayout.Height(32))) Generate();
        if (GUILayout.Button("Borrar esquinas", GUILayout.Height(24))) Clear();
        EditorGUI.EndDisabledGroup();
    }

    static TileBase Load(string name) => AssetDatabase.LoadAssetAtPath<TileBase>(Folder + name + ".asset");

    Tilemap GetOrCreateLayer()
    {
        Transform parent = ground.transform.parent;
        Transform t = parent != null ? parent.Find(LayerName) : null;
        if (t == null)
        {
            var go = new GameObject(LayerName);
            Undo.RegisterCreatedObjectUndo(go, "Crear " + LayerName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = ground.transform.localPosition;
            go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
            t = go.transform;
        }

        Tilemap map = t.GetComponent<Tilemap>();
        var gr = ground.GetComponent<TilemapRenderer>();
        var r = t.GetComponent<TilemapRenderer>();
        if (gr != null && r != null)
        {
            r.sortingLayerID = gr.sortingLayerID;
            r.sortingOrder = gr.sortingOrder + 1;
            r.mode = gr.mode;
            r.sharedMaterial = gr.sharedMaterial;
        }
        return map;
    }

    void Clear()
    {
        Transform parent = ground.transform.parent;
        Transform t = parent != null ? parent.Find(LayerName) : null;
        if (t == null) return;
        Tilemap map = t.GetComponent<Tilemap>();
        Undo.RegisterCompleteObjectUndo(map, "Borrar esquinas");
        map.ClearAllTiles();
    }

    void Generate()
    {
        TileBase floorRight = Load("esq_int_suelo_pared_der");
        TileBase floorLeft = Load("esq_int_suelo_pared_izq");
        TileBase ceilRight = Load("esq_int_techo_pared_der");
        TileBase ceilLeft = Load("esq_int_techo_pared_izq");
        if (floorRight == null || floorLeft == null || ceilRight == null || ceilLeft == null)
        {
            ShowNotification(new GUIContent("Faltan los tiles esq_int_* en " + Folder));
            return;
        }

        Tilemap map = GetOrCreateLayer();
        Undo.RegisterCompleteObjectUndo(map, "Generar esquinas interiores");
        map.ClearAllTiles();

        ground.CompressBounds();
        BoundsInt b = ground.cellBounds;
        int z = b.zMin;
        int count = 0;

        for (int x = b.xMin - 1; x <= b.xMax; x++)
        {
            for (int y = b.yMin - 1; y <= b.yMax; y++)
            {
                var p = new Vector3Int(x, y, z);
                if (ground.HasTile(p)) continue;

                bool down = Has(p, 0, -1), up = Has(p, 0, 1), left = Has(p, -1, 0), right = Has(p, 1, 0);

                bool a = down && right && Has(p, 1, -1);   // suelo abajo + pared a la derecha
                bool bb = down && left && Has(p, -1, -1);  // suelo abajo + pared a la izquierda
                bool c = up && right && Has(p, 1, 1);      // techo arriba + pared a la derecha
                bool d = up && left && Has(p, -1, 1);      // techo arriba + pared a la izquierda

                int n = (a ? 1 : 0) + (bb ? 1 : 0) + (c ? 1 : 0) + (d ? 1 : 0);
                if (n != 1) continue; // ninguna, o un hueco estrecho donde no cabe

                map.SetTile(p, a ? floorRight : bb ? floorLeft : c ? ceilRight : ceilLeft);
                count++;
            }
        }

        map.RefreshAllTiles();
        EditorUtility.SetDirty(map);
        ShowNotification(new GUIContent("Listo: " + count + " esquinas interiores"));
    }

    bool Has(Vector3Int p, int dx, int dy) => ground.HasTile(new Vector3Int(p.x + dx, p.y + dy, p.z));
}
