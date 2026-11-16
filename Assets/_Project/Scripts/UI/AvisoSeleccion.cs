using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Avisa cuando el objeto recibe la selección (teclado, mando o hover del mouse). Lo usa PanelRanuras.
public class AvisoSeleccion : MonoBehaviour, ISelectHandler
{
    public Action alSeleccionar;

    public void OnSelect(BaseEventData e) => alSeleccionar?.Invoke();
}
