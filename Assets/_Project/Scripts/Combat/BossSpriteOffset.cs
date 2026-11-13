using UnityEngine;

// Baja (o sube) al jefe mientras suena una animacion concreta, para corregir
// diferencias de altura entre sprite sheets (por ejemplo el combo).
// Poner en el objeto Boss (el que tiene el Animator).
public class BossSpriteOffset : MonoBehaviour
{
    [Tooltip("Nombres de los estados del Animator que se desplazan")]
    [SerializeField] private string[] states = { "Boss_AttackCombo" };

    [Tooltip("Negativo = mas abajo, positivo = mas arriba (en unidades del mundo)")]
    [SerializeField] private float yOffset = -0.2f;

    [Tooltip("Cuanto tarda en moverse (0 = instantaneo)")]
    [SerializeField] private float smoothTime = 0f;

    private Animator animator;
    private float baseY;
    private float current;
    private float vel;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        baseY = transform.localPosition.y;
    }

    private void LateUpdate()
    {
        if (animator == null) return;

        float target = 0f;
        var info = animator.GetCurrentAnimatorStateInfo(0);
        for (int i = 0; i < states.Length; i++)
            if (info.IsName(states[i])) { target = yOffset; break; }

        current = smoothTime <= 0f ? target : Mathf.SmoothDamp(current, target, ref vel, smoothTime);

        var p = transform.localPosition;
        p.y = baseY + current;
        transform.localPosition = p;
    }
}
