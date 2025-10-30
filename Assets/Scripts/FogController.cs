using UnityEngine;
using UnityEngine.Tilemaps;

public class FogManager : MonoBehaviour
{
    public Tilemap fogTilemap;
    public Tile fogTile;
    public Transform[] jugadores;
    public GeneradorMundo generadorMundo;
    public int bloquesDeVision = 2;

    void Start()
    {
        LlenarConNiebla();
    }

    void Update()
    {
        foreach (Transform jugador in jugadores)
        {
            if (jugador != null)
            {
                DespejarArea(jugador.position);
            }
        }
    }

    void LlenarConNiebla()
    {
        int ancho = generadorMundo.ancho;
        int profundidad = generadorMundo.profundidad;

        for (int x = 0; x < ancho; x++)
        {
            for (int y = -1; y > -profundidad; y--)
            {
                fogTilemap.SetTile(new Vector3Int(x, y, 0), fogTile);
            }
        }
    }

    void DespejarArea(Vector3 posicionDelJugador)
    {
        Vector3Int bloqueJugador = fogTilemap.WorldToCell(posicionDelJugador);

        for (int x = -bloquesDeVision; x <= bloquesDeVision; x++)
        {
            for (int y = -bloquesDeVision; y <= bloquesDeVision; y++)
            {
                Vector3Int bloqueADespejar = bloqueJugador + new Vector3Int(x, y, 0);
                fogTilemap.SetTile(bloqueADespejar, null);
            }
        }
    }
}