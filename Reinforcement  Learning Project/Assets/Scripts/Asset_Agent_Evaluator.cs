using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;
using System;
using System.IO;
using NUnit.Framework;
using System.Collections.Generic;


public class Asset_Agent_Evaluator : Agent
{
    public Asset_Generator grid;
    public GridChecker gridChecker;
    ShipCell cellToCollapse;
   
    bool generating = true;
   //checks for the RS/ship criteria is found
    bool Allshapes = true;
    bool nacelles = true;
    bool stardrive = true;
    bool saucer = true;

    int[] allTiles;
    bool completedGrid = false;
    List<int> currentGrid = new List<int>();
    List<int> previousGrid = new List<int>();
    //stores all completed grids into a dictionary to be evaluated after generating has finished
    Dictionary<string, List<int>> FinishedGrids = new Dictionary<string, List<int>>();
    int episodeCount = 0;
    int maxEpisodes = 500;
    float variationMax = 0;
    //key to find the grid list with the highest variaition
    string variationMaxKey = "";
    

    
    public override void OnEpisodeBegin()
    {   //sets all values back to base
        if (!generating) return;
        base.OnEpisodeBegin();
        completedGrid = false;
        grid.resetGrid();
        allTiles = grid.possibleTiles.Keys.ToArray();
        Allshapes = true;
        
        nacelles = true;
        stardrive = true;
        saucer = true;
        
        currentGrid.Clear();
        episodeCount++;
        Debug.Log(episodeCount);
        if (episodeCount > maxEpisodes)
        {
         if(generating)   EvaluateGrids();
            generating = false;
        }
      
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        if (completedGrid)
        {

            return;
        }
        cellToCollapse = getLowestEntropy();
        observeGridSection(sensor, cellToCollapse);

    }
    private void setPreviousGrid()
    {
        previousGrid.Clear();
        foreach (int val in currentGrid)
        {
            previousGrid.Add(val);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {


        if (!generating) return;

        int tileIndex = actions.DiscreteActions[0];

        ShipCell cellCollapse = getLowestEntropy();

        if (cellCollapse == null)
        {
            EndEpisode();
            return;
        }
        //negative reward if invalid key is chosen
        if (!cellCollapse.possibleTiles.ContainsKey(tileIndex))
        {
            AddReward(-0.5f);

            return;
        }
        //collpases the chosen cell then updates the grid
        cellCollapse.agentCollapseCell(tileIndex);


        grid.UpdateGeneration();



        if (grid.checkGridViable() == false)
        {
           
            EndEpisode();
            return;
        }

        if (grid.checkGridFill())
        {
            createGridList();
            //sets grid completed true to stop an observation error
            completedGrid = true;
            //checks the grid for RS/ship criteria for the key
            rewardConstraints();
            SaveGrid();
            setPreviousGrid();
            EndEpisode();
            return;
        }

    }

    //consistantly gets the cell with the lowest entropy to maximise consistancy throughout the decision process
    private ShipCell getLowestEntropy()
    {

        ShipCell chosenCell = null;

        int lowestEntropy = int.MaxValue;


        foreach (GameObject gridcell in grid.gridComps)
        {
            ShipCell cell = gridcell.GetComponent<ShipCell>();
            if (cell.collapsed) continue;
            if (cell.possibleTiles.Count < lowestEntropy)
            {
                chosenCell = cell;
                lowestEntropy = cell.possibleTiles.Count;
            }
        }

        return chosenCell;
    }



    private void observeGridSection(VectorSensor sensor, ShipCell cell)
    {
        int gridDimensions = 5; //Must be odd

        for (int x = 0; x < gridDimensions; x++)
        {

            for (int y = 0; y < gridDimensions; y++)
            {


                for (int z = 0; z < gridDimensions; z++)
                {
                    int gridX = cell.X + (x - gridDimensions / 2);
                    int gridY = cell.Y + (y - gridDimensions / 2);
                    int gridZ = cell.Z + (z - gridDimensions / 2);
                    ShipCell gridCell = grid.getCell(gridX, gridY, gridZ);
                    if (gridCell == null)
                    {
                        for (int tile = 0; tile < allTiles.Length; tile++)
                        {
                            sensor.AddObservation(0f);//each "possible tile" is false
                        }
                        sensor.AddObservation(0f); //is collapsed
                        sensor.AddObservation(x - gridDimensions / 2);//relative grid positions
                        sensor.AddObservation(y - gridDimensions / 2);
                        sensor.AddObservation(z - gridDimensions / 2);
                        sensor.AddObservation(0f);//if centre of grid
                        sensor.AddObservation(1f);//if outside of grid

                        continue;
                    }
                    for (int tile = 0; tile < allTiles.Length; tile++)
                    {

                        sensor.AddObservation(gridCell.possibleTiles.ContainsKey(tile) ? 1f : 0f);
                    }
                    sensor.AddObservation(gridCell.collapsed ? 1f : 0f);
                    sensor.AddObservation(x - gridDimensions / 2);//relative grid positions
                    sensor.AddObservation(y - gridDimensions / 2);
                    sensor.AddObservation(z - gridDimensions / 2);
                    if ((x - gridDimensions / 2) == 0 && (y - gridDimensions / 2) == 0 && (z - gridDimensions / 2) == 0) sensor.AddObservation(1f);
                    else sensor.AddObservation(0f);
                    sensor.AddObservation(0f);//states cell is not outside of grid
                }
            }
        }
    }
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        ShipCell cellCollapse = getLowestEntropy();

        Vector3Int position = new Vector3Int(cellCollapse.X, cellCollapse.Y, cellCollapse.Z);
        //masks the dictionary keys that are not valid so NN ignore it
        for (int i = 0; i < allTiles.Length; i++)
        {
            if (!cellCollapse.possibleTiles.ContainsKey(i))
            {
                actionMask.SetActionEnabled(0, i, false);
            }
        }
    }
    //uses the grid checker/ CRS for key purposes
    private void rewardConstraints()
    {
        gridChecker.initializeCheck();
        Allshapes = true;

        if (!gridChecker.GridCheck(gridChecker.Nacelle, 2))     
        {
            Allshapes = false;
            nacelles = false;
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
        if (Allshapes)
        {
            AddReward(5f);
        }
    }


    //stores the successful grid in a list to be check
    private void createGridList()
    {


        foreach (GameObject cell in grid.gridComps)
        {
            ShipTile tile = cell.GetComponent<ShipCell>().currentTile;
            int i = grid.getTileKey(tile);

            currentGrid.Add(i);

        }
    }
    //saves the grid to a global dictioanry with a key that contains info on what the grid has achieved
    private void SaveGrid()
    {
        List<int> gridToSave = new List<int>();
        string hasNacelle = "";
        string hasStardrive = "";
        string hasSaucer = "";
        foreach (int val in currentGrid)
        {
            gridToSave.Add(val);
        }
        if (nacelles) hasNacelle = "Nacelle";
        if (stardrive) hasStardrive = "Stardrive";
        if (saucer) hasSaucer = "Saucer";
        FinishedGrids.Add("Grid" + episodeCount + Allshapes+hasNacelle+hasStardrive+hasSaucer, gridToSave);

    }

    //iterates through all the saved grids 
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
            //parses through the key string for RS/Ship criteria outcomes
            if (entry.Key.Contains("true"))
            {
                validGrids++;
            }
            if (entry.Key.Contains("Nacelle")) nacelleMean++;
            if (entry.Key.Contains("Stardrive")) stardriveMean++;
            if (entry.Key.Contains("Saucer")) saucerMean++;
            //calculates the difference percentage for the completed grids
            variation = variationCheck(entry.Value, entry.Key);
            if (variation > variationMax)
            {
                variationMax = variation;
                variationMaxKey = entry.Key;
            }
            totalVariationMean += variation ;
        }
        Debug.Log("Total Finished Grid Percentage = " + FinishedGrids.Count);
        Debug.Log("Total Valid Grid Percentage = " + validGrids / FinishedGrids.Count);
        Debug.Log("Total Nacelle Percentage = " + nacelleMean / FinishedGrids.Count);
        Debug.Log("Total Stardrive Percentage = " + stardriveMean / FinishedGrids.Count);
        Debug.Log("Total Saucer Percentage = " + saucerMean / FinishedGrids.Count);
        Debug.Log("Average Grid Variance = " + totalVariationMean/ FinishedGrids.Count);
        Debug.Log("Highest Variation Percentage = " + variationMax);
    }


    //compares the current grid a the previous successful grid and compares diferences
    private float variationCheck(List<int> MainGrid, string key)
    {
        float variationPercentage = 0;
        foreach (KeyValuePair<string, List<int>> entry in FinishedGrids)
            {
                int differences = 0;
                if (entry.Key == key) continue;
                for (int i = 0; i < MainGrid.Count; i++)
                {
                    if (MainGrid[i] != entry.Value[i])
                    {
                        differences++;
                    }
                }
           
            variationPercentage += (float)differences / (float)MainGrid.Count;
            }
        
        return variationPercentage / FinishedGrids.Count;
        
    }

}
