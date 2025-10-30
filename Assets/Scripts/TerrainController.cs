using UnityEngine;
using UnityEngine.Tilemaps;

public class GeneradorMundo : MonoBehaviour
{
    [Header("Referencias")]
    public Tilemap tilemap;

    [Header("Dimensiones del Mundo")]
    [Tooltip("El ancho fijo del mundo en número de bloques.")]
    public int ancho = 8;
    
    [Tooltip("Cuántos bloques de profundo se podrá cavar desde la superficie.")]
    public int profundidad = 100;

    [Header("Tiles")]
    public Tile pastoTile;
    public Tile tierraTile;
    public Tile carbonTile;
    public Tile oroTile;
    public Tile diamanteTile;
    public Tile esmeraldaTile;
    public Tile rubyTile;

    [Header("Probabilidad de Aparición de Minerales")]
    [Range(0, 1f)] public float probabilidadCarbon = 0.10f; 
    [Range(0, 1f)] public float probabilidadOro = 0.05f;   
    [Range(0, 1f)] public float probabilidadDiamante = 0.02f; 
    [Range(0, 1f)] public float probabilidadEsmeralda = 0.025f; 
    [Range(0, 1f)] public float probabilidadRuby = 0.01f; 

    // --- Métodos de Unity ---

    void Start()
    {
        GenerarTerreno();
    }

    // --- Lógica de Generación ---

    void GenerarTerreno()
    {
        tilemap.ClearAllTiles();
        
        for (int x = 0; x < ancho; x++)
        {
            for (int y = 0; y > -profundidad; y--)
            {
                Vector3Int posicionActual = new Vector3Int(x, y, 0);
                TileBase tileAColocar; 

                if (y == 0)
                {
                    tileAColocar = pastoTile;
                }
                else
                {
                    tileAColocar = DecidirTile();
                }
                
                tilemap.SetTile(posicionActual, tileAColocar);
            }
        }
    }

    private TileBase DecidirTile()
    {
        float aleatorio = Random.Range(0f, 1f);
        float acumulado = 0f;

        acumulado += probabilidadRuby;
        if (aleatorio < acumulado) return rubyTile;

        acumulado += probabilidadDiamante;
        if (aleatorio < acumulado) return diamanteTile;

        acumulado += probabilidadEsmeralda;
        if (aleatorio < acumulado) return esmeraldaTile;

        acumulado += probabilidadOro;
        if (aleatorio < acumulado) return oroTile;

        acumulado += probabilidadCarbon;
        if (aleatorio < acumulado) return carbonTile;

        return tierraTile;
    }
}