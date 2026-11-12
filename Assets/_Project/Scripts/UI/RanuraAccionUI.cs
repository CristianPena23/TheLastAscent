using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Ranura de acción del HUD (ataque, movimiento, especial). Muestra la tecla o el botón real de la acción
// según el último dispositivo usado (teclado o gamepad), leyendo los bindings de InputSystem_Actions.
public class RanuraAccionUI : MonoBehaviour
{
    [SerializeField] private InputActionReference accion;
    [SerializeField] private TMP_Text textoTecla;
    [SerializeField] private float intervalo = 0.5f;

    private float espera;
    private bool ultimoMando;

    private void OnEnable() => Actualizar(true);

    private void Update()
    {
        espera -= Time.unscaledDeltaTime;
        if (espera > 0f) return;
        espera = intervalo;
        Actualizar(false);
    }

    private void Actualizar(bool forzar)
    {
        if (accion == null || accion.action == null || textoTecla == null) return;

        bool mando = UsandoMando();
        if (!forzar && mando == ultimoMando) return;
        ultimoMando = mando;

        string grupo = mando ? "Gamepad" : "Keyboard&Mouse";
        string tecla = accion.action.GetBindingDisplayString(InputBinding.MaskByGroup(grupo));
        if (string.IsNullOrEmpty(tecla)) tecla = accion.action.GetBindingDisplayString();
        textoTecla.text = tecla;
    }

    private static bool UsandoMando()
    {
        var mando = Gamepad.current;
        if (mando == null) return false;
        var teclado = Keyboard.current;
        return teclado == null || mando.lastUpdateTime > teclado.lastUpdateTime;
    }
}
