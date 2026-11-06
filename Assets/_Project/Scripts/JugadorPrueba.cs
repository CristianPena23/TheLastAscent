using UnityEngine;
using UnityEngine.InputSystem;

public class JugadorPrueba : MonoBehaviour
{
    [SerializeField] float velocidad = 5f;
    [SerializeField] float fuerzaSalto = 10f;
    [SerializeField] Transform sensorSuelo;
    [SerializeField] LayerMask capaSuelo;

    Rigidbody2D rb;
    float entradaHorizontal;
    bool quiereSaltar;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;

        entradaHorizontal = 0f;
        if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) entradaHorizontal = -1f;
        if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) entradaHorizontal = 1f;

        if (teclado.spaceKey.wasPressedThisFrame)
        {
            quiereSaltar = true;
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(entradaHorizontal * velocidad, rb.linearVelocity.y);

        if (quiereSaltar)
        {
            if (EstaEnElSuelo())
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            }
            quiereSaltar = false;
        }
    }

    bool EstaEnElSuelo()
    {
        return Physics2D.OverlapCircle(sensorSuelo.position, 0.1f, capaSuelo);
    }
}