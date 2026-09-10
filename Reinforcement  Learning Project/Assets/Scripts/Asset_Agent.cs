using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;
using System;
using System.IO;
using NUnit.Framework;
using System.Collections.Generic;

public class Asset_Agent : Agent
{
    public Asset_Generator grid;
    public GridChecker gridChecker;
    ShipCell cellToCollapse;
    bool generating = true;
    bool Allshapes = true;
    int[] allTiles;
    bool completedGrid = false;
    List<int> currentGrid = new List<int>();
    List<int> previousGrid = new List<int>();

    public override void OnEpisodeBegin()
    {   //sets all values back to base
        base.OnEpisodeBegin();
        completedGrid = false;
        grid.resetGrid();
        allTiles = grid.possibleTiles.Keys.ToArray();
        Allshapes = true;
        currentGrid.Clear();

    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //if the grid is not finished it will grab the lowest entropy cell and begin drawing the grid section
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
        foreach(int val in currentGrid)
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
            AddReward(-7f);
            EndEpisode();
            return;
        }
       
        if (grid.checkGridFill())
        {
            AddReward(1f);//used for high reward based agent
            createGridList();
            //Calculates and rewards variety in completed grids
            AddReward(calcTileEntropy() * 0.15f);
            AddReward(1.25f * checkGridRepeat());
            //sets grid completed true to stop an observation error
            completedGrid = true;
            rewardConstraints();       
            //stores the current grid as the previous most successful grid
            setPreviousGrid();
            EndEpisode();
            return;
        }
        AddReward(0.02f);//used for high reward agent

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
                    if ((x - gridDimensions / 2) == 0 && (y - gridDimensions / 2) == 0 && (z - gridDimensions / 2) == 0) sensor.AddObservation(1f);//checks if center of the grid
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
    //use this reward function for training high reward agent
    private void rewardConstraints()
    {
        gridChecker.initializeCheck();
        Allshapes = true;

        if (gridChecker.GridCheck(gridChecker.Nacelle, 2))
        {
            AddReward(2f);
        }
        else
        {
            Allshapes = false;
            AddReward(-0.5f);
        }

        if (gridChecker.GridCheck(gridChecker.Saucer, 1))
        {
             AddReward(2f);
        }
        else
        {
            Allshapes = false;
            AddReward(-0.5f);
        }

        if (gridChecker.GridCheck(gridChecker.StarDrive, 1))
        {
            AddReward(2f);
        }
        else
        {
            Allshapes = false;
            AddReward(-0.5f);
        }
        if (Allshapes)
        {
            AddReward(5f);
        }
    }

    //use this reward function for training low reward agent
    private void rewardConstraintsLowReward()
    {
        gridChecker.initializeCheck();
        Allshapes = true;

        if (!gridChecker.GridCheck(gridChecker.Nacelle, 2))
        {
            Allshapes = false;

        }

        if (!gridChecker.GridCheck(gridChecker.Saucer, 1))
        {
            Allshapes = false;
        }

        if (!gridChecker.GridCheck(gridChecker.StarDrive, 1))
        {
            Allshapes = false;
            AddReward(-0.1f);
        }
        if (Allshapes)
        {
            AddReward(2f);
        }
    }

    //calculates the shannon entropy of the grid to give a reward value proportial to the entropy value
    private float calcTileEntropy()
    {
        float entropy = 0f;

        if (currentGrid.Count == 0) return 0f;

        Dictionary<int, int> tileCount= new Dictionary<int, int>();

        foreach(int tile in currentGrid)
        {
            if (tileCount.ContainsKey(tile)) tileCount[tile]++;

            else tileCount[tile] = 1;
        }

        foreach (int count in tileCount.Values)
        {

            float probability = (float)count / currentGrid.Count;
            if (probability == 0) Debug.Log("0 PROBLEM WITH VAL "+ currentGrid.Count);
            entropy -= probability * Mathf.Log(probability);
        }

        return entropy;
    }

    //stores the successful grid in a list to be check
    private void createGridList()
    {

        foreach(GameObject cell in grid.gridComps)
        {
            ShipTile tile = cell.GetComponent<ShipCell>().currentTile;
            int i = grid.getTileKey(tile);
          
            currentGrid.Add(i);
 
        }
    }
    //compares the current grid a the previous successful grid and compares diferences
    private float checkGridRepeat()
    {
        if (previousGrid.Count != currentGrid.Count) return 1f;//the first time the check runs
        
        int differences = 0;
        for(int i = 0; i < previousGrid.Count; i++)
        {
            if (previousGrid[i] != currentGrid[i])
            {
                differences++;
            }
        }

        return differences / currentGrid.Count; //The more differences the more reward
    }
}