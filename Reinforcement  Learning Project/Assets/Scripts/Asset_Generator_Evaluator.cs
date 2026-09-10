using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asset_Generator_Evaluator : MonoBehaviour
{
    public int Length = 0; //z axis
    public int Width = 0; //x axis
    public int Height = 0; //y axis

    int dimensions;
    public Dictionary<int, GameObject> possibleTiles = new Dictionary<int, GameObject>();
    public GameObject[] tileObjects;
    public GridChecker gridChecker;
    public List<GameObject> gridComps;
    public GameObject cellObj;

    Ship_Sockets faceForward = Ship_Sockets.Blank;
    Ship_Sockets faceBackward = Ship_Sockets.Blank;
    Ship_Sockets faceLeft = Ship_Sockets.Blank;
    Ship_Sockets faceRight = Ship_Sockets.Blank;
    Ship_Sockets faceUp = Ship_Sockets.Blank;
    Ship_Sockets faceDown = Ship_Sockets.Blank;

    List<int> currentGrid = new List<int>();
    Dictionary<string, List<int>> FinishedGrids = new Dictionary<string, List<int>>();

    bool Allshapes = true;
    bool nacelle = true;
    bool stardrive = true;
    bool saucer = true;

    bool createdGrid = false;
    int episodeCOunt = 0;
    int maxEpisodes = 500;



    private void Start()
    {
        gridComps = new List<GameObject>();
        for (int i = 0; i < tileObjects.Length; i++)
        {
            possibleTiles.Add(i, tileObjects[i]);
        }
        beginEpisode();
    }

    //checks if max episodes have been hit if not will generate grid
    private void beginEpisode()
    {
        
        if (episodeCOunt < maxEpisodes)
        {
            episodeCOunt++;
            Debug.Log(episodeCOunt);
            resetGrid();

        }
        else
        {
            EvaluateGrids();
        }
    }

    public void resetGrid()
    {
        if (!createdGrid) InitialsizeGrid();
        foreach (GameObject cell in gridComps)
        {
            cell.GetComponent<ShipCell>().resetCell(possibleTiles);
        }
        StartCoroutine(checkEntropyNoAgent());
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
        StartCoroutine(checkEntropyNoAgent());

    }

    IEnumerator checkEntropyNoAgent()
    {
        ShipCell chosenCell = null;

        int lowestEntropy = int.MaxValue;


        foreach (GameObject gridcell in gridComps)
        {
            ShipCell cell = gridcell.GetComponent<ShipCell>();
            if (cell.collapsed) continue;
            if (cell.possibleTiles.Count < lowestEntropy)
            {
                chosenCell = cell;
                lowestEntropy = cell.possibleTiles.Count;
            }
        }

        yield return new WaitForSeconds(0.00001f);
        collapseCell(chosenCell);
    }
    public ShipCell getCell(int x, int z, int y)
    {
        ShipCell cell = null;
        int index = x + (z * Length) + ((Width * Length) * y);
        if (index >= 0 && index < (Width * Height * Length))
            cell = gridComps[index].GetComponent<ShipCell>();

        return cell;
    }

    int getTileKey(ShipTile tile)
    {
        int key = -1;
        for(int i = 0; i < tileObjects.Length; i++)
        {
            if (tileObjects[i].GetComponent<ShipTile>() == tile)
            {
                if(possibleTiles[i].GetComponent<ShipTile>() == tile)
                {
                    key = i;
                    break;
                }
            }
        }
        return key;
    }

    private void collapseCell(ShipCell cellToCollapse)
    {
        cellToCollapse.collapsed = true;
        cellToCollapse.UpdateArray();
        GameObject selectedTile = cellToCollapse.possibleTiles[cellToCollapse.getValidTile()];
        cellToCollapse.currentTile = selectedTile.GetComponent<ShipTile>();


        cellToCollapse.CurrentObject = selectedTile;

        UpdateGeneration();
    }
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
                                faceBackward = backward.currentTile.faceForward;
                            }

                        }
                        if (width > 0)
                        {
                            ShipCell left = getCell(length, width - 1, height);
                            if (left.collapsed)
                            {
                                faceLeft = left.currentTile.faceRight;
                            }
                        }
                        if (height > 0)
                        {
                            ShipCell down = getCell(length, width, height - 1);
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
                                faceRight = right.currentTile.faceLeft;
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
                        //  GameObject[] newTileList = setTiles();
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

        if (!checkGridViable())
        {
            beginEpisode();
        }
        if (checkGridFill())
        {
            createGridList();
            rewardConstraints();
            SaveGrid();
            beginEpisode();
        }
        else
        {
            StartCoroutine(checkEntropyNoAgent());
        }
    }

    Dictionary<int, GameObject> checkTiles()
    {
        Dictionary<int, GameObject> tempTiles = new Dictionary<int, GameObject>();
        foreach (KeyValuePair<int, GameObject> entry in possibleTiles)
        {
            tempTiles.Add(entry.Key, entry.Value);
        }
        for (int i = 0; i < possibleTiles.Count; i++)
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

    public bool checkGridViable()
    {
        foreach (GameObject cell in gridComps)
        {
            ShipCell shipCell = cell.GetComponent<ShipCell>();

            if (shipCell.possibleTiles.Count == 0) return false;
        }
        return true;
    }

    public bool checkGridFill()
    {
        foreach (GameObject cell in gridComps)
        {
            ShipCell shipCell = cell.GetComponent<ShipCell>();

            if (shipCell.collapsed == false) return false;
        }
        return true;
    }

    private void createGridList()
    {
        foreach(GameObject cell in gridComps)
        {
            ShipTile shipTile = cell.GetComponent<ShipCell>().currentTile;
            int i = getTileKey(shipTile);
            currentGrid.Add(i);
        }
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

    private void rewardConstraints()
    {
        gridChecker.initializeCheck();
        Allshapes = true;

        if (!gridChecker.GridCheck(gridChecker.Nacelle, 2))
        {
            Allshapes = false;
            nacelle = false;
        }

        if (!gridChecker.GridCheck(gridChecker.Saucer, 1))
        {
            Allshapes = false;
            saucer = false;
        }

        if (!gridChecker.GridCheck(gridChecker.StarDrive, 1))
        {
            Allshapes = false;
            stardrive = false;
        }
       
    }



    private void SaveGrid()
    {
        List<int> gridToSave = new List<int>();
        string hasNacelle = "";
        string hasSaucer = "";
        string hasStardrive = "";
        foreach(int val in currentGrid)
        {
            gridToSave.Add(val);
        }
        if (nacelle) hasNacelle = "Nacelle";
        if (saucer) hasSaucer = "Saucer";
        if (nacelle) hasStardrive = "Stardrive";
        FinishedGrids.Add("Grid" + episodeCOunt + Allshapes + hasNacelle + hasSaucer + hasStardrive, gridToSave);

    }

    private void EvaluateGrids()
    {
        int validGrids = 0;
        int nacelleMean = 0;
        int saucerMean = 0;
        int stardriveMean = 0;
        float totalVariationMean = 0;
        float variation = 0;
        foreach (KeyValuePair<string, List<int>> entry in FinishedGrids)
        {
            if (entry.Key.Contains("true")) validGrids++;
            if (entry.Key.Contains("Nacelle")) nacelleMean++;
            if (entry.Key.Contains("Saucer")) saucerMean++;
            if(entry.Key.Contains("Stardrive")) stardriveMean++;
            variation = variationCheck(entry.Value, entry.Key);
            totalVariationMean += variation;
        }
        Debug.Log("Total Finished Grid Percentage = " + FinishedGrids.Count );
        if( FinishedGrids.Count > 0)
        {
            Debug.Log("Total Valid Grid Percentage = " + validGrids / FinishedGrids.Count);
            Debug.Log("Total Nacelle Percentage = " + nacelleMean / FinishedGrids.Count);
            Debug.Log("Total Saucer Percentage = " + saucerMean / FinishedGrids.Count);
            Debug.Log("Total Stardrice Percentage = " + stardriveMean / FinishedGrids.Count);
            Debug.Log("Average Grid Variance = " + totalVariationMean / FinishedGrids.Count);

        }
        else
        {
            Debug.Log("Total Valid Grid Percentage = " + FinishedGrids.Count);
            Debug.Log("Total Nacelle Percentage = " + FinishedGrids.Count);
            Debug.Log("Total Saucer Percentage = " + FinishedGrids.Count);
            Debug.Log("Total Stardrice Percentage = " + FinishedGrids.Count);
            Debug.Log("Average Grid Variance = " + FinishedGrids.Count);
        }
            
    }

    private float variationCheck(List<int> mainGrid, string key)
    {
        float variationPercentage = 0;
        foreach(KeyValuePair<string,List<int>> entry in FinishedGrids)
        {
            int differences = 0;
            if (entry.Key == key) continue;
            for(int i = 0; i < mainGrid.Count; i++)
            {
                if (mainGrid[i] != entry.Value[i]) differences++;
            }

            variationPercentage += (float)differences / (float)mainGrid.Count;
        }
        if (FinishedGrids.Count > 0)
        {
            return variationPercentage / FinishedGrids.Count;
        }
        else return -1f;
    }

}
