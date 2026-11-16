using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de pausa (Esc / Start): Continuar, Guardar, Opciones, Salir al menú.
// "Guardar" abre las ranuras de guardado (PanelRanuras). Si el prefab no trae el botón,
// se crea solo copiando el de Opciones y se reacomoda la columna de botones.
// No se abre durante un diálogo, una transición de escena o con el jugador muerto.
public class MenuPausa : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private PanelUI panelPausa;
    [SerializeField] private PanelOpciones panelOpciones;

    [Header("Botones")]
    [SerializeField] private Button botonContinuar;
    [Tooltip("Vacío = se crea solo copiando el botón de Opciones")]
    [SerializeField] private Button botonGuardar;
    [SerializeField] private Button botonOpciones;
    [SerializeField] private Button botonSalirMenu;

    [SerializeField] private string escenaMenu = "SC_MenuPrincipal";

    [Header("Botón de pausa en pantalla")]
    [SerializeField] private bool mostrarBotonPausa = true;
    [Tooltip("Posición desde la esquina superior derecha (referencia 2400x1350)")]
    [SerializeField] private Vector2 posicionBoton = new Vector2(-70f, -60f);
    [SerializeField] private float tamanoBoton = 60f;

    private GameObject botonPausa;
    private PanelRanuras panelRanuras;

    private PlayerHealth jugador;
    private DialogueManager dialogos;
    private bool saliendo;

    private void Awake()
    {
        if (botonContinuar != null) botonContinuar.onClick.AddListener(Continuar);
        if (botonOpciones != null) botonOpciones.onClick.AddListener(AbrirOpciones);
        if (botonSalirMenu != null) botonSalirMenu.onClick.AddListener(SalirAlMenu);
        PrepararGuardado();
    }

    private void PrepararGuardado()
    {
        Button plantilla = botonOpciones != null ? botonOpciones : botonContinuar;
        if (plantilla == null) return;

        if (botonGuardar == null) botonGuardar = CrearBotonGuardar(plantilla);
        botonGuardar.onClick.AddListener(AbrirGuardado);

        // Copia el panel de pausa (marco, título, círculo y almas) para que se vea igual
        panelRanuras = PanelRanuras.Crear(transform, plantilla, PanelRanuras.Modo.Guardar, panelPausa);
    }

    // Copia el botón de Opciones, lo pone justo antes y reparte la columna con el mismo espaciado
    private Button CrearBotonGuardar(Button plantilla)
    {
        var columna = new List<RectTransform>();
        foreach (var b in new[] { botonContinuar, botonOpciones, botonSalirMenu })
            if (b != null) columna.Add((RectTransform)b.transform);

        var nuevo = Instantiate(plantilla, plantilla.transform.parent, false);
        nuevo.name = "Boton_Guardar";
        nuevo.onClick.RemoveAllListeners();
        nuevo.interactable = true;
        var etiqueta = nuevo.GetComponentInChildren<TMP_Text>(true);
        if (etiqueta != null) etiqueta.text = "Guardar";
        nuevo.transform.SetSiblingIndex(plantilla.transform.GetSiblingIndex());

        int indice = botonOpciones != null ? columna.IndexOf((RectTransform)botonOpciones.transform) : columna.Count;
        if (indice < 0) indice = columna.Count;

        if (columna.Count >= 2)
        {
            float paso = columna[0].anchoredPosition.y - columna[1].anchoredPosition.y;
            float centro = (columna[0].anchoredPosition.y + columna[columna.Count - 1].anchoredPosition.y) * 0.5f;
            columna.Insert(indice, (RectTransform)nuevo.transform);
            float arriba = centro + paso * (columna.Count - 1) * 0.5f;
            for (int i = 0; i < columna.Count; i++)
            {
                var p = columna[i].anchoredPosition;
                columna[i].anchoredPosition = new Vector2(p.x, arriba - paso * i);
            }
        }
        return nuevo;
    }

    private void Start()
    {
        jugador = FindFirstObjectByType<PlayerHealth>();
        dialogos = FindFirstObjectByType<DialogueManager>();
        if (mostrarBotonPausa) CrearBotonPausa();
    }

    // Botón "II" para pausar con el mouse o el dedo (Esc / Start siguen funcionando)
    private void CrearBotonPausa()
    {
        var tema = TemaUI.Predeterminado;
        Color borde = tema != null ? tema.textoNormal : new Color(0.8f, 0.75f, 0.68f);
        Color barras = tema != null ? tema.texto : Color.white;

        botonPausa = new GameObject("BotonPausa", typeof(RectTransform));
        botonPausa.layer = 5;
        botonPausa.transform.SetParent(transform, false);
        botonPausa.transform.SetAsFirstSibling(); // el panel de pausa queda por encima
        var r = (RectTransform)botonPausa.transform;
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = posicionBoton;
        r.sizeDelta = new Vector2(tamanoBoton, tamanoBoton);

        var fondo = botonPausa.AddComponent<Image>();
        fondo.color = new Color(0.05f, 0.04f, 0.035f, 0.75f);
        var contorno = botonPausa.AddComponent<Outline>();
        contorno.effectColor = new Color(borde.r, borde.g, borde.b, 0.7f);
        contorno.effectDistance = new Vector2(2f, -2f);

        var boton = botonPausa.AddComponent<Button>();
        boton.targetGraphic = fondo;
        var colores = boton.colors;
        colores.highlightedColor = new Color(1.3f, 1.2f, 1.1f);
        colores.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        boton.colors = colores;
        boton.navigation = new Navigation { mode = Navigation.Mode.None }; // que no lo seleccione el teclado
        boton.onClick.AddListener(() => { if (!PausaJuego.EstaPausado && PuedePausar()) Pausar(); });

        // Dos barras verticales: "II"
        for (int i = 0; i < 2; i++)
        {
            var barra = new GameObject("Barra", typeof(RectTransform));
            barra.layer = 5;
            barra.transform.SetParent(botonPausa.transform, false);
            var rb = (RectTransform)barra.transform;
            rb.anchorMin = rb.anchorMax = new Vector2(0.5f, 0.5f);
            rb.sizeDelta = new Vector2(tamanoBoton * 0.13f, tamanoBoton * 0.45f);
            rb.anchoredPosition = new Vector2((i == 0 ? -1f : 1f) * tamanoBoton * 0.12f, 0f);
            var img = barra.AddComponent<Image>();
            img.color = barras;
            img.raycastTarget = false;
        }
    }

    private void OnDestroy()
    {
        if (PausaJuego.EstaPausado) PausaJuego.Reanudar();
    }

    private void Update()
    {
        if (saliendo) return;

        if (!PausaJuego.EstaPausado)
        {
            if (EntradaUI.PausaPresionada() && PuedePausar()) Pausar();
            return;
        }

        if (panelOpciones != null && panelOpciones.EstaAbierto)
        {
            if (EntradaUI.AtrasPresionado() || EntradaUI.PausaPresionada()) panelOpciones.Volver();
            return;
        }

        if (panelRanuras != null && panelRanuras.EstaAbierto)
        {
            if (EntradaUI.AtrasPresionado() || EntradaUI.PausaPresionada()) panelRanuras.Volver();
            return;
        }

        if (EntradaUI.AtrasPresionado() || EntradaUI.PausaPresionada())
        {
            SonidosUI.Volver();
            Continuar();
        }
    }

    private bool PuedePausar()
    {
        if (SceneTransition.IsBusy) return false;
        if (jugador != null && jugador.IsDead) return false;
        if (dialogos != null && dialogos.EnDialogo) return false;
        if (PantallaRecuerdo.Abierta) return false;
        if (CinematicaInicio.Reproduciendo) return false;
        return true;
    }

    public void Pausar()
    {
        if (botonPausa != null) botonPausa.SetActive(false);
        PausaJuego.Pausar();
        if (panelPausa != null) panelPausa.Abrir();
    }

    public void Continuar()
    {
        if (panelOpciones != null && panelOpciones.EstaAbierto) panelOpciones.Cerrar(true);
        if (panelRanuras != null && panelRanuras.EstaAbierto) panelRanuras.Cerrar(true);
        if (panelPausa != null) panelPausa.Cerrar();
        PausaJuego.Reanudar();
        if (botonPausa != null) botonPausa.SetActive(true);
    }

    public void AbrirOpciones()
    {
        if (panelOpciones != null) panelOpciones.AbrirDesde(panelPausa);
    }

    public void AbrirGuardado()
    {
        if (panelRanuras != null) panelRanuras.AbrirDesde(panelPausa);
    }

    // Se queda en pausa durante el fade; PausaJuego se reanuda solo al cargar la escena nueva
    public void SalirAlMenu()
    {
        saliendo = true;
        if (SceneTransition.Instance != null) SceneTransition.Go(escenaMenu, "");
        else SceneManager.LoadScene(escenaMenu);
    }
}
