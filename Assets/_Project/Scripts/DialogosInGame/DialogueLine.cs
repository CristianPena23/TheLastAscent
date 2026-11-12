using UnityEngine;

// Acciones que una línea de diálogo dispara en la escena (las acotaciones del guion)
public enum AccionDialogo
{
    Ninguna,
    LuciernagaAparece,  // surge de la oscuridad y pulsa
    LuciernagaPulso,    // un pulso de luz
    LuciernagaIlumina,  // se adelanta a iluminar el camino y pulsa
}

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 5)] public string text;
    public AudioClip voice;   // voz en off, opcional
    public Sprite retrato;    // retrato de quien habla, opcional (cuadro izquierdo de la caja)

    [Tooltip("Acción en escena. Si el texto está vacío, solo ocurre la acción (la caja se oculta)")]
    public AccionDialogo accion;
    [Tooltip("Segundos que dura una línea sin texto antes de seguir")]
    public float duracion = 1.5f;
}
