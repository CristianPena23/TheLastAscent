using System;
using UnityEngine;

public class EnemigoBase : MonoBehaviour
{
    [SerializeField] float velocidadPatrulla = 1.5f;
    Rigidbody2D rb;

    int direccion = 1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(direccion * velocidadPatrulla, rb.linearVelocity.y);
    }
    [ContextMenu("Voltear")]
    void Voltear()
    {
        direccion = -direccion;

        Vector3 scale = transform.localScale;
        scale.x = -scale.x;
        transform.localScale = scale;
    }
    
}
