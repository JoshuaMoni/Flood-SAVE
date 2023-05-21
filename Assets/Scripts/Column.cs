using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/** 
<summary>
Class <c>Column</c> represents one column for the water simulation.
</summary>
**/
public class Column
{
    public Vector3 coords; // Unity scene coordinates of the column. Y is the terrain height below the column.
    public List<ColumnPipe> pipes; // List of pipes for which this column is either a source or sink
    public float drainRate; // The drainrate of the column
    public float waterVolume; // The volume of water in the column
    public float width; // The width of the column
    public float rainfall; // The rainfall for the column. Needed if rain is not constant over all columns

    public float soilPercentage; 
    public float drainCount;
    public float soilCapacity; 
    public float drainCapactiy; 
    public float maxDrainCapacity; // Keeps track of the maximum amount of man made drainage for the cell
    public float waterCount;
        
    public Column(float x, float y, float z, float dRate, float startingWaterVolume, float width, float soilPer, float dCount, float water, float soilCap, float drainCap)
    {
        this.coords = new Vector3(x, y, z);
        this.pipes = new List<ColumnPipe>();
        this.drainRate = dRate;
        this.waterVolume = startingWaterVolume;
        this.width = width;
        this.soilPercentage = soilPer; 
        this.drainCount = dCount;
        this.soilCapacity = soilCap; 
        this.drainCapactiy = drainCap;
        this.waterCount = water;
        this.maxDrainCapacity = drainCap; 
    }

    /** <summary>
     * Other classes can call this to safely change the volume of water in this column.
     * </summary>
     */
    public void ApplyWaterFlow(float flowVolume)
    {
        waterVolume += flowVolume;
        if (waterVolume < 0)
        {
            waterVolume = 0;
        }
    }

    public void AddPipe(ColumnPipe pipe)
    {
        pipes.Add(pipe);
    }

    /** <summary>
     * X and Z unity scene coordinates of this column.
     * </summary>
     */
    public Vector2 GridCoord
    {
        get
        {
            return new Vector2(coords.x, coords.z);
        }
    }

    /** <summary>
     * X and Z unity scene coordinates of the "last" corner of this columm.
     * </summary>
     */
    public Vector2 End
    {
        get
        {
            return GridCoord + Vector2.one * width;
        }
    }

    /**
     * <summary>
     * Depth of water in the column, in metres.
     * </summary>
     */
    public float WaterDepth
    {
        get
        {
            return waterVolume / (width * width);
        }
    }

    /**
     * <summary>
     * Height of the water surface for this column above the unity scene origin, in metres.
     * </summary>
     */
    public float WaterHeight
    {   
        get
        {
            return WaterDepth + coords.y;
        }
    }
}
