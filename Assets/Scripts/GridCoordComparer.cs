using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/** <summary>
* Sort by X coord, then y coord.
* </summary>
*/
public class GridCoordComparer : Comparer<Vector2>
{
    /** <summary>
     * Sort by X coord, then y coord.
     * </summary>
     */
    public override int Compare(Vector2 x, Vector2 y)
    {
        if (x.Equals(y))
        {
            return 0;
        }

        if (x.x != y.x)
        {
            return (int)(x.x - y.x);
        }

        return (int)(x.y - y.y);
    }
}

/**<summary>
* If either x or y are less than the second vector's x or y, the first vector is considered smaller.
* </summary>
*/
public class Vector2ORComparer : Comparer<Vector2>
{
    /**<summary>
     * If either x or y are less than the second vector's x or y, the first vector is considered smaller.
     * </summary>
     */
    public override int Compare(Vector2 x, Vector2 y)
    {
        if (x.Equals(y))
        {
            return 1;
        }

        if (x.x < y.x)
        {
            return -1;
        }

        if (x.x == y.x && x.y < y.y)
        {
            return -1;
        }

        return 1;
    }
}

/**
* If both x and y are less than the second vector's x and y, the first vector is considered smaller.
*/
public class Vector2ANDComparer : Comparer<Vector2>
{
    /**<summary>
    * If both x and y are less than the second vector's x and y, the first vector is considered smaller.
    * </summary>
    */
    public override int Compare(Vector2 x, Vector2 y)
    {
        if (x.Equals(y))
        {
            return 0;
        }

        if (x.x <= y.x && x.y <= y.y)
        {
            return -1;
        }

        return 1;
    }
}

