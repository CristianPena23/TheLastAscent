
// Menú: Tools > Ultimo Descenso > Player Sheet Setup
// Corta un sprite sheet en cuadrícula, crea el AnimationClip, lo agrega a AC_Player y crea las transiciones.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class PlayerSheetSetup : EditorWindow
{
    enum Link { None, Locomotion, ActionTrigger, AirRise, AirFall }

    class Preset
    {
        public string label, clip, trigger = "";
        public int first, last, texMaxSize = 4096, attackDir = -1;
        public float fps, ppu, pivotY = 0.14f;
        public bool loop, returnToIdle = true;
        public Link link;
    }

    static readonly Preset[] presets =
    {
        new Preset { label = "Run",        clip = "PL_Run",    first = 0, last = 24, fps = 30, ppu = 95,  loop = true,  link = Link.Locomotion },
        new Preset { label = "Parry",      clip = "PL_Parry",  first = 5, last = 14, fps = 30, ppu = 95,  loop = false, link = Link.ActionTrigger, trigger = "Parry" },
        new Preset { label = "Dash",       clip = "PL_Dash",   first = 0, last = 11, fps = 48, ppu = 95,  loop = false, link = Link.ActionTrigger, trigger = "Dash" },
        new Preset { label = "Jump (subida)", clip = "PL_JumpUp", first = 5, last = 8, fps = 20, ppu = 190, loop = false, link = Link.AirRise },
        new Preset { label = "Jump (caida)",  clip = "PL_Fall",   first = 10, last = 11, fps = 8, ppu = 190, loop = true,  link = Link.AirFall },
        new Preset { label = "Attack",     clip = "PL_Attack", first = 1, last = 12, fps = 36, ppu = 190, loop = false, link = Link.ActionTrigger, trigger = "Attack", texMaxSize = 2048, pivotY = 0.0625f, attackDir = 0 },
        new Preset { label = "Attack Up",   clip = "PL_AttackUp",   first = 7, last = 18, fps = 36, ppu = 95, loop = false, link = Link.ActionTrigger, trigger = "Attack", texMaxSize = 2048, pivotY = 0.14f, attackDir = 1 },
        new Preset { label = "Attack Down", clip = "PL_AttackDown", first = 5, last = 14, fps = 36, ppu = 95, loop = false, link = Link.ActionTrigger, trigger = "Attack", texMaxSize = 2048, pivotY = 0.14f, attackDir = 2 },
        new Preset { label = "Death",      clip = "PL_Death",  first = 0, last = 24, fps = 15, ppu = 190, loop = false, link = Link.ActionTrigger, trigger = "Die", returnToIdle = false },
        new Preset { label = "Personalizado", clip = "PL_Nuevo", first = 0, last = 24, fps = 12, ppu = 190, loop = true, link = Link.None },
    };

    Texture2D sheet;
    int presetIndex;
    int cols = 5, rows = 5, first, last = 24, texMaxSize = 4096, attackDir = -1;
    float fps = 12, ppu = 190, pivotY = 0.14f;
    bool loop = true, returnToIdle = true;
    string clipName = "PL_Nuevo", trigger = "";
    Link link;
    string animFolder = "Assets/_Project/Art/Sprites/Animations";
    string controllerPath = "Assets/_Project/Art/Sprites/Animations/AC_Player.controller";

    [MenuItem("Tools/Ultimo Descenso/Player Sheet Setup")]
    static void Open() => GetWindow<PlayerSheetSetup>("Player Sheet Setup");

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

        EditorGUILayout.Space();
        link = (Link)EditorGUILayout.EnumPopup("Transiciones", link);
        if (link == Link.ActionTrigger)
        {
            trigger = EditorGUILayout.TextField("Trigger", trigger);
            attackDir = EditorGUILayout.IntField("AttackDirection (-1 = ninguna, 0 = frente, 1 = arriba, 2 = abajo)", attackDir);
            returnToIdle = EditorGUILayout.Toggle("Volver a Idle al terminar", returnToIdle);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("Aplicar", GUILayout.Height(32))) Apply();
    }

    void LoadPreset(Preset p)
    {
        clipName = p.clip; first = p.first; last = p.last; fps = p.fps; ppu = p.ppu;
        pivotY = p.pivotY; loop = p.loop; link = p.link; trigger = p.trigger;
        returnToIdle = p.returnToIdle; texMaxSize = p.texMaxSize; attackDir = p.attackDir;
    }

    void Apply()
    {
        if (sheet == null) { ShowNotification(new GUIContent("Falta el sprite sheet")); return; }
        string path = AssetDatabase.GetAssetPath(sheet);

        ConfigureAndSlice(path);

        var frames = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .Select(s => new { s, i = IndexOf(s.name) })
            .Where(x => x.i >= first && x.i <= last)
            .OrderBy(x => x.i).Select(x => x.s).ToArray();
        if (frames.Length == 0) { ShowNotification(new GUIContent("No hay frames en ese rango")); return; }

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

        // Leer el PNG original para detectar celdas vacias.
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
                int y0 = (rows - 1 - r) * ch; // Unity cuenta Y desde abajo
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
        string clipPath = animFolder + "/" + clipName + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        bool isNew = clip == null;
        if (isNew) clip = new AnimationClip();

        clip.frameRate = fps;
        var keys = new List<ObjectReferenceKeyframe>();
        for (int i = 0; i < frames.Length; i++)
            keys.Add(new ObjectReferenceKeyframe { time = i / fps, value = frames[i] });
        // Clave final para que el ultimo frame dure lo mismo que los demas.
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

    static AnimatorStateTransition NewLink(AnimatorState from, AnimatorState to)
    {
        foreach (var old in from.transitions.Where(t => t.destinationState == to).ToArray())
            from.RemoveTransition(old);
        var t = from.AddTransition(to);
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = 0f;
        return t;
    }

    static void Param(AnimatorController c, string n, AnimatorControllerParameterType type)
    {
        if (!c.parameters.Any(p => p.name == n)) c.AddParameter(n, type);
    }

    void AddToController(AnimationClip clip)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (ctrl == null) ctrl = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        Param(ctrl, "Speed", AnimatorControllerParameterType.Float);
        Param(ctrl, "Grounded", AnimatorControllerParameterType.Bool);
        Param(ctrl, "VelocityY", AnimatorControllerParameterType.Float);
        Param(ctrl, "AttackDirection", AnimatorControllerParameterType.Int);
        foreach (var t in new[] { "Dash", "Attack", "Hurt", "Parry", "ParrySuccess", "Die" })
            Param(ctrl, t, AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var state = Find(sm, clipName);
        if (state == null) state = sm.AddState(clipName);
        state.motion = clip;

        var idle = Find(sm, "PL_Idle");
        if (link != Link.None && idle == null)
        {
            Debug.LogWarning("No existe el estado PL_Idle: no se crearon transiciones.");
            EditorUtility.SetDirty(ctrl);
            return;
        }

        switch (link)
        {
            case Link.Locomotion:
            {
                var a = NewLink(idle, state);
                a.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                var b = NewLink(state, idle);
                b.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
                break;
            }
            case Link.ActionTrigger:
            {
                foreach (var old in sm.anyStateTransitions.Where(t => t.destinationState == state).ToArray())
                    sm.RemoveAnyStateTransition(old);
                var any = sm.AddAnyStateTransition(state);
                any.hasExitTime = false;
                any.hasFixedDuration = true;
                any.duration = 0f;
                any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.If, 0f, trigger);
                if (attackDir >= 0) any.AddCondition(AnimatorConditionMode.Equals, attackDir, "AttackDirection");
                if (returnToIdle)
                {
                    var back = NewLink(state, idle);
                    back.hasExitTime = true;
                    back.exitTime = 1f;
                }
                break;
            }
            case Link.AirRise:
            case Link.AirFall:
            {
                var sources = new List<string> { "PL_Idle", "PL_Walk", "PL_Run" };
                if (link == Link.AirFall) sources.Add("PL_JumpUp");
                foreach (var n in sources)
                {
                    var from = Find(sm, n);
                    if (from == null || from == state) continue;
                    var t = NewLink(from, state);
                    t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Grounded");
                    if (link == Link.AirRise) t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VelocityY");
                    else t.AddCondition(AnimatorConditionMode.Less, -0.1f, "VelocityY");
                }
                var land = NewLink(state, idle);
                land.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");
                break;
            }
        }
        EditorUtility.SetDirty(ctrl);
    }
}
