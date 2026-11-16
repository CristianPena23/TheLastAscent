using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Menú de ranuras de guardado. Lo usan la pausa (modo Guardar) y el menú principal (modo Cargar).
// Se ve igual que el menú de pausa: velo oscuro, marco de hueso con la máscara, título en Cinzel
// y opciones con la llama de selección. Se construye por código, no hace falta armarlo en ningún prefab:
//   - En la pausa copia el propio panel de pausa (marco, título, círculo y almas arriba a la izquierda)
//     y reemplaza sus botones por las ranuras.
//   - En el menú principal arma el mismo marco con el arte del TemaUI.
// Los botones se copian de un botón existente del menú, así heredan fuente, llama y sonidos.
//   panel = PanelRanuras.Crear(transform, botonOpciones, PanelRanuras.Modo.Guardar, panelPausa);
//   panel.AbrirDesde(panelPausa);   // Volver() regresa a panelPausa
public class PanelRanuras : PanelUI
{
    public enum Modo { Guardar, Cargar }

    // Modo Cargar: se eligió una ranura y empezó la carga (el menú principal bloquea sus botones)
    public event Action AlCargarPartida;

    // Medidas del marco de la pausa (referencia de los prefabs) y de su contenido
    private static readonly Vector2 TamanoMarco = new Vector2(548.57f, 1014.86f);
    private static readonly Vector2 PosicionMarco = new Vector2(0f, -2.4f);
    private static readonly Vector2 PosicionTitulo = new Vector2(0f, 247.9f);
    private const float AnchoContenido = 470f;
    private const float AltoRanura = 112f;
    private const float PrimeraRanura = 100f;
    private const float SeparacionRanuras = 124f;
    private const float AlturaMensaje = -226f;
    private const float AlturaVolver = -286f;

    private Modo modo;
    private PanelUI volverA;
    private Button[] ranuras;
    private TMP_Text[] textos;
    private Button botonVolver;
    private TMP_Text mensaje;
    private int confirmando = -1; // ranura ocupada esperando la segunda pulsación para sobrescribir
    private bool cargando;

    // plantillaPanel: panel a imitar (el de pausa). Vacío = se arma el marco con el arte del TemaUI
    public static PanelRanuras Crear(Transform padre, Button plantillaBoton, Modo modo, PanelUI plantillaPanel = null)
    {
        var tema = TemaUI.Predeterminado;
        string nombre = modo == Modo.Guardar ? "PanelGuardar" : "PanelCargar";
        string titulo = modo == Modo.Guardar ? "GUARDAR" : "CARGAR";

        GameObject go = plantillaPanel != null
            ? CopiarPanel(plantillaPanel, padre, titulo)
            : ArmarPanel(padre, titulo, tema);
        go.name = nombre;
        go.transform.SetAsLastSibling();

        if (go.GetComponent<CanvasGroup>() == null) go.AddComponent<CanvasGroup>();
        var panel = go.AddComponent<PanelRanuras>(); // queda cerrado hasta AbrirDesde()
        panel.modo = modo;
        panel.Construir(plantillaBoton, tema);
        if (go.activeSelf) panel.Cerrar(true);
        return panel;
    }

    // Abre las ranuras ocultando el panel de origen; Volver() regresa a él
    public void AbrirDesde(PanelUI origen)
    {
        volverA = origen;
        confirmando = -1;
        cargando = false;
        if (mensaje != null) mensaje.text = "";
        Refrescar(); // antes de abrir: así las ranuras vacías ya se ven apagadas
        if (origen != null) origen.Cerrar(true);
        Abrir();
        Seleccionar(PrimeraDisponible());
    }

    public void Volver()
    {
        if (cargando) return;
        SonidosUI.Volver();
        Cerrar(true);
        if (volverA != null) volverA.Abrir();
        volverA = null;
    }

    // ---------- Lógica ----------

