using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Asset_Generator : MonoBehaviour
{
    public int Length = 0; //z axis
    public int Width = 0; //x axis
    public int Height = 0; //y axis
    
    int dimensions;
    public Dictionary<int, GameObject> possibleTiles = new Dictionary<int, GameObject>();
    public GameObject[] tileObjects;

    bool createdGrid = false;

    public List<GameObject> gridComps;
    //public List<ShipCell> gridComps;
    public GameObject cellObj;

    Ship_Sockets faceForward = Ship_Sockets.Blank;
    Ship_Sockets faceBackward = Ship_Sockets.Blank;
    Ship_Sockets faceLeft = Ship_Sockets.Blank;
    Ship_Sockets faceRight = Ship_Sockets.Blank;
    Ship_Sockets faceUp = Ship_Sockets.Blank;
    Ship_Sockets faceDown = Ship_Sockets.Blank;

    int interations = 0;

    
    //adds the tiles set in the array to the dictionary with the array index as the key
    private void Start()
    {
        gridComps = new List<GameObject>();
        for(int i  = 0; i < tileObjects.Length; i++)
        {
            possibleTiles.Add(i, tileObjects[i]);
        }
        resetGrid();
    }
    //creates the grid if none exists or resets each cell if it does
    public void resetGrid()
    {
        if (!createdGrid) InitialsizeGrid();
        else { 
            foreach (GameObject cell in gridComps)
            {

                cell.GetComponent<ShipCell>().resetCell(possibleTiles);
         
            }
            interations = 0;
        }
    }

    void InitialsizeGrid()
    {
        dimensions = Length * Width * Height;
        createdGrid = true;
        for (int y = 0; y < Height; y++)
        {
            for (int z = 0; z < Width; z++)
            {
                for (int x = 0; x < Length; x++)
                {
                    GameObject createCell = Instantiate(cellObj, new Vector3((x), (y), (z)), Quaternion.identity);
                    ShipCell newCell = createCell.GetComponent<ShipCell>();
                    newCell.CreateCellXtra(false, possibleTiles, x, y, z);
                    newCell.index = x + (z * Length) + ((Width * Length) * y);
                    gridComps.Add(createCell);
                }
            }
        }

    }
    //used if a grid is to be used without an agent attached
    void setUpGridNoAgent()
    {
        if (!createdGrid) InitialsizeGrid();
        else
        {
            foreach (GameObject cell in gridComps)
            {

                cell.GetComponent<ShipCell>().resetCell(possibleTiles);

            }
            //gridComps.Clear();
            interations = 0;
        }
        StartCoroutine(checkEntropyNoAgent());
    }
    //sorts the cells to find the ones with the lowest number of available tiles to chose from
    IEnumerator checkEntropyNoAgent()
    {
        List<GameObject> tempGrid = new List<GameObject>(gridComps);

        tempGrid.RemoveAll(c => c.GetComponent<ShipCell>().collapsed);

        tempGrid.Sort((a, b) => { return a.GetComponent<ShipCell>().tileOptions.Length - b.GetComponent<ShipCell>().tileOptions.Length; });

        int arrLength = tempGrid[0].GetComponent<ShipCell>().tileOptions.Length;
        int stopIndex = default;

        for (int i = 0; i < tempGrid.Count; i++)
        {
            if (tempGrid[i].GetComponent<ShipCell>().tileOptions.Length > arrLength)
            {
                stopIndex = i;
                break;
            }
        }

        if (stopIndex > 0)
        {
            tempGrid.RemoveRange(stopIndex, tempGrid.Count - stopIndex);
        }

        yield return new WaitForSeconds(0.00001f);

        CollapseCell(tempGrid);
    }
    //returns a cell from its grid position
    public ShipCell getCell(int x, int z, int y)
    {
        ShipCell cell = null;
        int index = x + (z * Length) + ((Width * Length) * y);
        if(index >= 0 && index < (Width*Height*Length))
          cell = gridComps[index].GetComponent<ShipCell>();
       
        return cell;
    }

    //gets the dictionary key for a chosen tile
    public int getTileKey(ShipTile Tile)
    {
        int key = -1;

        for(int i = 0; i<tileObjects.Length; i++)
        {
            if (tileObjects[i].GetComponent<ShipTile>() == Tile)
            {
               
                if (possibleTiles[i].GetComponent<ShipTile>() == Tile)
                {
                    key = i;
                    break;
                }
            }
        }
        return key;
    }

    //takse the list of lowest entropy tiles and chooses a random one to collapse
    void CollapseCell(List<GameObject> tempGrid)
    {
        int randIndex = UnityEngine.Random.Range(0, tempGrid.Count);
        //a temporary list for choosing a random tile
        List<GameObject> tileOptions = new List<GameObject>();
        ShipCell cellToCollapse = tempGrid[randIndex].GetComponent<ShipCell>();
    
            cellToCollapse.collapsed = true;
            for (int i = 0; i < cellToCollapse.tileOptions.Length; i++)
            {   //fills the temp list from cell info
                if (cellToCollapse.tileOptions[i] != null)
                {
                    tileOptions.Add(cellToCollapse.tileOptions[i]);
                }
            }
            randIndex = UnityEngine.Random.Range(0, tileOptions.Count);
            GameObject selectedTile = tileOptions[randIndex];
            cellToCollapse.currentTile = selectedTile.GetComponent<ShipTile>();
            cellToCollapse.CurrentObject = selectedTile;

            UpdateGeneration();
        

        
    }
    //goes through the whole grid and chooses updates the cell constraints based on the neighbours
    public void UpdateGeneration()
    {

        List<GameObject> newGenerationCell = new List<GameObject>(gridComps);
        for (int height = 0; height < Height; height++)
        {
            for (int width = 0; width < Width; width++)
            {
                for (int length = 0; length < Length; length++)
                {
                    int layer = Length * Width;
                    int index = length + (width * Length) + (layer * height);
                    
                    if (gridComps[index].GetComponent<ShipCell>().collapsed)
                    {
                        newGenerationCell[index] = gridComps[index];
                    }
                    else
                    {
                        clearOptions();
                        if (length > 0)
                        {
                            ShipCell backward = getCell(length - 1, width, height);
                            if (backward.collapsed)
                            {
                               faceBackward = backward.currentTile.faceForward ;
                            }

                        }
                        if (width > 0)
                        {
                            ShipCell left = getCell(length, width-1, height);
                            if (left.collapsed)
                            {
                                faceLeft = left.currentTile.faceRight;
                            }
                        }
                        if (height > 0)
                        {
                            ShipCell down = getCell(length, width, height-1);
                            if (down.collapsed)
                            {
                                faceDown = down.currentTile.faceUp;
                            }
                        }
                        if (length < Length - 1)
                        {
                            ShipCell forward = getCell(length + 1, width, height);
                            if (forward.collapsed)
                            {
                                faceForward = forward.currentTile.faceBackward;
                            }
                        }
                        if (width < Width - 1)
                        {
                            ShipCell right = getCell(length, width + 1, height);
                            if (right.collapsed)
                            {
                                faceRight =right.currentTile.faceLeft;
                            }
                        }
                        if (height < Height - 1)
                        {
                            ShipCell up = getCell(length, width, height + 1);
                            if (up.collapsed)
                            {
                               faceUp = up.currentTile.faceDown;
                            }
                        }

                        Dictionary<int, GameObject> newTiles = checkTiles();
                        //updates the cell with valid tiles
                        newGenerationCell[index].GetComponent<ShipCell>().RecreateCell(newTiles);
                        //setting cell constraints for debug purposes
                        newGenerationCell[index].GetComponent<ShipCell>().faceDown = faceDown;
                        newGenerationCell[index].GetComponent<ShipCell>().faceUp = faceUp;
                        newGenerationCell[index].GetComponent<ShipCell>().faceLeft = faceLeft;
                        newGenerationCell[index].GetComponent<ShipCell>().faceRight = faceRight;
                        newGenerationCell[index].GetComponent<ShipCell>().faceForward = faceForward;
                        newGenerationCell[index].GetComponent<ShipCell>().faceBackward = faceBackward;
                       

                    }
                }
            }
        }
        gridComps = newGenerationCell;
        interations++;

        if (interations != dimensions)
        {
             //StartCoroutine(checkEntropyNoAgent()); //Disabled when using an agent
        }
    }

    
    Dictionary<int, GameObject> checkTiles()
    {
        Dictionary<int, GameObject> tempTiles = new Dictionary<int, GameObject>();
        foreach(KeyValuePair<int, GameObject> entry in possibleTiles)
        {
            tempTiles.Add(entry.Key, entry.Value);
        }
       for(int i = 0; i < possibleTiles.Count; i++)
        {
            ShipTile shipTile = possibleTiles[i].GetComponent<ShipTile>();

            if (faceUp != Ship_Sockets.None)
            {
                if (!shipTile.socketMatch(shipTile.faceUp, faceUp)) tempTiles.Remove(i);
            }
            if (faceDown != Ship_Sockets.None)
            {
                if (!shipTile.socketMatch(shipTile.faceDown, faceDown)) tempTiles.Remove(i);
            }
            if (faceLeft != Ship_Sockets.None)
            {
                if (!shipTile.socketMatch(shipTile.faceLeft, faceLeft)) tempTiles.Remove(i);
            }
            if (faceRight != Ship_Sockets.None)
            {
                if (!shipTile.socketMatch(shipTile.faceRight, faceRight)) tempTiles.Remove(i);
            }
            if (faceForward != Ship_Sockets.None)
            {
                if (!shipTile.socketMatch(shipTile.faceForward, faceForward)) tempTiles.Remove(i);
            }
            if (faceBackward != Ship_Sockets.None)
            {
                if (!shipTile.socketMatch(shipTile.faceBackward, faceBackward)) tempTiles.Remove(i);
            }

        }
        return tempTiles;
    }
 
    void clearOptions()
    {
        faceRight = Ship_Sockets.None;
        faceLeft = Ship_Sockets.None;
        faceUp = Ship_Sockets.None;
        faceDown = Ship_Sockets.None;
        faceForward = Ship_Sockets.None;
        faceBackward = Ship_Sockets.None;
    }
    //checks if a grid is still viable by seeing if a cell has no possible tiles
    public bool checkGridViable()
    {
        foreach(GameObject cell in gridComps)
        {
            ShipCell shipCell = cell.GetComponent<ShipCell>();

            if (shipCell.possibleTiles.Count == 0) return false;
        }
        return true;
    }
    //checks to see if all cells are collapsed
    public bool checkGridFill()
    {
        foreach (GameObject cell in gridComps)
        {
            ShipCell shipCell = cell.GetComponent<ShipCell>();

            if (shipCell.collapsed == false) return false;
        }
        return true;
    }
    //renders a grid by looping through a sent list and renders each cell
    public void createGrid(List<int> sentGrid)
    {
        resetGrid();
        for (int i = 0; i < gridComps.Count; i++)
        {
            ShipCell cell = gridComps[i].GetComponent<ShipCell>();
            cell.agentBuildGrid(sentGrid[i]);
        }

    }
   
}

public enum directions { faceUp, faceDown, faceForward, faceBackward, faceLeft, faceRight };