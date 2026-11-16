using System;
using System.Collections;
using System.IO;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

// Guardado y carga de partidas en ranuras (archivos JSON en Application.persistentDataPath).
// Se crea solo al iniciar el juego, no hay que ponerlo en ninguna escena.
//
// Uso:
//   SistemaGuardado.Guardar(0);      // guarda la partida actual en la ranura 1
//   SistemaGuardado.Cargar(0);       // carga la escena guardada y deja al jugador como estaba
//   SistemaGuardado.NuevaPartida();  // olvida almas, llaves, diálogos vistos y tiempo
//   SistemaGuardado.Leer(0);         // datos de la ranura (null si está vacía o dañada)
//
// Se guarda: escena, posición, punto de reaparición, vida, energía, almas, llaves,
// diálogos de entrada ya vistos, aviso del poder, recuerdos recuperados, tiempo jugado y fecha.
public class SistemaGuardado : MonoBehaviour
{
    public const int NumeroRanuras = 3;
    private const string Prefijo = "partida_";

    private static SistemaGuardado instancia;
    private static DatosPartida pendiente; // partida que se está cargando: se aplica al llegar a su escena

    // Segundos jugados en la partida actual (no cuenta la pausa ni el menú)
    public static float TiempoJugado { get; private set; }

    private bool hayJugador;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void LimpiarEstaticos()
    {
        pendiente = null;
        TiempoJugado = 0f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("SistemaGuardado");
        DontDestroyOnLoad(go);
        go.AddComponent<SistemaGuardado>();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (instancia != this) return;
        instancia = null;
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    private void Update()
    {
        if (hayJugador && !PausaJuego.EstaPausado && !SceneTransition.IsBusy)
            TiempoJugado += Time.unscaledDeltaTime;
    }

    // ---------- Ranuras ----------

    public static string Ruta(int ranura) =>
        Path.Combine(Application.persistentDataPath, $"{Prefijo}{ranura + 1}.json");

    public static bool Existe(int ranura) => File.Exists(Ruta(ranura));

    public static bool HayPartidas()
    {
        for (int i = 0; i < NumeroRanuras; i++)
            if (Existe(i)) return true;
        return false;
    }

    // null si la ranura está vacía o el archivo está dañado
    public static DatosPartida Leer(int ranura)
    {
        string ruta = Ruta(ranura);
        if (!File.Exists(ruta)) return null;
        try
        {
            var datos = JsonUtility.FromJson<DatosPartida>(File.ReadAllText(ruta));
            return datos != null && !string.IsNullOrEmpty(datos.escena) ? datos : null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"SistemaGuardado: no se pudo leer la ranura {ranura + 1}: {e.Message}");
            return null;
        }
    }

    public static void Borrar(int ranura)
    {
        try { if (Existe(ranura)) File.Delete(Ruta(ranura)); }
        catch (Exception e) { Debug.LogWarning($"SistemaGuardado: no se pudo borrar la ranura {ranura + 1}: {e.Message}"); }
    }

    // ---------- Guardar ----------

