using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using System.Linq;
using System;
using System.IO;

public class Asset_Agent_Secondary : Agent
{
    public Asset_Generator grid;
    public GridChecker gridChecker;
    ShipCell cellToCollapse;
    bool generating = true;
    bool Allshapes = true;
    int completeGrids = 0;
    int[] allTiles;
    bool completedGrid = false;

    float[,,,] gridSectionTensor = new float[,,,] { };

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        completedGrid = false;
        grid.resetGrid();
        allTiles = grid.possibleTiles.Keys.ToArray();
        Allshapes = true;

    }

    public override void CollectObservations(VectorSensor sensor)
    {

        if (completedGrid)
        {
 
            return;
        }
        cellToCollapse = getLowestEntropy();
        //  observeGridSection(sensor, cellToCollapse);
        Vector3Int pos = new Vector3Int(cellToCollapse.X, cellToCollapse.Y, cellToCollapse.Z);
        CellObservation(sensor, cellToCollapse);
        NeighbourObservation(sensor, pos);
       
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

        if (!cellCollapse.possibleTiles.ContainsKey(tileIndex))
        {
            AddReward(-0.5f);

            return;
        }

        cellCollapse.agentCollapseCell(tileIndex);

        
        grid.UpdateGeneration();

        if (grid.checkGridViable() == false)
        {
            AddReward(-5f);
           // Debug.LogWarning("FAILED GEN");
            EndEpisode();
            return;
        }
       
        if (grid.checkGridFill())
        {
            //AddReward(1f);
            completedGrid = true;
            rewardConstraints();
            generating = false;
            EndEpisode();
            return;
        }

       // AddReward(0.01f);
    }

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
                    sensor.AddObservation(0f);
                }
            }
        }
    }
    public override void WriteDiscreteActionMask(IDiscreteActionMask actionMask)
    {
        ShipCell cellCollapse = getLowestEntropy();

        Vector3Int position = new Vector3Int(cellCollapse.X, cellCollapse.Y, cellCollapse.Z);

        for (int i = 0; i < allTiles.Length; i++)
        {
            if (!cellCollapse.possibleTiles.ContainsKey(i))
            {
                actionMask.SetActionEnabled(0, i, false);
            }
        }
    }

    private void rewardConstraints()
    {
        gridChecker.initializeCheck();
        Allshapes = true;

        if (gridChecker.GridCheck(gridChecker.Nacelle, 2))
        {
          //  AddReward(1f);
            Debug.Log("NACELLE FOUND");
        }
        else
        {
            Allshapes = false;
            //AddReward(-1f);
        }

        if (gridChecker.GridCheck(gridChecker.Saucer, 1))
        {
            //  AddReward(1f);
            Debug.Log("SAUCER FOUND");
        }
        else
        {
            Allshapes = false;
           // AddReward(-1f);
        }

        if (gridChecker.GridCheck(gridChecker.StarDrive, 1))
        {
           // AddReward(1f);
            Debug.Log("STARDRIVE FOUND");
        }
        else
        {
            Allshapes = false;
           // AddReward(-1f);
        }
        if (Allshapes)
        {
            AddReward(5f);
            Debug.Log("ALL SHAPES");
        }
        else Debug.Log("NO SHAPES");
    }
    //Old observations code
    private void CellObservation(VectorSensor sensor, ShipCell cell)
    {
        float entropy = cell.possibleTiles.Count / grid.possibleTiles.Count;

        sensor.AddObservation(entropy);

        TileObservation(sensor, cell);
    }

    private void NeighbourObservation(VectorSensor sensor, Vector3Int position)
    {
        int x = position.x;
        int y = position.y;
        int z = position.z;


        AddNeighbour(sensor, grid.getCell(x + 1, y, z));
        AddNeighbour(sensor, grid.getCell(x - 1, y, z));
        AddNeighbour(sensor, grid.getCell(x, y + 1, z));
        AddNeighbour(sensor, grid.getCell(x, y - 1, z));
        AddNeighbour(sensor, grid.getCell(x, y, z + 1));
        AddNeighbour(sensor, grid.getCell(x, y, z - 1));
    }
    private void TileObservation(VectorSensor sensor, ShipCell cell)
    {
        for (int tile = 0; tile < allTiles.Length; tile++)
        {
            sensor.AddObservation(cell.possibleTiles.ContainsKey(tile) ? 1f : 0f);
        }
    }



    private void AddNeighbour(VectorSensor sensor, ShipCell neighbour)
    {
        if (neighbour == null)
        {
            sensor.AddObservation(0f);

            for (int tile = 0; tile < allTiles.Length; tile++)
            {
                sensor.AddObservation(0f);
            }

            return;
        }

        sensor.AddObservation(1f);

        for (int tile = 0; tile < allTiles.Length; tile++)
        {
            sensor.AddObservation(neighbour.possibleTiles.ContainsKey(tile) ? 1f : 0f);
        }
    }

    private void saveWorkingGrid()
    {
        string fileName = "GoodGrid" + completeGrids;
        if (File.Exists(fileName)){
            return;
        }
        var sr = File.CreateText(fileName);
        foreach(GameObject cell in grid.gridComps)
        {
            ShipCell shipCell = cell.GetComponent<ShipCell>();
            sr.WriteLine("(" + shipCell.X+","+ shipCell.Y+","+ shipCell.Z+") Tile = "+shipCell.currentTile);
        }
        sr.Close();
    }
}