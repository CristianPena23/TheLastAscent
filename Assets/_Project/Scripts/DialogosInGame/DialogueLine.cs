using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speaker;
    [TextArea(2, 5)] public string text;
    public AudioClip voice;   // voz en off, opcional
    public Sprite retrato;    // retrato de quien habla, opcional (cuadro izquierdo de la caja)
}