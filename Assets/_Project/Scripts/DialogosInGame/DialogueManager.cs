using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject caja;
    [SerializeField] TMP_Text textoNombre;
    [SerializeField] TMP_Text textoDialogo;
    [SerializeField] Image retrato; // opcional: se oculta si la línea no tiene retrato
    [SerializeField] AudioSource audioVoz;

    [Header("Escritura")]
    [SerializeField] float segundosPorLetra = 0.03f;

    DialogueData actual;
    int indice;
    bool escribiendo;
    Coroutine rutina;

    public bool EnDialogo { get; private set; }

    public void Iniciar(DialogueData dialogo)
    {
        actual = dialogo;
        indice = 0;
        EnDialogo = true;
        caja.SetActive(true);
        MostrarLinea();
    }

    void MostrarLinea()
    {
        DialogueLine linea = actual.lines[indice];
        textoNombre.text = linea.speaker;
        if (retrato != null)
        {
            retrato.sprite = linea.retrato;
            retrato.enabled = linea.retrato != null;
        }

        audioVoz.Stop();
        if (linea.voice != null)
        {
            audioVoz.clip = linea.voice;
            audioVoz.Play();
        }

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Escribir(linea.text));
    }

    IEnumerator Escribir(string texto)
    {
        escribiendo = true;
        textoDialogo.text = texto;
        textoDialogo.maxVisibleCharacters = 0;

        for (int i = 0; i < texto.Length; i++)
        {
            textoDialogo.maxVisibleCharacters = i + 1;
            yield return new WaitForSecondsRealtime(segundosPorLetra);
        }

        escribiendo = false;
    }

    public void Avanzar()
    {
        if (!EnDialogo) return;

        if (escribiendo)
        {
            StopCoroutine(rutina);
            textoDialogo.maxVisibleCharacters = int.MaxValue;
            escribiendo = false;
            return;
        }

        indice++;
        if (indice >= actual.lines.Length) Terminar();
        else MostrarLinea();
    }

    public void Saltar()
    {
        if (EnDialogo) Terminar();
    }

    void Terminar()
    {
        if (rutina != null) StopCoroutine(rutina);
        audioVoz.Stop();
        caja.SetActive(false);
        EnDialogo = false;
        escribiendo = false;
    }
}