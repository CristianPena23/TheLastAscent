using System;
using System.Collections.Generic;

// Lo que se escribe en cada ranura de guardado (JSON en Application.persistentDataPath).
// Si se agrega un campo nuevo, las partidas viejas lo cargan con su valor por defecto.
[Serializable]
public class DatosPartida
{
    public int version = 1;

    // Dónde
    public string escena;
    public string circulo;                   // "Círculo IX", para mostrarlo en la ranura
    public float posX, posY;                 // posición del jugador al guardar
    public float reaparicionX, reaparicionY; // último checkpoint / entrada de la escena

    // Jugador
    public int vida;
    public int energia;
    public int almas;

    // Progreso
    public List<string> llaves = new List<string>();
    public List<string> dialogosVistos = new List<string>();
    public bool avisoPoderMostrado;
    public List<string> recuerdos = new List<string>(); // ids de Recuerdos recuperados

    // Ranura
    public float tiempoJugado; // segundos
    public string fecha;       // DateTime.Now en formato ISO ("o")
}
