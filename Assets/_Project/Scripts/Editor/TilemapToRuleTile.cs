// Menu: Tools > Ultimo Descenso > Convertir Tilemap a Rule Tile
// Reemplaza TODOS los tiles pintados de un Tilemap por un solo Rule Tile (ej. RT_Circulo9).
// Se puede deshacer con Ctrl+Z.
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapToRuleTile : EditorWindow
{
    Tilemap tilemap;
    TileBase ruleTile;

    [MenuItem("Tools/Ultimo Descenso/Convertir Tilemap a Rule Tile")]
    static void Open() => GetWindow<TilemapToRuleTile>("Tilemap a Rule Tile");

    void OnEnable()
    {
        if (Selection.activeGameObject != null)
            tilemap = Selection.activeGameObject.GetComponent<Tilemap>();
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox("Arrastra el Tilemap de la escena y el Rule Tile. Todos los tiles pintados se reemplazan.", MessageType.Info);
        tilemap = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tilemap, typeof(Tilemap), true);
        ruleTile = (TileBase)EditorGUILayout.ObjectField("Rule Tile", ruleTile, typeof(TileBase), false);

        EditorGUI.BeginDisabledGroup(tilemap == null || ruleTile == null);
        if (GUILayout.Button("Convertir", GUILayout.Height(32))) Convert();
        EditorGUI.EndDisabledGroup();
    }

    void Convert()
    {
        Undo.RegisterCompleteObjectUndo(tilemap, "Convertir Tilemap a Rule Tile");

        tilemap.CompressBounds();
        BoundsInt b = tilemap.cellBounds;
        int count = 0;
        foreach (Vector3Int pos in b.allPositionsWithin)
        {
            if (tilemap.GetTile(pos) == null) continue;
            tilemap.SetTile(pos, ruleTile);
            count++;
        }

        tilemap.RefreshAllTiles();
        EditorUtility.SetDirty(tilemap);
        ShowNotification(new GUIContent("Listo: " + count + " tiles convertidos"));
    }
}