    private void AlElegir(int i)
    {
        if (cargando) return;

        if (modo == Modo.Cargar)
        {
            if (!SistemaGuardado.Cargar(i))
            {
                Mensaje("No se pudo cargar esta partida");
                Refrescar();
                return;
            }
            cargando = true;
            foreach (var b in ranuras) b.interactable = false;
            botonVolver.interactable = false;
            AlCargarPartida?.Invoke();
            return;
        }

        // Guardar: si la ranura ya tiene partida, la primera pulsación pide confirmación
        if (SistemaGuardado.Existe(i) && confirmando != i)
        {
            confirmando = i;
            Refrescar();
            Mensaje("Pulsa otra vez para sobrescribir");
            return;
        }

        confirmando = -1;
        bool ok = SistemaGuardado.Guardar(i);
        Refrescar();
        Mensaje(ok ? $"Partida guardada en la ranura {i + 1}" : "No se pudo guardar la partida");
    }

    private void Refrescar()
    {
        for (int i = 0; i < ranuras.Length; i++)
        {
            bool existe = SistemaGuardado.Existe(i);
            var datos = existe ? SistemaGuardado.Leer(i) : null;

            string linea1, linea2 = "";
            if (confirmando == i) linea1 = "¿Sobrescribir?";
            else if (datos != null) Detalle(datos, out linea1, out linea2);
            else if (existe) linea1 = "Datos dañados";
            else linea1 = "Vacía";

            string texto = $"Ranura {i + 1}\n<size=58%><alpha=#C8>{linea1}";
            if (!string.IsNullOrEmpty(linea2)) texto += $"\n<size=50%><alpha=#99>{linea2}";
            textos[i].text = texto;

            // Al cargar solo se pueden elegir ranuras con partida
            ranuras[i].interactable = modo == Modo.Guardar || datos != null;
        }
    }

    private Selectable PrimeraDisponible()
    {
        foreach (var b in ranuras) if (b.interactable) return b;
        return botonVolver;
    }

    private void Mensaje(string texto)
    {
        if (mensaje != null) mensaje.text = texto;
    }

    // "Círculo IX  ·  120 almas"  /  "1:05:12  ·  15/11/2026 18:40"
    private static void Detalle(DatosPartida d, out string linea1, out string linea2)
    {
        string lugar = string.IsNullOrEmpty(d.circulo) ? d.escena : d.circulo;
        linea1 = $"{lugar}  ·  {d.almas.ToString("#,0", CultureInfo.InvariantCulture)} almas";

        var t = TimeSpan.FromSeconds(Mathf.Max(0f, d.tiempoJugado));
        string tiempo = $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
        linea2 = DateTime.TryParse(d.fecha, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var f)
            ? $"{tiempo}  ·  {f.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}"
            : tiempo;
    }

    // ---------- Construcción ----------

    // Copia del panel de pausa sin sus botones ni su PanelUI (se reemplaza por PanelRanuras)
    private static GameObject CopiarPanel(PanelUI plantilla, Transform padre, string titulo)
    {
        var go = Instantiate(plantilla.gameObject, padre, false);
        foreach (var viejo in go.GetComponents<PanelUI>()) DestroyImmediate(viejo);
        foreach (var b in go.GetComponentsInChildren<Button>(true)) DestroyImmediate(b.gameObject);

        var t = go.transform.Find("Titulo");
        var texto = t != null ? t.GetComponent<TMP_Text>() : null;
        if (texto != null) PonerTitulo(texto, titulo);
        return go;
    }

