using System.Globalization;
using TMPro;
using UnityEngine;

// Muestra la cantidad de almas (HUD y menú de pausa)
public class ContadorAlmasUI : MonoBehaviour
{
    [SerializeField] private TMP_Text texto;
    [Tooltip("1,245 como en el mockup")]
    [SerializeField] private bool separadorDeMiles = true;

    private void OnEnable()
    {
        Almas.AlCambiar += Mostrar;
        Mostrar(Almas.Cantidad);
    }

    private void OnDisable() => Almas.AlCambiar -= Mostrar;

    private void Mostrar(int cantidad)
    {
        if (texto == null) return;
        texto.text = separadorDeMiles
            ? cantidad.ToString("#,0", CultureInfo.InvariantCulture)
            : cantidad.ToString(CultureInfo.InvariantCulture);
    }
}
