// Menú: Tools > Ultimo Descenso > Configurar UI / Reconstruir UI
// Construye la interfaz del documento IEEE (mockups de menú principal, HUD, pausa y diálogo):
//  - Tema (Resources/UI_Tema), fuentes TMP (Cinzel y EB Garamond) e importación del arte temporal
//  - Prefabs PF_PanelOpciones, PF_MenuPausa, PF_PantallaMuerte y escena SC_MenuPrincipal
//  - Estilo de PF_HUD (retrato, vida, energía, almas, minimapa, ranuras) y del prefab de diálogo
// "Configurar" solo crea lo que falta. "Reconstruir" borra lo generado y lo vuelve a crear.
// Las posiciones usan coordenadas de los mockups (1400x787) escaladas a 1920x1080.
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class ConfigurarUI
{
    const string CarpetaArte = "Assets/_Project/Art/UI/Temporal/";
    const string CarpetaFuentes = "Assets/_Project/Art/Fuentes/";
    const string RutaTema = "Assets/_Project/Resources/UI_Tema.asset";
    const string RutaTemaVieja = "Assets/_Project/Settings/UI_Tema.asset";
    const string CarpetaPrefabs = "Assets/_Project/Prefabs/";
    const string RutaOpciones = CarpetaPrefabs + "PF_PanelOpciones.prefab";
    const string RutaPausa = CarpetaPrefabs + "PF_MenuPausa.prefab";
    const string RutaMuerte = CarpetaPrefabs + "PF_PantallaMuerte.prefab";
    const string RutaHUD = CarpetaPrefabs + "PF_HUD.prefab";
    const string RutaIconoVida = CarpetaPrefabs + "PF_HealthIcon.prefab";
    const string RutaDialogo = CarpetaPrefabs + "DialgoueSystem.prefab";
    const string RutaEscenaMenu = "Assets/_Project/Scenes/SC_MenuPrincipal.unity";
    const string RutaAcciones = "Assets/InputSystem_Actions.inputactions";

    const int OrdenMuerte = 45;
    const int OrdenPausa = 50;

    // Mockup 1400x787 -> referencia 1920x1080
    const float K = 1920f / 1400f;
    static readonly Vector2 Centro = new Vector2(0.5f, 0.5f);
    static readonly Vector2 ArribaIzq = new Vector2(0f, 1f);
    static readonly Vector2 ArribaDer = new Vector2(1f, 1f);
    static readonly Vector2 AbajoDer = new Vector2(1f, 0f);
    static readonly Vector2 AbajoCentro = new Vector2(0.5f, 0f);

    static TemaUI tema;

    [MenuItem("Tools/Ultimo Descenso/Configurar UI")]
    public static void Configurar() => Ejecutar(false);

    [MenuItem("Tools/Ultimo Descenso/Reconstruir UI (borra y recrea)")]
    public static void Reconstruir() => Ejecutar(true);

    static void Ejecutar(bool reconstruir)
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Sal del modo Play antes de configurar la UI."); return; }

        if (reconstruir) BorrarGenerado();
        ImportarArte();
        tema = CrearTema();
        var opciones = CrearPrefabOpciones();
        var pausa = CrearPrefabPausa(opciones);
        var muerte = CrearPrefabMuerte();
        EstilizarHUD(pausa, muerte);
        EstilizarDialogo();
        CrearEscenaMenu(opciones);
        AssetDatabase.SaveAssets();
        Debug.Log("UI configurada con el diseño del documento.");
    }

    static void BorrarGenerado()
    {
        // Quitar del HUD las instancias anidadas antes de borrar sus prefabs
        if (File.Exists(RutaHUD))
        {
            var raiz = PrefabUtility.LoadPrefabContents(RutaHUD);
            foreach (var c in raiz.GetComponentsInChildren<MenuPausa>(true)) Object.DestroyImmediate(c.gameObject);
            foreach (var c in raiz.GetComponentsInChildren<PantallaMuerte>(true)) Object.DestroyImmediate(c.gameObject);
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaHUD);
            PrefabUtility.UnloadPrefabContents(raiz);
        }
        foreach (var r in new[] { RutaPausa, RutaMuerte, RutaOpciones, RutaEscenaMenu, RutaTema, RutaTemaVieja })
            if (File.Exists(r)) AssetDatabase.DeleteAsset(r);
    }

    // ---------- Arte, fuentes y tema ----------

    static void ImportarArte()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CarpetaArte.TrimEnd('/') }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            if (imp.textureType == TextureImporterType.Sprite && imp.spriteImportMode == SpriteImportMode.Single && !imp.mipmapEnabled) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.maxTextureSize = 2048;
            imp.SaveAndReimport();
        }
    }

    static Sprite Arte(string nombre) => AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaArte + nombre + ".png");

    static TMP_FontAsset Fuente(string nombre, out Material sombra)
    {
        string ruta = CarpetaFuentes + nombre + " SDF.asset";
        string rutaMat = CarpetaFuentes + nombre + " SDF Sombra.mat";
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ruta);
        if (fa == null)
        {
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(CarpetaFuentes + nombre + ".ttf");
            fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            fa.name = nombre + " SDF";
            AssetDatabase.CreateAsset(fa, ruta);
            fa.material.name = nombre + " SDF Material";
            AssetDatabase.AddObjectToAsset(fa.material, fa);
            fa.atlasTextures[0].name = nombre + " SDF Atlas";
            AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            fa.TryAddCharacters("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzÁÉÍÓÚÜÑáéíóúüñ0123456789.,;:¿?¡!'\"()-–—…«» ");
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
        }
        sombra = AssetDatabase.LoadAssetAtPath<Material>(rutaMat);
        if (sombra == null)
        {
            // Sombra suave debajo del texto para que se lea sobre el fondo pintado
            sombra = new Material(fa.material) { name = nombre + " SDF Sombra" };
            sombra.EnableKeyword("UNDERLAY_ON");
            sombra.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.85f));
            sombra.SetFloat("_UnderlaySoftness", 0.6f);
            sombra.SetFloat("_UnderlayDilate", 0.3f);
            sombra.SetFloat("_UnderlayOffsetY", -0.4f);
            AssetDatabase.CreateAsset(sombra, rutaMat);
        }
        return fa;
    }

    static TemaUI CrearTema()
    {
        var t = AssetDatabase.LoadAssetAtPath<TemaUI>(RutaTema);
        if (t == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(RutaTema));
            t = ScriptableObject.CreateInstance<TemaUI>();
            AssetDatabase.CreateAsset(t, RutaTema);
        }
        if (t.fuenteTitulo == null) t.fuenteTitulo = Fuente("Cinzel", out t.materialTitulo);
        if (t.fuenteTexto == null) t.fuenteTexto = Fuente("EBGaramond", out t.materialTexto);
        if (t.marcador == null) t.marcador = Arte("llama");
        if (t.separador == null) t.separador = Arte("separador");
        if (t.marcoBarraJefe == null) t.marcoBarraJefe = Arte("marco_barra_jefe");
        if (t.placaJefe == null) t.placaJefe = Arte("placa_jefe");
        EditorUtility.SetDirty(t);
        return t;
    }

    // ---------- Opciones ----------

    static GameObject CrearPrefabOpciones()
    {
        var existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaOpciones);
        if (existente != null) return existente;

        var raiz = Nuevo("PF_PanelOpciones", null);
        Estirar(raiz);
        Img(raiz, tema.velo);
        raiz.AddComponent<CanvasGroup>();
        var panel = raiz.AddComponent<PanelOpciones>();

        // Mismo marco que la pausa (mockup: x500 y25 400x740)
        ImgArte(raiz.transform, "Marco", "marco_panel", 500, 25, 400, 740, Centro);
        Texto(raiz.transform, "Titulo", "OPCIONES", 52, tema.texto, true, 560, 175, 280, 70, Centro);

        var lblVol = Texto(raiz.transform, "Etiqueta_Volumen", "Volumen", 40, tema.textoNormal, false, 560, 290, 280, 44, Centro);
        var slider = CrearSlider(raiz.transform, 585, 345, 230, 18);
        Efecto(slider.gameObject, lblVol, Marcador(lblVol.transform), true);

        var lblPc = Texto(raiz.transform, "Etiqueta_PantallaCompleta", "Pantalla completa", 40, tema.textoNormal, false, 560, 405, 280, 44, Centro);
        var toggle = CrearToggle(raiz.transform, 685, 458, 30, 30);
        Efecto(toggle.gameObject, lblPc, Marcador(lblPc.transform), true);

        var volver = Boton(raiz.transform, "Volver", 560, 545, 280, 50, Centro);

        Asignar(panel, "sliderVolumen", slider);
        Asignar(panel, "togglePantallaCompleta", toggle);
        Asignar(panel, "botonVolver", volver);
        Asignar(panel, "primerSeleccionado", slider);
        return Guardar(raiz, RutaOpciones);
    }

    // ---------- Pausa ----------

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
        var p = panelGo.transform;

        // Contexto arriba a la izquierda: círculo actual y almas
        var circulo = Texto(p, "Circulo", "Círculo IX", 32, tema.texto, false, 45, 36, 220, 34, ArribaIzq);
        circulo.alignment = TextAlignmentOptions.Left;
        var linea = ImgArte(p, "LineaCirculo", "linea_circulo", 36, 66, 200, 24, ArribaIzq);
        var nombreCirc = panelGo.AddComponent<NombreCirculoUI>();
        Asignar(nombreCirc, "texto", circulo);
        Asignar(nombreCirc, "ocultarSiVacio", linea.gameObject);
        ImgArte(p, "MedallonAlmas", "medallon_almas", 38, 116, 60, 68, ArribaIzq);
        var almas = Texto(p, "Almas", "0", 34, tema.texto, false, 106, 122, 160, 36, ArribaIzq);
        almas.alignment = TextAlignmentOptions.Left;
        var etiquetaAlmas = Texto(p, "EtiquetaAlmas", "ALMAS", 17, tema.textoNormal, true, 106, 156, 160, 22, ArribaIzq);
        etiquetaAlmas.alignment = TextAlignmentOptions.Left;
        var contador = panelGo.AddComponent<ContadorAlmasUI>();
        Asignar(contador, "texto", almas);

        // Panel central (mockup: marco x500 y25 400x740)
        ImgArte(p, "Marco", "marco_panel", 500, 25, 400, 740, Centro);
        Texto(p, "Titulo", "PAUSA", 66, tema.texto, true, 580, 175, 240, 75, Centro);
        var continuar = Boton(p, "Continuar", 550, 302, 300, 46, Centro);
        var mapa = Boton(p, "Mapa", 550, 362, 300, 46, Centro);
        var habilidades = Boton(p, "Habilidades", 550, 422, 300, 46, Centro);
        var opciones = Boton(p, "Opciones", 550, 482, 300, 46, Centro);
        var salir = Boton(p, "Salir al menú", 550, 544, 300, 46, Centro);
        Asignar(panel, "primerSeleccionado", continuar);

        var opcionesGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabOpciones, raiz.transform);
        opcionesGo.name = "PanelOpciones";

        Asignar(menu, "panelPausa", panel);
        Asignar(menu, "panelOpciones", opcionesGo.GetComponent<PanelOpciones>());
        Asignar(menu, "botonContinuar", continuar);
        Asignar(menu, "botonMapa", mapa);
        Asignar(menu, "botonHabilidades", habilidades);
        Asignar(menu, "botonOpciones", opciones);
        Asignar(menu, "botonSalirMenu", salir);
        return Guardar(raiz, RutaPausa);
    }

    // ---------- Muerte ----------

    static GameObject CrearPrefabMuerte()
    {
        var existente = AssetDatabase.LoadAssetAtPath<GameObject>(RutaMuerte);
        if (existente != null) return existente;

        var raiz = CrearCanvas("PF_PantallaMuerte", OrdenMuerte);
        raiz.AddComponent<CanvasGroup>();
        var pantalla = raiz.AddComponent<PantallaMuerte>();

        var velo = Nuevo("Velo", raiz.transform);
        Estirar(velo);
        Img(velo, new Color(0.03f, 0.01f, 0.01f, 0.9f)).raycastTarget = false;

        var sep = ImgArte(raiz.transform, "Separador", "separador", 320, 400, 760, 60, Centro);
        sep.color = new Color(1f, 1f, 1f, 0.8f);
        var msg = Texto(raiz.transform, "Mensaje", "La culpa te arrastra de nuevo", 58, tema.texto, false, 250, 320, 900, 80, Centro);
        msg.fontStyle = FontStyles.Italic;

        Asignar(pantalla, "textoMensaje", msg);
        return Guardar(raiz, RutaMuerte);
    }

    // ---------- HUD ----------

    static void EstilizarHUD(GameObject pausa, GameObject muerte)
    {
        if (!File.Exists(RutaHUD)) { Debug.LogWarning("No se encontró " + RutaHUD); return; }

        // Ícono de vida: segmento rojo del mockup
        if (File.Exists(RutaIconoVida))
        {
            var icono = PrefabUtility.LoadPrefabContents(RutaIconoVida);
            ((RectTransform)icono.transform).sizeDelta = new Vector2(56 * K, 40 * K);
            var img = icono.GetComponent<Image>();
            img.sprite = Arte("vida_llena");
            img.color = Color.white;
            img.raycastTarget = false;
            PrefabUtility.SaveAsPrefabAsset(icono, RutaIconoVida);
            PrefabUtility.UnloadPrefabContents(icono);
        }

        var raiz = PrefabUtility.LoadPrefabContents(RutaHUD);
        var r = raiz.transform;

        // Lo generado antes se reemplaza
        foreach (var n in new[] { "Retrato", "Almas", "Minimapa", "Ranuras" })
        {
            var viejo = r.Find(n);
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        }

        // Retrato (mockup: x15 y15 150x150)
        var retrato = ImgArte(r, "Retrato", "retrato_jugador", 15, 15, 150, 150, ArribaIzq);
        retrato.transform.SetAsFirstSibling();

        // Vida en segmentos (mockup: desde x169 y51, segmentos de 56x40)
        var vida = raiz.GetComponentInChildren<HealthUI>(true);
        if (vida != null)
        {
            var rv = (RectTransform)vida.transform;
            rv.anchorMin = rv.anchorMax = ArribaIzq;
            rv.pivot = new Vector2(0f, 0.5f);
            rv.anchoredPosition = new Vector2(169 * K, -(51 + 20) * K);
            rv.sizeDelta = new Vector2(6 * 56 * K, 40 * K);
            var hl = vida.GetComponent<HorizontalLayoutGroup>();
            if (hl != null)
            {
                hl.spacing = -2 * K;
                hl.childAlignment = TextAnchor.MiddleLeft;
                hl.childControlWidth = hl.childControlHeight = false;
                hl.childForceExpandWidth = hl.childForceExpandHeight = false;
            }
            var bg = vida.GetComponent<Image>();
            if (bg != null) bg.color = new Color(0, 0, 0, 0);
            AsignarValor(vida, "fullSprite", Arte("vida_llena"));
            AsignarValor(vida, "emptySprite", Arte("vida_vacia"));
            AsignarColor(vida, "fullColor", Color.white);
            AsignarColor(vida, "emptyColor", Color.white);
        }

        // Energía violeta (mockup: marco x158 y98 270x34; relleno con márgenes del marco)
        var energia = raiz.GetComponentInChildren<EnergyUI>(true);
        if (energia != null)
        {
            var re = (RectTransform)energia.transform;
            Colocar(re, 158, 98, 270, 34, ArribaIzq);
            var marco = energia.GetComponent<Image>();
            if (marco == null) marco = energia.gameObject.AddComponent<Image>();
            marco.sprite = Arte("marco_energia");
            marco.color = Color.white;
            marco.raycastTarget = false;

            var so = new SerializedObject(energia);
            var relleno = so.FindProperty("fillRect").objectReferenceValue as RectTransform;
            if (relleno != null)
            {
                var interior = re.Find("Interior");
                if (interior == null) interior = Nuevo("Interior", re).transform;
                var ri = (RectTransform)interior;
                ri.anchorMin = Vector2.zero; ri.anchorMax = Vector2.one;
                ri.offsetMin = new Vector2(21 * K, 11 * K);
                ri.offsetMax = new Vector2(-24 * K, -11 * K);
                relleno.SetParent(interior, false);
                var img = relleno.GetComponent<Image>();
                if (img != null) img.color = tema.energia;
            }
        }

        // Almas (mockup: icono x45 y168 50x62, número a la derecha)
        var almasGo = Nuevo("Almas", r);
        Colocar((RectTransform)almasGo.transform, 40, 165, 200, 68, ArribaIzq);
        ImgArte(almasGo.transform, "Icono", "icono_almas", 5, 3, 50, 62, ArribaIzq, true);
        var numAlmas = TextoEn(almasGo.transform, "Cantidad", "0", 40, tema.texto, false, 64, 14, 136, 40);
        numAlmas.alignment = TextAlignmentOptions.Left;
        Asignar(almasGo.AddComponent<ContadorAlmasUI>(), "texto", numAlmas);

        // Minimapa (placeholder) con el nombre del círculo (mockup: x1145 y8 252x255)
        var mini = ImgArte(r, "Minimapa", "minimapa", 1118, 8, 252, 255, ArribaDer);
        var nombre = TextoEn(mini.transform, "Circulo", "Círculo IX", 30, tema.texto, false, 40, 200, 172, 36);
        Asignar(mini.gameObject.AddComponent<NombreCirculoUI>(), "texto", nombre);

        // Ranuras de acción (mockup: x1045/1159/1273 y637, 98x98, tecla debajo)
        var ranuras = Nuevo("Ranuras", r);
        Estirar(ranuras);
        var acciones = AssetDatabase.LoadAllAssetsAtPath(RutaAcciones).OfType<InputActionReference>().ToArray();
        string[] arte = { "ranura_ataque", "ranura_movimiento", "ranura_especial" };
        string[] accion = { "Attack", "Dash", "Parry" };
        for (int i = 0; i < 3; i++)
        {
            float x = 1045 + i * 114;
            var ranura = ImgArte(ranuras.transform, "Ranura_" + accion[i], arte[i], x, 625, 98, 98, AbajoDer);
            var tecla = Nuevo("Tecla", ranura.transform);
            ColocarEn((RectTransform)tecla.transform, 30, 92, 38, 30);
            Img(tecla, new Color(0.06f, 0.05f, 0.045f, 0.95f)).raycastTarget = false;
            var borde = tecla.AddComponent<Outline>();
            borde.effectColor = new Color(tema.textoNormal.r, tema.textoNormal.g, tema.textoNormal.b, 0.5f);
            borde.effectDistance = new Vector2(1.5f, -1.5f);
            var txt = TextoEn(tecla.transform, "Texto", "?", 24, tema.texto, false, 0, 0, 38, 30);
            txt.enableAutoSizing = true; txt.fontSizeMin = 12; txt.fontSizeMax = 26;
            var ui = ranura.gameObject.AddComponent<RanuraAccionUI>();
            Asignar(ui, "textoTecla", txt);
            var refAccion = acciones.FirstOrDefault(a => a.action != null && a.action.name == accion[i] && a.action.actionMap.name == "Player");
            if (refAccion != null) Asignar(ui, "accion", refAccion);
        }

        // Pausa y muerte anidadas
        if (raiz.GetComponentInChildren<MenuPausa>(true) == null) Anidar(pausa, r, OrdenPausa);
        if (raiz.GetComponentInChildren<PantallaMuerte>(true) == null) Anidar(muerte, r, OrdenMuerte);

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaHUD);
        PrefabUtility.UnloadPrefabContents(raiz);
        Debug.Log("PF_HUD actualizado con el diseño del documento.");
    }

    static void Anidar(GameObject prefab, Transform padre, int orden)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        Estirar(go);
        var canvas = go.GetComponent<Canvas>();
        canvas.overrideSorting = true; // canvas anidado: necesita esto para respetar su orden
        canvas.sortingOrder = orden;
    }

    // ---------- Diálogo ----------

    static void EstilizarDialogo()
    {
        if (!File.Exists(RutaDialogo)) return;
        var raiz = PrefabUtility.LoadPrefabContents(RutaDialogo);
        var manager = raiz.GetComponentInChildren<DialogueManager>(true);
        var caja = Buscar(raiz.transform, "Caja");
        if (manager == null || caja == null) { PrefabUtility.UnloadPrefabContents(raiz); return; }

        var scaler = raiz.GetComponentInChildren<CanvasScaler>(true);
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // Caja (mockup: x95 y490 1150x270)
        var imgCaja = caja.GetComponent<Image>();
        imgCaja.sprite = Arte("marco_dialogo");
        imgCaja.color = Color.white;
        imgCaja.type = Image.Type.Simple;
        Colocar((RectTransform)caja, 95, 490, 1150, 270, AbajoCentro);

        var nombre = Buscar(caja, "TextNombre")?.GetComponent<TMP_Text>();
        if (nombre != null)
        {
            ColocarEn(nombre.rectTransform, 278, 24, 267, 39);
            EstiloTexto(nombre, 36, tema.texto, false);
            nombre.alignment = TextAlignmentOptions.Center;
        }
        var cuerpo = Buscar(caja, "TextDialogue")?.GetComponent<TMP_Text>();
        if (cuerpo != null)
        {
            ColocarEn(cuerpo.rectTransform, 250, 100, 830, 140);
            EstiloTexto(cuerpo, 38, tema.texto, false);
            cuerpo.alignment = TextAlignmentOptions.MidlineLeft;
            cuerpo.textWrappingMode = TextWrappingModes.Normal;
        }

        var retratoT = Buscar(caja, "Retrato");
        var retrato = retratoT != null ? retratoT.gameObject : Nuevo("Retrato", caja);
        ColocarEn((RectTransform)retrato.transform, 27, 59, 163, 186);
        var imgRet = retrato.GetComponent<Image>();
        if (imgRet == null) imgRet = retrato.AddComponent<Image>();
        imgRet.preserveAspect = true;
        imgRet.raycastTarget = false;
        imgRet.sprite = Arte("retrato_virgilio");
        imgRet.enabled = false; // lo activa DialogueManager si la línea tiene retrato
        retrato.transform.SetSiblingIndex(0);
        Asignar(manager, "retrato", imgRet);

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaDialogo);
        PrefabUtility.UnloadPrefabContents(raiz);
        Debug.Log("Prefab de diálogo actualizado con el diseño del documento.");
    }

    static Transform Buscar(Transform t, string nombre) =>
        t.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == nombre);

    // ---------- Menú principal ----------

    static void CrearEscenaMenu(GameObject prefabOpciones)
    {
        if (File.Exists(RutaEscenaMenu)) { AgregarABuild(); return; }
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

        // Fondo: el pozo circular iluminado en lo alto
        var fondo = Nuevo("Fondo", canvas.transform);
        Estirar(fondo);
        var imgFondo = Img(fondo, Color.white);
        imgFondo.raycastTarget = false;
        imgFondo.sprite = Arte("fondo_menu");
        if (imgFondo.sprite != null)
        {
            var arf = fondo.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = imgFondo.sprite.rect.width / imgFondo.sprite.rect.height;
        }

        var panelGo = Nuevo("PanelPrincipal", canvas.transform);
        Estirar(panelGo);
        panelGo.AddComponent<CanvasGroup>();
        var panel = panelGo.AddComponent<PanelUI>();
        var p = panelGo.transform;

        // Título (mockup: "THE" y "LAST ASCENT" con iniciales grandes) y separador ornamental
        Texto(p, "Titulo_The", "THE", 74, tema.texto, true, 600, 92, 200, 75, Centro);
        var titulo = Texto(p, "Titulo", "<size=125%>L</size>AST <size=125%>A</size>SCENT", 190, tema.texto, true, 240, 160, 920, 160, Centro);
        titulo.enableAutoSizing = true; titulo.fontSizeMin = 60; titulo.fontSizeMax = 190;
        titulo.characterSpacing = -2;
        ImgArte(p, "Separador", "separador", 320, 318, 760, 60, Centro);

        var nueva = Boton(p, "Nueva partida", 500, 403, 400, 46, Centro, true);
        var continuar = Boton(p, "Continuar", 500, 465, 400, 46, Centro, true);
        var opciones = Boton(p, "Opciones", 500, 527, 400, 46, Centro, true);
        var salir = Boton(p, "Salir", 500, 589, 400, 46, Centro, true);
        Asignar(panel, "primerSeleccionado", nueva);

        var opcionesGo = (GameObject)PrefabUtility.InstantiatePrefab(prefabOpciones, canvas.transform);
        opcionesGo.name = "PanelOpciones";

        Asignar(menu, "panelPrincipal", panel);
        Asignar(menu, "panelOpciones", opcionesGo.GetComponent<PanelOpciones>());
        Asignar(menu, "botonNuevaPartida", nueva);
        Asignar(menu, "botonContinuar", continuar);
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

    // Opción de menú: texto serif centrado + llama a la izquierda al seleccionarse
    static Button Boton(Transform padre, string texto, float x, float y, float w, float h, Vector2 ancla, bool sombra = false)
    {
        var go = Nuevo("Boton_" + texto.Replace(" ", "").Replace("ú", "u"), padre);
        Colocar((RectTransform)go.transform, x, y, w, h, ancla);
        var img = Img(go, new Color(1f, 1f, 1f, 0f)); // invisible, solo recibe el mouse
        var b = go.AddComponent<Button>();
        b.transition = Selectable.Transition.None;
        b.targetGraphic = img;

        var lbl = TextoEn(go.transform, "Texto", texto, 42, tema.textoNormal, false, 0, 0, w, h, sombra);
        Estirar(lbl.gameObject);
        Efecto(go, lbl, Marcador(go.transform), false);
        return b;
    }

    static RectTransform Marcador(Transform padre)
    {
        var go = Nuevo("Llama", padre);
        var img = Img(go, Color.white);
        img.sprite = tema.marcador;
        img.preserveAspect = true;
        img.raycastTarget = false;
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(30 * K, 34 * K);
        go.SetActive(false);
        return r;
    }

    static void Efecto(GameObject go, TMP_Text etiqueta, RectTransform marcador, bool fila)
    {
        var fx = go.AddComponent<EfectoBotonUI>();
        Asignar(fx, "etiqueta", etiqueta);
        Asignar(fx, "marcador", marcador);
        AsignarBool(fx, "marcadorJuntoAlTexto", true);
        if (fila) // la llama va junto a la etiqueta de la fila, no junto al slider
            marcador.SetParent(etiqueta.transform, false);
    }

    static Slider CrearSlider(Transform padre, float x, float y, float w, float h)
    {
        var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
        go.name = "Slider_Volumen";
        go.transform.SetParent(padre, false);
        go.layer = 5;
        Colocar((RectTransform)go.transform, x, y, w, h, Centro);
        var s = go.GetComponent<Slider>();
        s.transition = Selectable.Transition.None;
        s.value = 1f;
        foreach (var i in go.GetComponentsInChildren<Image>(true))
        {
            if (i.name == "Background") i.color = new Color(0.12f, 0.1f, 0.09f, 1f);
            else if (i.name == "Fill") i.color = tema.brasa;
            else if (i.name == "Handle") i.color = tema.texto;
        }
        return s;
    }

    static Toggle CrearToggle(Transform padre, float x, float y, float w, float h)
    {
        var go = DefaultControls.CreateToggle(new DefaultControls.Resources());
        go.name = "Toggle_PantallaCompleta";
        go.transform.SetParent(padre, false);
        go.layer = 5;
        Colocar((RectTransform)go.transform, x, y, w, h, Centro);
        var label = go.transform.Find("Label");
        if (label != null) Object.DestroyImmediate(label.gameObject);
        var t = go.GetComponent<Toggle>();
        t.transition = Selectable.Transition.None;
        var fondo = go.transform.Find("Background");
        if (fondo != null)
        {
            var r = (RectTransform)fondo;
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            fondo.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.09f, 1f);
            fondo.gameObject.AddComponent<Outline>().effectColor = new Color(tema.textoNormal.r, tema.textoNormal.g, tema.textoNormal.b, 0.6f);
            var check = fondo.Find("Checkmark");
            if (check != null)
            {
                var rc = (RectTransform)check;
                rc.anchorMin = Vector2.zero; rc.anchorMax = Vector2.one;
                rc.offsetMin = new Vector2(8, 8); rc.offsetMax = new Vector2(-8, -8);
                check.GetComponent<Image>().color = tema.brasa;
            }
        }
        return t;
    }

    // Texto posicionado con coordenadas del mockup
    static TextMeshProUGUI Texto(Transform padre, string nombre, string texto, float tamano, Color color, bool titulo,
        float x, float y, float w, float h, Vector2 ancla, bool sombra = true)
    {
        var t = CrearTexto(padre, nombre, texto, tamano, color, titulo, sombra);
        Colocar(t.rectTransform, x, y, w, h, ancla);
        return t;
    }

    // Texto posicionado dentro de su padre (coordenadas del mockup relativas a la esquina del padre)
    static TextMeshProUGUI TextoEn(Transform padre, string nombre, string texto, float tamano, Color color, bool titulo,
        float x, float y, float w, float h, bool sombra = true)
    {
        var t = CrearTexto(padre, nombre, texto, tamano, color, titulo, sombra);
        ColocarEn(t.rectTransform, x, y, w, h);
        return t;
    }

    static TextMeshProUGUI CrearTexto(Transform padre, string nombre, string texto, float tamano, Color color, bool titulo, bool sombra)
    {
        var go = Nuevo(nombre, padre);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = texto;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        EstiloTexto(t, tamano, color, titulo, sombra);
        return t;
    }

    static void EstiloTexto(TMP_Text t, float tamano, Color color, bool titulo, bool sombra = true)
    {
        t.font = titulo ? tema.fuenteTitulo : tema.fuenteTexto;
        var mat = titulo ? tema.materialTitulo : tema.materialTexto;
        if (sombra && mat != null) t.fontSharedMaterial = mat;
        t.fontSize = tamano;
        t.color = color;
    }

    static Image ImgArte(Transform padre, string nombre, string arte, float x, float y, float w, float h, Vector2 ancla, bool relativo = false)
    {
        var go = Nuevo(nombre, padre);
        var img = Img(go, Color.white);
        img.sprite = Arte(arte);
        img.raycastTarget = false;
        if (relativo) ColocarEn((RectTransform)go.transform, x, y, w, h);
        else Colocar((RectTransform)go.transform, x, y, w, h, ancla);
        return img;
    }

    // Coordenadas del mockup (esquina superior izquierda en 1400x787) -> RectTransform anclado
    static void Colocar(RectTransform r, float x, float y, float w, float h, Vector2 ancla)
    {
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = new Vector2(0.5f, 0.5f);
        float cx = (x + w * 0.5f) * K, cy = (787f - (y + h * 0.5f)) * K;
        r.anchoredPosition = new Vector2(cx - ancla.x * 1920f, cy - ancla.y * 1080f);
        r.sizeDelta = new Vector2(w * K, h * K);
        r.localScale = Vector3.one;
    }

    // Coordenadas del mockup relativas a la esquina superior izquierda del padre
    static void ColocarEn(RectTransform r, float x, float y, float w, float h)
    {
        r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2((x + w * 0.5f) * K, -(y + h * 0.5f) * K);
        r.sizeDelta = new Vector2(w * K, h * K);
        r.localScale = Vector3.one;
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

    static void Asignar(Object obj, string campo, Object valor) => AsignarValor(obj, campo, valor);

    static void AsignarValor(Object obj, string campo, Object valor)
    {
        var so = new SerializedObject(obj);
        var p = so.FindProperty(campo);
        if (p == null) { Debug.LogError($"ConfigurarUI: no existe el campo '{campo}' en {obj.GetType().Name}"); return; }
        p.objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AsignarColor(Object obj, string campo, Color valor)
    {
        var so = new SerializedObject(obj);
        var p = so.FindProperty(campo);
        if (p == null) { Debug.LogError($"ConfigurarUI: no existe el campo '{campo}' en {obj.GetType().Name}"); return; }
        p.colorValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AsignarBool(Object obj, string campo, bool valor)
    {
        var so = new SerializedObject(obj);
        var p = so.FindProperty(campo);
        if (p == null) { Debug.LogError($"ConfigurarUI: no existe el campo '{campo}' en {obj.GetType().Name}"); return; }
        p.boolValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
