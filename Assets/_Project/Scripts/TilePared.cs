using UnityEngine;
using UnityEngine.Tilemaps;

// Tile de pared / suelo del castillo del jefe 2.
// Se pinta con UN solo tile y elige solo la pieza correcta segun su posicion,
// asi las 16 piezas forman un dibujo continuo sin costuras.
// Crear: lo crea la herramienta Tools > Ultimo Descenso > Configurar Arena Boss 2.
public class TilePared : TileBase
{
    [Tooltip("16 piezas: indice = columna * 4 + fila (fila 0 = arriba de la imagen)")]
    public Sprite[] piezas = new Sprite[16];
    public Tile.ColliderType colliderType = Tile.ColliderType.Grid;
    [Tooltip("Si no hay tile encima, no se dibuja (queda solo la colision): ahi se dibuja la orilla de roca")]
    public bool ocultarSiLibreArriba = false;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        int c = ((position.x % 4) + 4) % 4;
        int r = 3 - (((position.y % 4) + 4) % 4);
        Sprite s = (piezas != null && piezas.Length == 16) ? piezas[c * 4 + r] : null;
        if (ocultarSiLibreArriba && tilemap.GetTile(position + Vector3Int.up) == null) s = null;
        tileData.sprite = s;
        tileData.color = Color.white;
        tileData.transform = Matrix4x4.identity;
        tileData.flags = TileFlags.LockAll;
        tileData.colliderType = colliderType;
    }
}
