using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.GraphView;

public class GridChecker : MonoBehaviour
{
    public Asset_Generator grid;
    int grid_height;
    int grid_width;
    int grid_length;
    List<int> indexSkip = new List<int>();
    List<ShipCell> grid_Cells = new List<ShipCell>();
    //array's of valid section tiles
    public ShipTile[] Nacelle_Parts;
    public ShipTile[] Saucer_Parts;
    public ShipTile[] StarDrive_Parts;

    public shipCriteria Nacelle; 
    public shipCriteria Saucer;
    public shipCriteria StarDrive;

 

    //creates all the object classes to be searched for and stores the cells in an array 
    public void initializeCheck()
    {

        grid_height = grid.Height;
        grid_width = grid.Width;
        grid_length = grid.Length;
        Nacelle = new shipCriteria(3, 4, 2, 3, 2, 3, Nacelle_Parts);
        Saucer = new shipCriteria(2, 6, 2, 14, 3, 5, Saucer_Parts);
        StarDrive = new shipCriteria(3, 10, 2, 8, 4, 6, StarDrive_Parts);

        
        foreach (GameObject cell in grid.gridComps)
        {
            grid_Cells.Add(cell.GetComponent<ShipCell>());
        }
    }

    public bool GridCheck(shipCriteria section, int requiredNumber)
    {
        
        indexSkip.Clear();
        bool match = false;
        int numOfSection = 0;

        for (int y = 0; y < grid_height; y++)
        {
            for(int x = 0; x < grid_width; x++)
            {
                for (int z = 0; z < grid_length; z++)
                {
                    int index = z + (x * grid_length) + ((grid_length * grid_width) * y);
                    //will skip checking that cell if it was checked in a previous search
                    if (indexSkip.Contains(index))
                    {
                        continue;
                    }
                    ShipCell currentCell = grid_Cells[index];
                    if (currentCell.collapsed) continue;
                    if (section.allocated_parts[0].TileName== currentCell.currentTile.TileName)
                    {
                        int[] dimensions = drawShape(Nacelle, index);
                        match = checkShape(Nacelle, index, dimensions[0], dimensions[1], dimensions[2]);
                        if (match)
                        {
                            numOfSection++;
                        }
                    }
                }
            }
        }

        if (numOfSection == requiredNumber) return true;
        else return false;
    }

   
    //from the found starting cell it draws the outline of the shape
    int[] drawShape(shipCriteria shipPart, int index)
    {
        int[] dimensions = new int[] { };
        bool findingLength = true;
        int length = 0;
        bool findingWidth = true;
        int width = 0;
        bool findingHeight = true;
        int height = 0;
        int i = 0;

        while (findingLength)
        {
            
            ShipCell Cell = grid_Cells[index + i].GetComponent<ShipCell>();
            if (validTile(Cell, shipPart.allocated_parts))
            {
                length++;
                i++;
            }
            else findingLength = false;
        }
        i = 0;
        dimensions[0] = length;
        while (findingWidth)
        {
            ShipCell Cell = grid_Cells[index +(grid_length* i)].GetComponent<ShipCell>();
            if (validTile(Cell, shipPart.allocated_parts))
            {
                width++;
                i++;
            }
            else findingWidth = false;
        }
        dimensions[1] = width;
        while (findingHeight)
        {
            ShipCell Cell = grid_Cells[index + ((grid_length*grid_width) * i)].GetComponent<ShipCell>();
            if (validTile(Cell, shipPart.allocated_parts))
            {
                height++;
                i++;
            }
            else findingHeight = false;
        }
        dimensions[2] = height;
        //stores the saved info into an array that it returns
        return dimensions;
    }

    //checks through the grid section that has been drawn and will look to see if a tile is not included in the valid tiles list
    bool checkShape(shipCriteria parts, int index, int Length, int Width, int Height)
    {
        int shapeIndex = 0;
        bool match = true;
        if (Enumerable.Range(parts.min_length, parts.max_length).Contains(Length) && Enumerable.Range(parts.min_width, parts.max_width).Contains(Width) && Enumerable.Range(parts.min_height, parts.max_height).Contains(Height))
        {
            for (int y = 0; y < Height; y++)
            {
                if (!match) break;
                for (int x = 0; x < Length; x++)
                {
                    if (!match) break;
                    for (int z = 0; z < Width; z++)
                    {
                        shapeIndex = index + (x) + (y * (grid_length * grid_width)) + (z * grid_length);
                        ShipCell Cell = grid_Cells[shapeIndex].GetComponent<ShipCell>();
                        if (validTile(Cell, parts.allocated_parts) == false)
                        {

                            match = false;
                            indexSkip.Clear();
                            break;
                        }
                        else indexSkip.Add(shapeIndex);
                    }
                }
            }
            return match;
        }
        else return false;
    }


    //checks tiles validity
    bool validTile(ShipCell cell, ShipTile[] parts)
    {
        bool match = false;

        foreach (ShipTile part in parts)
        { 
            if(cell.currentTile.TileName == part.TileName) match = true;
        }


        return match;
    }
}

//ship critea class
public class shipCriteria
{
    public int min_length { get;}
    public int max_length { get;}
    public int min_height { get;}
    public int max_height { get; }
    public int min_width { get; }
    public int max_width { get; }
    public ShipTile[] allocated_parts { get; set; } 

    public shipCriteria(int min_length, int max_length, int min_height, int max_height, int min_width, int max_width, ShipTile[] allocated_parts)
    {
        this.min_length = min_length;
        this.max_length = max_length;
        this.min_height = min_height;
        this.max_height = max_height;
        this.min_width = min_width;
        this.max_width = max_width;
        this.allocated_parts = allocated_parts;
    }
}


