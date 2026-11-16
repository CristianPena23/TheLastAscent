// Menu: Tools > Ultimo Descenso > Asignar fuego al jefe 2
// Carga los frames de fuego (Art/Sprites/Boss2/Fuego) en el Boss2AttackController de la escena abierta.
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AsignarFuegoJefe2
{
    const string Folder = "Assets/_Project/Art/Sprites/Boss2/Fuego";

    [MenuItem("Tools/Ultimo Descenso/Asignar fuego al jefe 2")]
    static void Run()
    {
        var boss = Object.FindFirstObjectByType<Boss2AttackController>();
        if (boss == null)
        {
            EditorUtility.DisplayDialog("Fuego", "No encontre Boss2AttackController en la escena abierta (abre SC_Boss2).", "OK");
            return;
        }

        Sprite[] Load(string prefix) =>
            AssetDatabase.FindAssets(prefix + " t:Sprite", new[] { Folder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p, new NaturalCompare())
                .Select(AssetDatabase.LoadAssetAtPath<Sprite>)
                .Where(s => s != null).ToArray();

        var col = Load("fuego_columna");
        var ball = Load("fuego_bola");
        if (col.Length == 0 || ball.Length == 0)
        {
            EditorUtility.DisplayDialog("Fuego", "No encontre los sprites en " + Folder + ". Espera a que Unity termine de importarlos.", "OK");
            return;
        }

        var so = new SerializedObject(boss);
        Fill(so.FindProperty("columnFrames"), col);
        Fill(so.FindProperty("ballFrames"), ball);
        so.FindProperty("fireFps").floatValue = 12f;
        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Fuego asignado: " + col.Length + " frames de columna y " + ball.Length + " de bola.");
    }

    static void Fill(SerializedProperty arr, Sprite[] sprites)
    {
        arr.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    // fuego_bola_2 antes que fuego_bola_10
    class NaturalCompare : System.Collections.Generic.IComparer<string>
    {
        public int Compare(string a, string b)
        {
            int na = Num(a), nb = Num(b);
            return na != nb ? na.CompareTo(nb) : string.CompareOrdinal(a, b);
        }
        static int Num(string p)
        {
            string n = System.IO.Path.GetFileNameWithoutExtension(p);
            int u = n.LastIndexOf('_');
            return u >= 0 && int.TryParse(n.Substring(u + 1), out int v) ? v : 0;
        }
    }
}
