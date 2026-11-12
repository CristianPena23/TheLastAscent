using System.Collections;
using UnityEngine;

public class IntroCaida : MonoBehaviour
{
    [Header("Jugador")]
    [SerializeField] PlayerController control;
    [SerializeField] PlayerDash dash;
    [SerializeField] Transform groundCheck;
    [SerializeField] float radio = 0.2f;
    [SerializeField] LayerMask capaSuelo;

    [Header("Diálogo")]
    [SerializeField] DialogueManager dialogos;
    [SerializeField] DialogueData dialogoIntro;
    [SerializeField] float pausaTrasImpacto = 0.6f;

    IEnumerator Start()
    {
        control.enabled = false;
        dash.enabled = false;

        yield return new WaitForSeconds(0.5f);

        while (!Physics2D.OverlapCircle(groundCheck.position, radio, capaSuelo))
            yield return null;

        yield return new WaitForSeconds(pausaTrasImpacto);

        dialogos.Iniciar(dialogoIntro);

        while (dialogos.EnDialogo)
            yield return null;

        control.enabled = true;
        dash.enabled = true;
    }
}