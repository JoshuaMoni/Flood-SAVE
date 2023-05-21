using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** 
<summary>
Class <c>ColumnPipe</c> connects two <c>Column</c> instances together and does the majority of the calculations for water flow.
</summary>
**/
public class ColumnPipe
{
    // Columns connected by this pipe.
    // Arbitrary assignment, made distinct so we don't forget what a negative vs positive flow direction should do
    // It DOES NOT MATTER which column is which.
    public Column source;
    public Column sink;

    // The water flow velocity in the last timestep, in metres per second.
    private float previousFlowVelocity;

    public ColumnPipe(Column a, Column b)
    {
        source = a;
        sink = b;

        a.AddPipe(this);
        b.AddPipe(this);
    }

    /**
     * <summary>
     * Allow water to flow between the two columns connected by this pipe.
     * Water flow is determined by the length of time since the last flow.
     * </summary>
     */
    public void Flow(float deltaTime)
    {
        float flowVelocity = GetFlowVelocity(deltaTime);
        float flowVolume = deltaTime * CrossSectionalArea * flowVelocity;

        // Conservation of Mass (Don't flow more water than exists in the source column)
        if (flowVolume < 0)
        {
            flowVolume = Mathf.Max(-sink.waterVolume, flowVolume);
        }
        else
        {
            flowVolume = Mathf.Min(source.waterVolume, flowVolume);
        }

        // Bit of a hack, sets previous flow velocity to zero retroactively.
        // The source material took a different approach, just delaying flow for another timestep if conservation of mass would be broken,
        // rather than setting it to the largest it could be.
        // I think this is better, but their method didn't require this.
        if (CrossSectionalArea == 0)
        {
            previousFlowVelocity = 0;
        } else
        {
            previousFlowVelocity = flowVolume / (deltaTime * CrossSectionalArea);
        }

        source.ApplyWaterFlow(-flowVolume);
        sink.ApplyWaterFlow(flowVolume);
    }

    /**<summary>
     * Gets the velocity of water flow for the current timestep in metres cubed per second
     * </summary>
     */
    private float GetFlowVelocity(float deltaTime)
    {
        // Scale friction with the timestep to ensure energy loss is consistent.
        float friction = 1 - ((1 - ColumnManager.Instance.friction) * deltaTime / 0.005f);
        float flowVelocity = previousFlowVelocity * friction + deltaTime * FlowAcceleration;
        return flowVelocity;
    }

    /**<summary>
    * Gets the acceleration of water flow for the current timestep in metres cubed per second squared.
    * </summary>
    */
    private float FlowAcceleration
    {
        get
        {
            return ColumnManager.Instance.gravity * (source.WaterHeight - sink.WaterHeight) / PipeLength;
        }
    }

    /**<summary>
     * Cross-sectional area of the pipe, in metres. Width is the width of the smallest of the two columns, height is the lowest water height - highest terrain height.
     * </summary>
     */
    private float CrossSectionalArea
    {
        get
        {
            float width = Mathf.Min(sink.width, source.width);

            return width * Height;
        }
    }

    private float MaxSurfaceHeight
    {
        get
        {
            return Mathf.Max(source.WaterHeight, sink.WaterHeight);
        }
    }

    private float MaxGroundHeight
    {
        get
        {
            return Mathf.Max(source.coords.y, sink.coords.y);
        }
    }

    private float Height
    {
        get
        {
            return Mathf.Max(MaxSurfaceHeight - MaxGroundHeight, 0);
        }
    }

    /**
     * <summary>
     * Length of the pipe, in metres. Defined as the distance between two corresponding corners of the connected columns,
     * which is typically equivalent to the distance between the centres of the two columns.
     * Not always though, for pipes between columns of different resolutions this introduces a bit of error.
     * </summary>
     */
    private float PipeLength
    {
        get
        {
            // Horizontal distance between corners of each column
            Vector3 sourceCorner = new Vector3(source.coords.x, 0, source.coords.z);
            Vector3 sinkCorner = new Vector3(sink.coords.x, 0, sink.coords.z);

            return Vector3.Distance(sourceCorner, sinkCorner);
        }
    }

}
