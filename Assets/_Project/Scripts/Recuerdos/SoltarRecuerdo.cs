using System.Collections;
using UnityEngine;

// Hace que un jefe suelte su recuerdo al morir. BossHealth lo agrega solo: el recuerdo se busca
// en el catálogo (Recuerdos.DeJefe) por el nombre del jefe. Si se quiere otro texto,
// poner este componente a mano en el jefe y marcar "usarPropio".
[RequireComponent(typeof(BossHealth))]
public class SoltarRecuerdo : MonoBehaviour
{
    [Tooltip("Usar el recuerdo de abajo en vez del catálogo")]
    [SerializeField] private bool usarPropio;
    [SerializeField] private DatosRecuerdo recuerdo = new DatosRecuerdo();
    [Tooltip("Segundos después de que el jefe termina de desvanecerse")]
    [SerializeField] private float espera = 0.6f;

    private BossHealth jefe;
    private FragmentoRecuerdo fragmento;

    // true mientras el recuerdo está en el suelo sin recoger (BossHealth no cambia de escena)
    public bool Pendiente => fragmento != null;
    public float Espera => espera;

    public DatosRecuerdo Datos => usarPropio ? recuerdo : Recuerdos.DeJefe(jefe != null ? jefe.BossName : "");

    private void Awake()
    {
        jefe = GetComponent<BossHealth>();
        jefe.Died += AlMorir;
    }

    private void OnDestroy()
    {
        if (jefe != null) jefe.Died -= AlMorir;
    }

    private void AlMorir()
    {
        var datos = Datos;
        if (datos == null || Recuerdos.Tiene(datos.id)) return;
        StartCoroutine(Soltar(datos));
    }

    private IEnumerator Soltar(DatosRecuerdo datos)
    {
        // Centro del jefe antes de que se apague el sprite
        var sprite = GetComponentInChildren<SpriteRenderer>();
        Vector3 origen = sprite != null ? sprite.bounds.center : transform.position;

        yield return new WaitForSeconds(jefe.DeathFadeTime + espera);
        fragmento = FragmentoRecuerdo.Crear(datos, origen);
    }
}