    // Mismo aspecto que la pausa, para escenas sin panel de pausa (menú principal)
    private static GameObject ArmarPanel(Transform padre, string titulo, TemaUI tema)
    {
        var go = new GameObject("Panel", typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(padre, false);
        Estirar((RectTransform)go.transform);
        go.AddComponent<CanvasGroup>();

        // Velo que oscurece lo de atrás y bloquea los clics
        var velo = go.AddComponent<Image>();
        velo.color = tema != null ? tema.velo : new Color(0f, 0f, 0f, 0.72f);

        var marco = Nuevo("Marco", go.transform);
        marco.anchoredPosition = PosicionMarco;
        marco.sizeDelta = TamanoMarco;
        var imgMarco = marco.gameObject.AddComponent<Image>();
        imgMarco.raycastTarget = false;
        if (tema != null && tema.marcoPanel != null) imgMarco.sprite = tema.marcoPanel;
        else imgMarco.color = tema != null ? tema.fondo : new Color(0.05f, 0.043f, 0.035f, 0.96f);

        var r = Nuevo("Titulo", go.transform);
        var texto = r.gameObject.AddComponent<TextMeshProUGUI>();
        if (tema != null && tema.fuenteTitulo != null)
        {
            texto.font = tema.fuenteTitulo;
            if (tema.materialTitulo != null) texto.fontSharedMaterial = tema.materialTitulo;
        }
        texto.fontSize = 66f;
        texto.color = tema != null ? tema.texto : Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;
        PonerTitulo(texto, titulo);
        return go;
    }

    private static void PonerTitulo(TMP_Text texto, string titulo)
    {
        texto.text = titulo;
        texto.textWrappingMode = TextWrappingModes.NoWrap;
        var r = texto.rectTransform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = PosicionTitulo;
        r.sizeDelta = new Vector2(AnchoContenido, 103f);
    }

    private void Construir(Button plantilla, TemaUI tema)
    {
        Color cMensaje = tema != null ? tema.brasa : new Color(0.85f, 0.27f, 0.12f);

        ranuras = new Button[SistemaGuardado.NumeroRanuras];
        textos = new TMP_Text[SistemaGuardado.NumeroRanuras];
        for (int i = 0; i < ranuras.Length; i++)
        {
            int indice = i;
            ranuras[i] = Clonar(plantilla, transform, $"Ranura_{i + 1}", "",
                new Vector2(0f, PrimeraRanura - i * SeparacionRanuras), new Vector2(AnchoContenido, AltoRanura));
            textos[i] = Etiqueta(ranuras[i]);
            textos[i].lineSpacing = -14f;
            ranuras[i].onClick.AddListener(() => AlElegir(indice));
        }

        mensaje = Nuevo("Mensaje", transform).gameObject.AddComponent<TextMeshProUGUI>();
        if (tema != null && tema.fuenteTexto != null)
        {
            mensaje.font = tema.fuenteTexto;
            if (tema.materialTexto != null) mensaje.fontSharedMaterial = tema.materialTexto;
        }
        mensaje.fontSize = 24f;
        mensaje.fontStyle = FontStyles.Italic;
        mensaje.color = cMensaje;
        mensaje.alignment = TextAlignmentOptions.Center;
        mensaje.textWrappingMode = TextWrappingModes.NoWrap;
        mensaje.raycastTarget = false;
        mensaje.rectTransform.anchoredPosition = new Vector2(0f, AlturaMensaje);
        mensaje.rectTransform.sizeDelta = new Vector2(AnchoContenido, 36f);

        var plantillaR = (RectTransform)plantilla.transform;
        botonVolver = Clonar(plantilla, transform, "Boton_Volver", "Volver",
            new Vector2(0f, AlturaVolver), new Vector2(Mathf.Min(plantillaR.sizeDelta.x, AnchoContenido), plantillaR.sizeDelta.y));
        botonVolver.onClick.AddListener(Volver);

        // Al pasar a otra ranura se cancela la confirmación pendiente
        foreach (var b in ranuras)
        {
            var aviso = b.gameObject.AddComponent<AvisoSeleccion>();
            aviso.alSeleccionar = () =>
            {
                if (confirmando < 0 || ranuras[confirmando] == b) return;
                confirmando = -1;
                Mensaje("");
                Refrescar();
            };
        }
    }

    private static Button Clonar(Button plantilla, Transform padre, string nombre, string texto, Vector2 pos, Vector2 tam)
    {
        var copia = Instantiate(plantilla, padre, false);
        copia.name = nombre;
        copia.onClick.RemoveAllListeners();
        copia.interactable = true;
        copia.transform.localScale = Vector3.one;
        var r = (RectTransform)copia.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
        var etiqueta = Etiqueta(copia);
        if (etiqueta != null) etiqueta.text = texto;
        return copia;
    }

    private static TMP_Text Etiqueta(Button b) => b.GetComponentInChildren<TMP_Text>(true);

    private static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(padre, false);
        return (RectTransform)go.transform;
    }

    private static void Estirar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }
}
