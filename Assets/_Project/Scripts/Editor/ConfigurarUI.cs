// Menú: Tools > Ultimo Descenso > Configurar UI
// Crea el tema, los prefabs de la interfaz (pausa, opciones, muerte), la escena SC_MenuPrincipal
// y agrega pausa + muerte al PF_HUD. Solo crea lo que falta: si un asset ya existe, lo deja como está.
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ConfigurarUI
{
    const string RutaTema = "Assets/_Project/Settings/UI_Tema.asset";
    const string CarpetaPrefabs = "Assets/_Project/Prefabs";
    const string RutaOpciones = CarpetaPrefabs + "/PF_PanelOpciones.prefab";
    const string RutaPausa = CarpetaPrefabs + "/PF_MenuPausa.prefab";
    const string RutaMuerte = CarpetaPrefabs + "/PF_PantallaMuerte.prefab";
    const string RutaHUD = CarpetaPrefabs + "/PF_HUD.prefab";
    const string RutaEscenaMenu = "Assets/_Project/Scenes/SC_MenuPrincipal.unity";
    const string RutaFondo = "Assets/_Project/Art/Backgrounds/bg_circulo9_far.png";

    const int OrdenPausa = 50;
    const int OrdenMuerte = 45;

    static TemaUI tema;

    [MenuItem("Tools/Ultimo Descenso/Configurar UI")]
    public static void ConfigurarTodo()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal del modo Play antes de configurar la UI."); return; }

        tema = CrearTema();
        var opciones = CrearPrefabOpciones();
        var pausa = CrearPrefabPausa(opciones);
        var muerte = CrearPrefabMuerte();
        AgregarAlHUD(pausa, muerte);
        CrearEscenaMenu(opciones);
        AssetDatabase.SaveAssets();
        Debug.Log("UI configurada. Revisa los prefabs PF_MenuPausa / PF_PantallaMuerte / PF_PanelOpciones y la escena SC_MenuPrincipal.");
    }

    // ---------- Tema ----------

    static TemaUI CrearTema()
    {
        var t = AssetDatabase.LoadAssetAtPath<TemaUI>(RutaTema);
        if (t != null) return t;
        t = ScriptableObject.CreateInstance<TemaUI>();
        AssetDatabase.CreateAsset(t, RutaTema);
        Debug.Log("Creado " + RutaTema);
        return t;
    }

    // ---------- Prefabs ----------

    static GameObject CrearPrefabOpciones()
    {
        var existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaOpciones);
        if (existente != null) return existente;

        var raiz = Nuevo("PF_PanelOpciones", null);
        Estirar(raiz);
        Img(raiz, tema.velo);
        raiz.AddComponent<CanvasGroup>();
        var panel = raiz.AddComponent<PanelOpciones>();

        var caja = Caja(raiz.transform);
        Texto(caja, "Titulo", "OPCIONES", 52, tema.acento, 72);
        Separador(caja);

        // Volumen
        var filaVol = Fila(caja, "Fila_Volumen");
        var lblVol = Etiqueta(filaVol, "VOLUMEN");
        var slider = CrearSlider(filaVol);
        Efecto(slider.gameObject, lblVol, Marcador(filaVol));

        // Pantalla completa
        var filaPc = Fila(caja, "Fila_PantallaCompleta");
        var lblPc = Etiqueta(filaPc, "PANTALLA COMPLETA");
        var toggle = CrearToggle(filaPc);
        Efecto(toggle.gameObject, lblPc, Marcador(filaPc));

        Separador(caja);
        var volver = Boton(caja, "Volver");

        Asignar(panel, "sliderVolumen", slider);
        Asignar(panel, "togglePantallaCompleta", toggle);
        Asignar(panel, "botonVolver", volver);
        Asignar(panel, "primerSeleccionado", slider);

        return Guardar(raiz, RutaOpciones);
    }

    static GameObject CrearPrefabPausa(GameObject prefabOpciones)
    {
        var existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPausa);
        if (existente != null) return existente;

        var raiz = CrearCanvas("PF_MenuPausa", OrdenPausa);
        var audio = raiz.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.ignoreListenerPause = true;
        var menu = raiz.AddComponent<MenuPausa>();

        var panelGo = Nuevo("PanelPausa", raiz.transform);
        Estirar(panelGo);
        Img(panelGo, tema.velo);
        panelGo.AddComponent<CanvasGroup>();
        var panel = panelGo.AddComponent<PanelUI>();

        var caja = Caja(panelGo.transform);
        Texto(caja, "Titulo", "PAUSA", 64, tema.acento, 84);
        Separador(caja);
        var continuar = Boton(caja, "Continuar");
        var opciones = Boton(caja, "Opciones");
        var menuBtn = Boton(caja, "Volver al menu");
        Asignar(panel, "primerSeleccionado", continuar);

        var opcionesGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabOpciones, raiz.transform);
        opcionesGo.name = "PanelOpciones";

        Asignar(menu, "panelPausa", panel);
        Asignar(menu, "panelOpciones", opcionesGo.GetComponent<PanelOpciones>());
        Asignar(menu, "botonContinuar", continuar);
        Asignar(menu, "botonOpciones", opciones);
        Asignar(menu, "botonMenu", menuBtn);

        return Guardar(raiz, RutaPausa);
    }

    static GameObject CrearPrefabMuerte()
    {
        var existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMuerte);
        if (existente != null) return existente;

        var raiz = CrearCanvas("PF_PantallaMuerte", OrdenMuerte);
        raiz.AddComponent<CanvasGroup>();
        var pantalla = raiz.AddComponent<PantallaMuerte>();

        var velo = Nuevo("Velo", raiz.transform);
        Estirar(velo);
        Img(velo, new Color(0f, 0f, 0f, 0.85f)).raycastTarget = false;

        var centro = Nuevo("Centro", raiz.transform);
        var rc = (RectTransform)centro.transform;
        rc.sizeDelta = new Vector2(1400, 200);
        var vlg = centro.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.spacing = 24;
        vlg.childControlWidth = vlg.childControlHeight = true;
        vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;

        var linea = Nuevo("Linea", centro.transform);
        Img(linea, tema.peligro).raycastTarget = false;
        var le = linea.AddComponent<LayoutElement>();
        le.preferredWidth = 520; le.preferredHeight = 2;

        var msg = Texto(centro.transform, "Mensaje", "La culpa te arrastra de nuevo", 54, tema.texto, 80, 1400);
        msg.fontStyle = FontStyles.Italic;
        msg.characterSpacing = 4;

        Asignar(pantalla, "textoMensaje", msg);
        return Guardar(raiz, RutaMuerte);
    }

    static void AgregarAlHUD(GameObject pausa, GameObject muerte)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RutaHUD) == null) { Debug.LogWarning("No se encontró " + RutaHUD); return; }

        var raiz = PrefabUtility.LoadPrefabContents(RutaHUD);
        bool cambio = false;

        if (raiz.GetComponentInChildren<MenuPausa>(true) == null) { Anidar(pausa, raiz.transform, OrdenPausa); cambio = true; }
        if (raiz.GetComponentInChildren<PantallaMuerte>(true) == null) { Anidar(muerte, raiz.transform, OrdenMuerte); cambio = true; }

        foreach (var h in raiz.GetComponentsInChildren<HealthUI>(true)) cambio |= AsignarSiVacio(h, "tema", tema);
        foreach (var e in raiz.GetComponentsInChildren<EnergyUI>(true)) cambio |= AsignarSiVacio(e, "tema", tema);

        if (cambio) PrefabUtility.SaveAsPrefabAsset(raiz, RutaHUD);
        PrefabUtility.UnloadPrefabContents(raiz);
        if (cambio) Debug.Log("PF_HUD actualizado: menú de pausa, pantalla de muerte y tema.");
    }

    static void Anidar(GameObject prefab, Transform padre, int orden)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        Estirar(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.overrideSorting = true; // canvas anidado: necesita esto para respetar su orden
        canvas.sortingOrder = orden;
    }

    // ---------- Escena del menú principal ----------

    static void CrearEscenaMenu(GameObject prefabOpciones)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscenaMenu) != null) { AgregarABuild(); return; }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.transform.position = new Vector3(0, 0, -10);

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

        var canvas = CrearCanvas("Canvas_MenuPrincipal", 10);
        var audio = canvas.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.ignoreListenerPause = true;
        var menu = canvas.AddComponent<MenuPrincipal>();

        // Fondo del círculo 9, oscurecido
        var fondo = Nuevo("Fondo", canvas.transform);
        Estirar(fondo);
        var imgFondo = Img(fondo, new Color(0.55f, 0.6f, 0.7f, 1f));
        imgFondo.raycastTarget = false;
        var sprite = AssetDatabase.LoadAllAssetsAtPath(RutaFondo).OfType<Sprite>().FirstOrDefault();
        if (sprite != null)
        {
            imgFondo.sprite = sprite;
            var arf = fondo.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = sprite.rect.width / sprite.rect.height;
        }
        var velo = Nuevo("Velo", canvas.transform);
        Estirar(velo);
        Img(velo, new Color(tema.fondo.r, tema.fondo.g, tema.fondo.b, 0.55f)).raycastTarget = false;

        // Panel principal: título + botones
        var panelGo = Nuevo("PanelPrincipal", canvas.transform);
        Estirar(panelGo);
        panelGo.AddComponent<CanvasGroup>();
        var panel = panelGo.AddComponent<PanelUI>();

        var titulo = Nuevo("Titulo", panelGo.transform);
        var rt = (RectTransform)titulo.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.68f);
        rt.sizeDelta = new Vector2(1600, 260);
        var vt = titulo.AddComponent<VerticalLayoutGroup>();
        vt.childAlignment = TextAnchor.MiddleCenter;
        vt.childControlWidth = vt.childControlHeight = true;
        vt.childForceExpandWidth = vt.childForceExpandHeight = false;
        vt.spacing = 8;
        var t1 = Texto(titulo.transform, "Nombre", "ÚLTIMO DESCENSO", 104, tema.acento, 130, 1600);
        t1.characterSpacing = 12;
        t1.fontStyle = FontStyles.Bold;
        var t2 = Texto(titulo.transform, "Subtitulo", "THE LAST ASCENT", 30, tema.textoTenue, 44, 1600);
        t2.characterSpacing = 40;

        var botones = Nuevo("Botones", panelGo.transform);
        var rb = (RectTransform)botones.transform;
        rb.anchorMin = rb.anchorMax = new Vector2(0.5f, 0.3f);
        rb.sizeDelta = new Vector2(480, 260);
        var vb = botones.AddComponent<VerticalLayoutGroup>();
        vb.childAlignment = TextAnchor.MiddleCenter;
        vb.childControlWidth = vb.childControlHeight = true;
        vb.childForceExpandWidth = vb.childForceExpandHeight = false;
        vb.spacing = 12;
        var jugar = Boton(botones.transform, "Jugar");
        var opciones = Boton(botones.transform, "Opciones");
        var salir = Boton(botones.transform, "Salir");
        Asignar(panel, "primerSeleccionado", jugar);

        var opcionesGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabOpciones, canvas.transform);
        opcionesGo.name = "PanelOpciones";

        Asignar(menu, "panelPrincipal", panel);
        Asignar(menu, "panelOpciones", opcionesGo.GetComponent<PanelOpciones>());
        Asignar(menu, "botonJugar", jugar);
        Asignar(menu, "botonOpciones", opciones);
        Asignar(menu, "botonSalir", salir);

        EditorSceneManager.SaveScene(escena, RutaEscenaMenu);
        Debug.Log("Creada " + RutaEscenaMenu);
        AgregarABuild();
    }

    static void AgregarABuild()
    {
        var lista = EditorBuildSettings.scenes.ToList();
        if (lista.Any(s => s.path == RutaEscenaMenu)) return;
        lista.Insert(0, new EditorBuildSettingsScene(RutaEscenaMenu, true));
        EditorBuildSettings.scenes = lista.ToArray();
        Debug.Log("SC_MenuPrincipal agregada como primera escena del Build.");
    }

    // ---------- Piezas ----------

    static GameObject CrearCanvas(string nombre, int orden)
    {
        var go = Nuevo(nombre, null);
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = orden;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1920, 1080);
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    // Caja centrada con fondo y borde azul hielo, crece con su contenido
    static Transform Caja(Transform padre)
    {
        var borde = Nuevo("Caja", padre);
        Img(borde, new Color(tema.acento.r, tema.acento.g, tema.acento.b, 0.35f));
        var lb = borde.AddComponent<VerticalLayoutGroup>();
        lb.padding = new RectOffset(2, 2, 2, 2);
        lb.childControlWidth = lb.childControlHeight = true;
        borde.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        borde.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var interior = Nuevo("Interior", borde.transform);
        Img(interior, tema.panel);
        var v = interior.AddComponent<VerticalLayoutGroup>();
        v.padding = new RectOffset(72, 72, 48, 48);
        v.spacing = 14;
        v.childAlignment = TextAnchor.MiddleCenter;
        v.childControlWidth = v.childControlHeight = true;
        v.childForceExpandWidth = v.childForceExpandHeight = false;
        return interior.transform;
    }

    static Transform Fila(Transform padre, string nombre)
    {
        var go = Nuevo(nombre, padre);
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 32;
        h.padding = new RectOffset(40, 0, 0, 0);
        h.childAlignment = TextAnchor.MiddleLeft;
        h.childControlWidth = h.childControlHeight = true;
        h.childForceExpandWidth = h.childForceExpandHeight = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 820; le.preferredHeight = 56;
        return go.transform;
    }

    // Etiqueta de fila de opciones, alineada a la izquierda
    static TextMeshProUGUI Etiqueta(Transform fila, string texto)
    {
        var t = Texto(fila, "Etiqueta", texto, 30, tema.textoTenue, 48, 340);
        t.alignment = TextAlignmentOptions.Left;
        t.characterSpacing = 4;
        return t;
    }

    static void Separador(Transform padre)
    {
        var go = Nuevo("Separador", padre);
        Img(go, new Color(tema.acento.r, tema.acento.g, tema.acento.b, 0.25f)).raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 360; le.preferredHeight = 2;
    }

    // Rombo dorado que indica la opción seleccionada
    static GameObject Marcador(Transform padre)
    {
        var go = Nuevo("Marcador", padre);
        Img(go, tema.resaltado).raycastTarget = false;
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0f, 0.5f);
        r.sizeDelta = new Vector2(12, 12);
        r.anchoredPosition = new Vector2(18, 0);
        r.localEulerAngles = new Vector3(0, 0, 45);
        go.AddComponent<LayoutElement>().ignoreLayout = true;
        go.SetActive(false);
        return go;
    }

    static Button Boton(Transform padre, string texto)
    {
        var go = Nuevo("Boton_" + texto.Replace(" ", ""), padre);
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 440; le.preferredHeight = 64;
        var img = Img(go, new Color(1f, 1f, 1f, 0f)); // invisible, solo recibe el mouse
        var b = go.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.targetGraphic = img;

        var lbl = Texto(go.transform, "Texto", texto.ToUpper(), 36, tema.textoTenue, 64);
        Estirar(lbl.gameObject);
        lbl.characterSpacing = 8;
        Efecto(go, lbl, Marcador(go.transform));
        return b;
    }

    static Slider CrearSlider(Transform padre)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = "Slider";
        go.transform.SetParent(padre, false);
        go.layer = 5;
        var s = go.GetComponent<Slider>();
        s.transition = Selectable.Transition.None;
        s.value = 1f;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 380; le.preferredHeight = 20;
        foreach (var i in go.GetComponentsInChildren<Image>(true))
        {
            if (i.name == "Background") i.color = new Color(1f, 1f, 1f, 0.12f);
            else if (i.name == "Fill") i.color = tema.acento;
            else if (i.name == "Handle") i.color = tema.resaltado;
        }
        return s;
    }

    static Toggle CrearToggle(Transform padre)
    {
        var go = DefaultControls.CreateToggle(new DefaultControls.Resources());
        go.name = "Toggle";
        go.transform.SetParent(padre, false);
        go.layer = 5;
        var label = go.transform.Find("Label");
        if (label != null) Object.DestroyImmediate(label.gameObject);
        var t = go.GetComponent<Toggle>();
        t.transition = Selectable.Transition.None;
        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = 36; le.preferredHeight = 36;
        var fondo = go.transform.Find("Background");
        if (fondo != null)
        {
            var r = (RectTransform)fondo;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            fondo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
            var check = fondo.Find("Checkmark");
            if (check != null)
            {
                var rc = (RectTransform)check;
                rc.anchorMin = Vector2.zero; rc.anchorMax = Vector2.one;
                rc.offsetMin = new Vector2(7, 7); rc.offsetMax = new Vector2(-7, -7);
                check.GetComponent<Image>().color = tema.acento;
            }
        }
        return t;
    }

    static void Efecto(GameObject go, TMP_Text etiqueta, GameObject marcador)
    {
        var fx = go.AddComponent<EfectoBotonUI>();
        Asignar(fx, "tema", tema);
        Asignar(fx, "etiqueta", etiqueta);
        Asignar(fx, "marcador", marcador);
    }

    static TextMeshProUGUI Texto(Transform padre, string nombre, string texto, float tamano, Color color, float alto, float ancho = -1)
    {
        var go = Nuevo(nombre, padre);
        var t = go.AddComponent<TextMeshProUGUI>();
        if (tema.fuente != null) t.font = tema.fuente;
        t.text = texto;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = alto;
        if (ancho > 0) le.preferredWidth = ancho;
        return t;
    }

    static GameObject Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5; // UI
        if (padre != null) go.transform.SetParent(padre, false);
        return go;
    }

    static void Estirar(GameObject go)
    {
        var r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.localScale = Vector3.one;
    }

    static Image Img(GameObject go, Color c)
    {
        var i = go.AddComponent<Image>();
        i.color = c;
        return i;
    }

    static GameObject Guardar(GameObject raiz, string ruta)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(raiz, ruta);
        Object.DestroyImmediate(raiz);
        Debug.Log("Creado " + ruta);
        return prefab;
    }

    static void Asignar(Object obj, string campo, Object valor)
    {
        var so = new SerializedObject(obj);
        var p = so.FindProperty(campo);
        if (p == null) { Debug.LogError($"ConfigurarUI: no existe el campo '{campo}' en {obj.GetType().Name}"); return; }
        p.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static bool AsignarSiVacio(Object obj, string campo, Object valor)
    {
        var so = new SerializedObject(obj);
        var p = so.FindProperty(campo);
        if (p == null || p.objectReferenceValue != null) return false;
        p.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }
}
