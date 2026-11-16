// Menu: Tools > Ultimo Descenso > Boss Sheet Setup
// Corta un sprite sheet del jefe, crea el AnimationClip y lo agrega a AC_Boss1 con sus transiciones.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class BossSheetSetup : EditorWindow
{
    class Preset
    {
        public string label, clip, trigger = "";
        public int cols = 5, rows = 5, first, last, texMaxSize = 2048;
        public float fps, ppu, pivotY = 0.02f;
        public bool loop, isIdle, pingPong;
    }

    static readonly Preset[] presets =
    {
        new Preset { label = "Idle (respirando 5x5)",        clip = "Boss_Idle",        cols = 5, rows = 5, first = 0, last = 24, fps = 12, ppu = 24, loop = true,  isIdle = true },
        new Preset { label = "Ataque mano derecha (7x7)",    clip = "Boss_AttackRight", cols = 7, rows = 7, first = 0, last = 48, fps = 30, ppu = 24, loop = false, trigger = "AttackRight" },
        new Preset { label = "Ataque mano izquierda (5x5, 512 px)", clip = "Boss_AttackLeft", cols = 5, rows = 5, first = 0, last = 24, fps = 30, ppu = 48, loop = false, trigger = "AttackLeft", texMaxSize = 4096 },
        new Preset { label = "Jefe 2 - Idle (5x5, 512 px)",        clip = "Boss2_Idle",   cols = 5, rows = 5, first = 0, last = 24, fps = 12, ppu = 48, loop = true,  isIdle = true, pingPong = true, texMaxSize = 4096, pivotY = 0.287f },
        new Preset { label = "Jefe 2 - Ataque basico (5x5, 512 px)", clip = "Boss2_Attack", cols = 5, rows = 5, first = 0, last = 24, fps = 24, ppu = 48, loop = false, trigger = "Attack", texMaxSize = 4096, pivotY = 0.287f },
        new Preset { label = "Jefe 2 - Manos arriba (4x4, 512 px)", clip = "Boss2_RaiseHands", cols = 4, rows = 4, first = 0, last = 15, fps = 20, ppu = 48, loop = false, trigger = "RaiseHands", texMaxSize = 4096, pivotY = 0.287f },
        new Preset { label = "Personalizado",                clip = "Boss_Nuevo",       cols = 5, rows = 5, first = 0, last = 24, fps = 12, ppu = 24, loop = false },
    };

    // La carpeta de animaciones y el controller se calculan segun la carpeta del sprite sheet
    // (Sprites/Boss1/... -> AC_Boss1, Sprites/Boss2/... -> AC_Boss2)
    string AnimFolder, ControllerPath, bossFolderName;
    string IdleName => bossFolderName == "Boss1" ? "Boss_Idle" : bossFolderName + "_Idle";

    Texture2D sheet;
    int presetIndex;
    int cols = 5, rows = 5, first, last = 24, texMaxSize = 2048;
    float fps = 12, ppu = 24, pivotY = 0.02f;
    bool loop = true, isIdle, pingPong;
    string clipName = "Boss_Idle", trigger = "";

    [MenuItem("Tools/Ultimo Descenso/Boss Sheet Setup")]
    static void Open() => GetWindow<BossSheetSetup>("Boss Sheet Setup");

    void OnEnable() => LoadPreset(presets[presetIndex]);

    void OnGUI()
    {
        sheet = (Texture2D)EditorGUILayout.ObjectField("Sprite sheet (PNG)", sheet, typeof(Texture2D), false);

        EditorGUI.BeginChangeCheck();
        presetIndex = EditorGUILayout.Popup("Preset", presetIndex, presets.Select(p => p.label).ToArray());
        if (EditorGUI.EndChangeCheck()) LoadPreset(presets[presetIndex]);

        EditorGUILayout.Space();
        cols = EditorGUILayout.IntField("Columnas", cols);
        rows = EditorGUILayout.IntField("Filas", rows);
        ppu = EditorGUILayout.FloatField("Pixels Per Unit", ppu);
        pivotY = EditorGUILayout.FloatField("Pivot Y (pies)", pivotY);
        texMaxSize = EditorGUILayout.IntField("Max Texture Size", texMaxSize);

        EditorGUILayout.Space();
        clipName = EditorGUILayout.TextField("Nombre del clip", clipName);
        first = EditorGUILayout.IntField("Frame inicial", first);
        last = EditorGUILayout.IntField("Frame final", last);
        fps = EditorGUILayout.FloatField("FPS", fps);
        loop = EditorGUILayout.Toggle("Loop", loop);
        pingPong = EditorGUILayout.Toggle("Ida y vuelta (ping-pong)", pingPong);
        isIdle = EditorGUILayout.Toggle("Es el estado Idle (por defecto)", isIdle);
        if (!isIdle) trigger = EditorGUILayout.TextField("Trigger del ataque", trigger);

        EditorGUILayout.Space();
        if (GUILayout.Button("Aplicar", GUILayout.Height(32))) Apply();
    }

    void LoadPreset(Preset p)
    {
        clipName = p.clip; cols = p.cols; rows = p.rows; first = p.first; last = p.last;
        fps = p.fps; ppu = p.ppu; pivotY = p.pivotY; loop = p.loop; trigger = p.trigger;
        isIdle = p.isIdle; texMaxSize = p.texMaxSize; pingPong = p.pingPong;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    void Apply()
    {
        if (sheet == null) { ShowNotification(new GUIContent("Falta el sprite sheet")); return; }
        string path = AssetDatabase.GetAssetPath(sheet);
        string dir = Path.GetDirectoryName(path).Replace('\\', '/');
        bossFolderName = Path.GetFileName(dir);
        AnimFolder = dir + "/Animations";
        ControllerPath = AnimFolder + "/AC_" + bossFolderName + ".controller";

        ConfigureAndSlice(path);

        var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .Select(s => new { s, i = IndexOf(s.name) })
            .Where(x => x.i >= first && x.i <= last)
            .OrderBy(x => x.i).Select(x => x.s).ToArray();
        if (frames.Length == 0) { ShowNotification(new GUIContent("No hay frames en ese rango")); return; }

        if (pingPong && frames.Length > 2)
            frames = frames.Concat(frames.Skip(1).Take(frames.Length - 2).Reverse()).ToArray();

        EnsureFolder(AnimFolder);
        var clip = BuildClip(frames);
        AddToController(clip);
        AssetDatabase.SaveAssets();
        ShowNotification(new GUIContent("Listo: " + clipName + " (" + frames.Length + " frames)"));
    }

    static int IndexOf(string spriteName)
    {
        int u = spriteName.LastIndexOf('_');
        return u >= 0 && int.TryParse(spriteName.Substring(u + 1), out int i) ? i : -1;
    }

    void ConfigureAndSlice(string path)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Bilinear;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.maxTextureSize = texMaxSize;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();

        var tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tmp.LoadImage(File.ReadAllBytes(path));
        int w = tmp.width, h = tmp.height;
        int cw = w / cols, ch = h / rows;
        var px = tmp.GetPixels32();
        DestroyImmediate(tmp);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();

        var existing = new Dictionary<string, SpriteRect>();
        foreach (var r in dp.GetSpriteRects()) existing[r.name] = r;

        string baseName = Path.GetFileNameWithoutExtension(path);
        var list = new List<SpriteRect>();
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                int idx = r * cols + c;
                int x0 = c * cw;
                int y0 = (rows - 1 - r) * ch;
                if (CellEmpty(px, w, x0, y0, cw, ch)) continue;

                string n = baseName + "_" + idx;
                SpriteRect sr;
                if (!existing.TryGetValue(n, out sr))
                    sr = new SpriteRect { name = n, spriteID = UnityEditor.GUID.Generate() };
                sr.rect = new Rect(x0, y0, cw, ch);
                sr.alignment = SpriteAlignment.Custom;
                sr.pivot = new Vector2(0.5f, pivotY);
                list.Add(sr);
            }
        }
        dp.SetSpriteRects(list.ToArray());

        var nameProvider = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider != null)
            nameProvider.SetNameFileIdPairs(list.Select(s => new SpriteNameFileIdPair(s.name, s.spriteID)).ToList());

        dp.Apply();
        ((AssetImporter)ti).SaveAndReimport();
    }

    static bool CellEmpty(Color32[] px, int w, int x0, int y0, int cw, int ch)
    {
        for (int y = y0; y < y0 + ch; y += 3)
            for (int x = x0; x < x0 + cw; x += 3)
                if (px[y * w + x].a > 10) return false;
        return true;
    }

    AnimationClip BuildClip(Sprite[] frames)
    {
        string clipPath = AnimFolder + "/" + clipName + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNew = clip == null;
        if (isNew) clip = new AnimationClip();

        clip.frameRate = fps;
        var keys = new List<ObjectReferenceKeyframe>();
        for (int i = 0; i < frames.Length; i++)
            keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = frames[i] });
        keys.Add(new ObjectReferenceKeyframe { time = frames.Length / fps, value = loop ? frames[0] : frames[frames.Length - 1] });

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        if (isNew) AssetDatabase.CreateAsset(clip, clipPath);
        else EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimatorState Find(AnimatorStateMachine sm, string n) =>
        sm.states.Select(s => s.state).FirstOrDefault(s => s.name == n);

    void AddToController(AnimationClip clip)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        if (bossFolderName == "Boss1")
            foreach (var t in new[] { "AttackRight", "AttackLeft" })
                if (!ctrl.parameters.Any(p => p.name == t))
                    ctrl.AddParameter(t, AnimatorControllerParameterType.Trigger);
        if (!string.IsNullOrEmpty(trigger) && !ctrl.parameters.Any(p => p.name == trigger))
            ctrl.AddParameter(trigger, AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var state = Find(sm, clipName);
        if (state == null) state = sm.AddState(clipName);
        state.motion = clip;

        if (isIdle)
        {
            sm.defaultState = state;
        }
        else
        {
            foreach (var old in sm.anyStateTransitions.Where(t => t.destinationState == state).ToArray())
                sm.RemoveAnyStateTransition(old);

            var any = sm.AddAnyStateTransition(state);
            any.hasExitTime = false;
            any.hasFixedDuration = true;
            any.duration = 0f;
            any.canTransitionToSelf = false;
            any.AddCondition(AnimatorConditionMode.If, 0f, trigger);

            var idle = Find(sm, IdleName);
            if (idle != null)
            {
                foreach (var old in state.transitions.Where(t => t.destinationState == idle).ToArray())
                    state.RemoveTransition(old);
                var back = state.AddTransition(idle);
                back.hasExitTime = true;
                back.exitTime = 1f;
                back.hasFixedDuration = true;
                back.duration = 0f;
            }
            else
            {
                Debug.LogWarning("Aun no existe " + IdleName + ": aplica primero el preset Idle y vuelve a aplicar este.");
            }
        }
        EditorUtility.SetDirty(ctrl);
    }
}
