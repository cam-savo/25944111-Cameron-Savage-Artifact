using System.Collections.Generic;
using UnityEngine;

public class ShipCell : MonoBehaviour
{
    public bool collapsed;
    public GameObject[] tileOptions;
    public Dictionary<int, GameObject> possibleTiles = new Dictionary<int, GameObject>();
    public GameObject CurrentObject;
    public ShipTile currentTile;
    public Ship_Sockets faceForward = Ship_Sockets.Blank;
    public Ship_Sockets faceBackward = Ship_Sockets.Blank;
    public Ship_Sockets faceLeft = Ship_Sockets.Blank;
    public Ship_Sockets faceRight = Ship_Sockets.Blank;
    public Ship_Sockets faceUp = Ship_Sockets.Blank;
    public Ship_Sockets faceDown = Ship_Sockets.Blank;
    public int index;
    public int X;
    public int Y;
    public int Z;
    int maxTiles;

    //creates the cell and sets all base values
    public void CreateCell(bool collapsedState, Dictionary<int, GameObject> tiles)
    {
        collapsed = collapsedState;
        possibleTiles.Clear();
        foreach (KeyValuePair<int, GameObject> entry in tiles)
        {
            possibleTiles.Add(entry.Key, entry.Value);
        }
        foreach (KeyValuePair<int, GameObject> entry in possibleTiles)
        {
            tileOptions[entry.Key] = entry.Value;
        }
    }
    //same as other cell including the location for debug purposes
    public void CreateCellXtra(bool collapsedState, Dictionary<int, GameObject> tiles,int x, int y, int z)
    {
        collapsed = collapsedState;
        maxTiles = tiles.Count;
        tileOptions = new GameObject[tiles.Count];
        possibleTiles.Clear();
        foreach (KeyValuePair<int, GameObject> entry in tiles)
        {
            possibleTiles.Add(entry.Key, entry.Value);
        }
        foreach (KeyValuePair<int, GameObject> entry in possibleTiles)
        {
            tileOptions[entry.Key] = entry.Value;
        }
            X =x; Y=y; Z=z;

    }
    public void RecreateCell(Dictionary<int, GameObject> tiles)
    {
        possibleTiles.Clear();
        foreach (KeyValuePair<int, GameObject> entry in tiles)
        {
            possibleTiles.Add(entry.Key, entry.Value);
        }
    }

    //collapses the cell by setting the tile comp and object value and spawns the tile
    public void collapseCell(GameObject tile)
    {
        collapsed = true;
        GameObject selectedTile = tile;
        currentTile = selectedTile.GetComponent<ShipTile>();


        GameObject spawnTile = Instantiate(selectedTile, transform.position, Quaternion.identity);
        spawnTile.transform.transform.localScale = new Vector3(1f, 1f, 1f);
        CurrentObject = spawnTile;
    }

    //collapses the cell by setting the tile comp and object value without spawning the tile
    public void agentCollapseCell(int tileID)
    {
        collapsed = true;
        GameObject selectedTile = possibleTiles[tileID];
        CurrentObject = selectedTile;
        currentTile = selectedTile.GetComponent<ShipTile>();

    }
    //triggered when the grid is reset to set all values back to default without needing to delete and instansiate a new cell
    public void resetCell(Dictionary<int, GameObject> baselineTiles)
    {
        collapsed = false;
        possibleTiles.Clear();
        foreach (KeyValuePair<int, GameObject> entry in baselineTiles)
        {
            possibleTiles.Add(entry.Key, entry.Value);
        }
       // debugUpdateArray();
        currentTile = null;
        CurrentObject = null;
        faceForward = Ship_Sockets.None;
        faceBackward = Ship_Sockets.None;
        faceLeft = Ship_Sockets.None;
        faceRight = Ship_Sockets.None;
        faceUp = Ship_Sockets.None;
        faceDown = Ship_Sockets.None;

}

    public void UpdateArray()
    {
        tileOptions = new GameObject[maxTiles];

        foreach (KeyValuePair<int, GameObject> entry in possibleTiles)
        {
            tileOptions[entry.Key] = entry.Value;
        }
    }
    //gets a random valid tile for the standard WFC system
    public int getValidTile()
    {
        int tileID = -1;
        int i = 0;
        int[] tiles = new int[] { };
        foreach (KeyValuePair<int, GameObject> entry in possibleTiles)
        {
            tiles[i] = entry.Key;
        }

        tileID = UnityEngine.Random.Range(0, tiles.Length);

            return tileID;
    }
    //spawns the grid from a saved grid from the asset generator
    public void agentBuildGrid(int tileID)
    {
        collapsed = true;
        GameObject selectedTile = possibleTiles[tileID];

        GameObject spawnTile = Instantiate(selectedTile, transform.position, Quaternion.identity);
        spawnTile.transform.transform.localScale = new Vector3(1f, 1f, 1f);
        spawnTile.GetComponent<ShipTile>().index = index;
        CurrentObject = selectedTile;
        currentTile = selectedTile.GetComponent<ShipTile>();

    }
}
