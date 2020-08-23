﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridFinder : MonoBehaviour
{
    public static GridFinder instance;

    // TEMP POINTS USED BY THE GIZMOS THINGO
    public int rad = 2; // The distance from the origin in hexes
    public Vector2Int origin = Vector2Int.zero; // the origin position
    public bool allowSkips = false; // bool for if we want to be able to find the hexes on the other side of a gap or not
   
    private Dictionary<Vector2Int, Hex> currentSpawnedMap; // the dictonary that is updated by the mapSpawner that the grid finder script uses to find its neighbours
    private Vector3 currentMapOffset;
    private Quaternion currentMapRotation;
    

  [SerializeField]  private HexagonGrid grid = null; // refrence to the grid 


    public Vector2Int WorldToGridPoint(Vector3 position)
    {
       // position -= currentMapOffset; // need to move the map with its holders offset

        //This was disabled, as it appeared to counteract the grid scripts conversion methods
        //position =  Quaternion.Inverse( currentMapRotation) *  position; // need to allign the rotation
      
        Debug.DrawRay(position, Vector3.up * 4, Color.red);
        return new Vector2Int(grid.WorldToCell(position).x, grid.WorldToCell(position).y);

       

    }


    public Vector2Int WorldToGridPoint(Hex hex)
    {
        return WorldToGridPoint(hex.transform.position);
    }

    public Vector2Int WorldToGridPoint(GameObject objectInGrid)
    {
        return WorldToGridPoint(objectInGrid.transform.position);
    }

    public Vector3? GridPosToWorld(int x, int y)
    {
        //Vector3? worldPos = grid.CellToWorld(new Vector2Int(x, y)).Value;
        //if (worldPos.HasValue)
            return grid.CellToWorld(new Vector2Int(x, y)).Value;
        //else return new Vector3(0, 0, 0);
    }

    public Vector3? GridPosToWorld(Vector2Int position)
    {
        return GridPosToWorld(position.x, position.y);
    }




    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }


        if (grid == null) grid = MapSpawner.Instance.GetCurrentGrid();

        
    }

    //private void Update()
    //{
    //    Debug.DrawRay(currentMapOffset, Vector3.right, Color.blue);
    //}


    // Sets up the current spawned map dictionary... Function is called in the Map Spawner when it spawns a new map.
    public void SetMap(Dictionary<Vector2Int, Hex> newMap, Vector3 mapOffset, Quaternion mapRotation)
    {
        currentSpawnedMap = newMap;
        this.currentMapOffset = mapOffset;
        this.currentMapRotation = mapRotation;
    }

    
    // Gets all of the neighbour positions then turns them into hexes using the dictonary.  
    public Hex[] GetAllNeighbourHexs(Vector2Int origin, float radius, bool moveOverGaps = false)
    {
        
        List<Hex> neighbourHexs = new List<Hex>();
        Vector2Int[] targetPoints = GetAllNeighbourPoints(origin, radius, moveOverGaps);

        foreach (Vector2Int point in targetPoints)
        {
            //if (point != origin)
            if (point != origin && currentSpawnedMap.ContainsKey(point))
            {
                if (currentSpawnedMap[point].readyState != Hex.HexReadyState.destroyed) // make sure it is alive first
                    neighbourHexs.Add(currentSpawnedMap[point]);
            }

        }

        return neighbourHexs.ToArray();

    }


    // Gets all of the neighbour positions
    public Vector2Int[] GetAllNeighbourPoints(Vector2Int origin, float radius, bool moveOverGaps = false)
    {

        List<Vector2Int> nFoundPoints = new List<Vector2Int>();
        List<Vector2Int> newPoints = new List<Vector2Int>();

        // Add the origin Hex
        nFoundPoints.Add(origin);
        newPoints.Add(origin); 


        for (int i = 0; i <= radius; i++)
        {
            List<Vector2Int> newNewPoints = new List<Vector2Int>();
            List<Vector2Int> oldNewPoints = new List<Vector2Int>();

            foreach (Vector2Int newPoint in newPoints)
            {
                oldNewPoints.Add(newPoint);


                foreach (Vector2Int point in GetSurroundingNeighbourPoints(newPoint, moveOverGaps))
                {
                    newNewPoints.Add(point);

                   // Vector3 posA = GridPosToWorld(point.x, point.y);
                   // Debug.DrawLine(GridPosToWorld(point.x, point.y), (GridPosToWorld(newPoint.x + point.x, newPoint.y)));
                }

            }



            foreach (Vector2Int point in oldNewPoints)
            {
                newPoints.Remove(point);
                nFoundPoints.Add(point);

            }

            foreach (Vector2Int point in newNewPoints)
            {
                if (!newPoints.Contains(point))
                {
                    newPoints.Add(point);
                }

            }

        }


        return nFoundPoints.ToArray();
    }

    // Looks for the 6 positions that surround a point... If returnEmptyPositions hexes is false it wont retun empty positions, dah.
    //private Vector2Int[] GetSurroundingNeighbourPointsOLD(Vector2Int origin, bool returnEmptyPositions = false)
    //{
    //    List<Vector2Int> neighbourPositions = new List<Vector2Int>();

    //    for (int x = -1; x <= 1; x++)
    //    {
    //        for (int y = -1; y <= 1; y++)
    //        {
    //            bool allow = false;

    //            // when y is even

    //            if (origin.y % 2 == 0)
    //            {

    //              if (
    //              (x == 0 && y == 1) ||
    //              (x == 1 && y == 0) ||
    //              (x == 0 && y == -1) ||
    //              (x == -1 && y == -1) ||
    //              (x == -1 && y == 0) ||
    //              (x == -1 && y == 1)
    //              )
    //                {
    //                    allow = true;
    //                }
    //            }
    //            else
    //            {

    //             if ( 
    //            (x == 0 && y == -1) ||
    //            (x == 0 && y == 1) ||
    //             (x == 1 && y == -1) ||
    //             (x == 1 && y == 1) ||
    //              (x == 1 && y == 0) ||
    //             (x == -1 && y == 0)
    //              )
    //                {
    //                    allow = true;
    //                }
    //            }



    //            if (allow)
    //            {
    //                Vector2Int newNeighbour = new Vector2Int(x + origin.x, y + origin.y);

    //                if (returnEmptyPositions)
    //                {
    //                    neighbourPositions.Add(newNeighbour);
    //                }
    //                else
    //                {
    //                    if (currentSpawnedMap.ContainsKey(newNeighbour))
    //                    {
    //                        neighbourPositions.Add(newNeighbour);
    //                    }
    //                }

    //            }
    //        }
    //    }

    //    return neighbourPositions.ToArray();
    //}


    private Vector2Int[] GetSurroundingNeighbourPoints(Vector2Int origin, bool returnEmptyPositions = false)
    {

        List<Vector2Int> neighbourPositions = new List<Vector2Int>();


        Vector2Int newNeighbour;

        Vector2Int[] neighbourCoords = new Vector2Int[6] {
            new Vector2Int(origin.x, origin.y + 1),
            new Vector2Int(origin.x + 1, origin.y + 1), 

            new Vector2Int(origin.x - 1, origin.y),
            new Vector2Int(origin.x + 1, origin.y),

            new Vector2Int(origin.x - 1, origin.y - 1),
            new Vector2Int(origin.x, origin.y - 1)

        };

        for (int i = 0; i < neighbourCoords.Length; i++)
        {
            // newNeighbour = new Vector2Int(x + origin.x, y + origin.y);

            newNeighbour = neighbourCoords[i];

            if (returnEmptyPositions)
            {
                neighbourPositions.Add(newNeighbour);
            }
            else
            {
                if (currentSpawnedMap.ContainsKey(newNeighbour))
                {
                    neighbourPositions.Add(newNeighbour);
                }
            }
        }

        

        return neighbourPositions.ToArray();
    }

    // Turns a vector 2 int into a hex... retuns null if a hex cannot be found at a given point
    public Hex GetHexAtPoint(Vector2Int hexPos)
    {
        if (currentSpawnedMap == null || !currentSpawnedMap.ContainsKey(hexPos))
        {

            return null;
        }
        else
        {
            return currentSpawnedMap[hexPos];
        }
    }

    // Just a lazy overload :P
    private Hex GetHexAtPoint(int xPos, int yPos)
    {

        return GetHexAtPoint(new Vector2Int(xPos, yPos));
    }

    public Vector2Int MouseToGridPoint()
    {
        Vector3 position = Input.mousePosition;

        Plane plane = new Plane(Vector3.up, Vector3.zero + Vector3.up * currentMapOffset.y);
        Ray mouseRay = Camera.main.ScreenPointToRay(position);


        float distance;

        if (plane.Raycast(mouseRay, out distance))   // outs the required distance
        {



            Vector3 mousePlanePoint = mouseRay.GetPoint(distance);

            if (currentMapRotation.y % 30 == 0)
            {

            }


            // Debug.Log("YEET" + WorldToGridPoint(mousePlanePoint, false));


            return WorldToGridPoint(mousePlanePoint);


        }


        return Vector2Int.zero;
    }




    //private void OnDrawGizmos()
    //{

    //    if (currentSpawnedMap != null && currentSpawnedMap.Keys.Count > 0)
    //    {
    //        Gizmos.color = Color.green;


    //        rad = 1;
    //        Hex[] testNeighbours = GetAllNeighbourHexs(origin, rad, allowSkips);
    //        //Debug.Log("testNeighbours = " + testNeighbours.Length);
    //        foreach (Hex hex in testNeighbours)
    //        {

    //           Gizmos.DrawSphere(hex.transform.position, 0.5f);
    //        }
    //    }

    //}

    private void OnGUI()
    {
        //foreach()
           // Dictionary<Vector2Int, Hex> currentSpawnedMap;

                   // GUI.Label(new Rect(0, 0, 200, 200), "Calculated x pos = :");
        //        GUI.Label(new Rect(0, 10, 200, 200), calculatedXPos.ToString());
    }
}