    // Devuelve false si no hay jugador en la escena (menú) o si falla la escritura
    public static bool Guardar(int ranura)
    {
        if (ranura < 0 || ranura >= NumeroRanuras) return false;

        var jugador = FindFirstObjectByType<PlayerHealth>();
        if (jugador == null || jugador.IsDead) return false;

        var energia = jugador.GetComponent<PlayerEnergy>();
        Vector3 pos = jugador.transform.position;
        Vector3 reaparicion = jugador.PuntoReaparicion;

        var datos = new DatosPartida
        {
            escena = SceneManager.GetActiveScene().name,
            circulo = DatosCirculo.NombreActual(),
            posX = pos.x,
            posY = pos.y,
            reaparicionX = reaparicion.x,
            reaparicionY = reaparicion.y,
            vida = jugador.CurrentHealth,
            energia = energia != null ? energia.Current : 0,
            almas = Almas.Cantidad,
            llaves = new System.Collections.Generic.List<string>(KeyInventory.Todas),
            dialogosVistos = IntroCaida.Mostrados(),
            avisoPoderMostrado = PoderInmunidad.AvisoMostrado,
            recuerdos = Recuerdos.Todos(),
            tiempoJugado = TiempoJugado,
            fecha = DateTime.Now.ToString("o")
        };

        // Se escribe a un temporal y luego se reemplaza: si el juego se cierra a mitad, la ranura vieja sigue entera
        string ruta = Ruta(ranura);
        string temporal = ruta + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            File.WriteAllText(temporal, JsonUtility.ToJson(datos, true));
            if (File.Exists(ruta)) File.Delete(ruta);
            File.Move(temporal, ruta);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SistemaGuardado: no se pudo guardar en la ranura {ranura + 1}: {e.Message}");
            try { if (File.Exists(temporal)) File.Delete(temporal); } catch { }
            return false;
        }
    }

    // ---------- Cargar ----------

    // Devuelve false si la ranura está vacía/dañada o su escena no está en Build Settings
    public static bool Cargar(int ranura)
    {
        var datos = Leer(ranura);
        if (datos == null) return false;
        if (!Application.CanStreamedLevelBeLoaded(datos.escena))
        {
            Debug.LogWarning($"SistemaGuardado: la escena \"{datos.escena}\" no está en Build Settings.");
            return false;
        }

        ReiniciarProgreso();
        Almas.Establecer(datos.almas);
        if (datos.llaves != null) foreach (var llave in datos.llaves) KeyInventory.Add(llave);
        IntroCaida.MarcarMostrados(datos.dialogosVistos);
        PoderInmunidad.AvisoMostrado = datos.avisoPoderMostrado;
        Recuerdos.Marcar(datos.recuerdos);
        TiempoJugado = datos.tiempoJugado;

        pendiente = datos;
        if (SceneTransition.Instance != null) SceneTransition.Go(datos.escena, "");
        else SceneManager.LoadScene(datos.escena);
        return true;
    }

    // Partida nueva: nada de lo anterior se arrastra
    public static void NuevaPartida()
    {
        pendiente = null;
        ReiniciarProgreso();
        TiempoJugado = 0f;
    }

    private static void ReiniciarProgreso()
    {
        Almas.Reiniciar();
        KeyInventory.Clear();
        IntroCaida.OlvidarMostrados();
        PoderInmunidad.OlvidarAviso();
        Recuerdos.Olvidar();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        hayJugador = FindFirstObjectByType<PlayerController>() != null;

        if (pendiente == null || escena.name != pendiente.escena) return;
        var datos = pendiente;
        pendiente = null;
        StartCoroutine(AplicarAlJugador(datos));
    }

    // Espera un frame para que todos los Start() de la escena hayan corrido (vida/energía iniciales, SpawnPoint)
    private IEnumerator AplicarAlJugador(DatosPartida datos)
    {
        yield return null;

        var jugador = FindFirstObjectByType<PlayerHealth>();
        if (jugador == null)
        {
            Debug.LogWarning("SistemaGuardado: la escena cargada no tiene jugador.");
            yield break;
        }

        Vector3 pos = jugador.transform.position;
        Vector3 nueva = new Vector3(datos.posX, datos.posY, pos.z);
        jugador.transform.position = nueva;
        var rb = jugador.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = nueva;
            rb.linearVelocity = Vector2.zero;
        }

        jugador.SetRespawnPoint(new Vector3(datos.reaparicionX, datos.reaparicionY, pos.z));
        jugador.EstablecerVida(datos.vida);
        var energia = jugador.GetComponent<PlayerEnergy>();
        if (energia != null) energia.Establecer(datos.energia);

        // La cámara salta al jugador en vez de deslizarse desde la posición inicial de la escena
        foreach (var cam in FindObjectsByType<CinemachineVirtualCameraBase>(FindObjectsSortMode.None))
            cam.PreviousStateIsValid = false;
    }
}
