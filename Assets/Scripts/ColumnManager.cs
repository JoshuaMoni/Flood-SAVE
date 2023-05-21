using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;
using Unity.VisualScripting;
using System;
using UnityEngine.UI;

/** 
 <summary>
Class <c>ColumnManager</c> handles all the water simulation. Including columns, pipes, water visualisation, weather data and drainage data.
 </summary>
**/
public class ColumnManager : MonoBehaviour
{
    [Header("GameObject Dependencies")]

    public GameObject terrain; // Terrain game object

    public GameObject drainStructure; // The prefab to use to render drainage infrastructure
    public GameObject cloudPrefab;
    public Text dateUI;

    public GameObject waterMeshesObject;
    private List<Mesh> waterMeshes = new List<Mesh>();

    private Terrain terrainComp; // terrain component of terrain gameobject
    private Dictionary<Vector2, Column> columnDict = new Dictionary<Vector2, Column>(); // Allow us to access columns by their X and Z unity scene coordinates in O(1) time.
    private List<Vector2> coords; // Sorted list of column coordinates
    private GridCoordComparer coordComparer = new GridCoordComparer(); // Column coordinate sorter
    private List<ColumnPipe> pipes = new List<ColumnPipe>(); // List of pipes, sorted by elevation.
    private SortedSet<float> sideLengths = new SortedSet<float>(); // Set of resolutions that are currently in use.
    private TerrainData tData; // Object containing all the grass, water, infrastructure data
    private WeatherData wData; // Object containing all the weather 
    private int weatherIndex; // Current index of the weather data, corresponds to which hour is being used.
    private float timer = 0f;
    private int hoursPassed = 0;
    private int intervalCount = 0; // This will flip when the interval of rain changes
    private bool currentlyRaining = true; // Changes the UI element
    

    [Header("Resolution")]
    public float _columnSideLength = 25f; // Low resolution column size, default value 25 metres

    public bool isMultiResolution = false; // Determine whether to render a high resolution area
    public float _detailedSideLength = 2.5f; // High resolution column size, default value 2.5
    public Vector2 detailedStart = new Vector2(5, 5); // Start area of high resolution, default (5, 5)
    public Vector2 detailedEnd = new Vector2(100, 100); // End area of high resolution, default (100, 100)

    [Header("UI")]
    public float riskThreshold = 0.01f;// Minimum water level to render water in the UI
    public float riskLevelDifference = 0.05f; // Depth difference between different risk levels, in metres.

    [Header("Physics")]
    public float friction = 0.9995f; // Friction of water, corresponds to energy lost, meant to model viscosity
    public float gravity = 9.806f; // Gravity

    [Header("Time")]
    public float timestep = 1f; // Multiplier for time, at 1, the simulation will move one hour per real life second. At 0.5 the simulation will move one hour in half a real life second.
                                // At 2 the simulation will move one hour in two real life seconds
    public int weatherStartTime = 825746400; // Unixtime for where to start in wData
    public float rainOverride = -1f;
    public float intervalsOfRain = 0f; // Change this this to change the time period that it rains for
    public float timeWithOutRain = 3f; // Used to control the amount of time with no rain
    

    [Header("Drainage")]
    public float waterDRate = 1000f; // Drain rate of water
    public float grassDRate = 0.6f; // Drain rate of grass
    public float drainDRate = 30f; // Drain rate of drain infrastructure
    public float drainCapacity = 100f; // Estimate of the capacity of a single drain 
    public float soilCapacity = 120000f; // Capacity of 10m^2 grid of soil  
    public float blockedDrainage = 1f; // Simulates drains being blocked, set to 1 for drains to have full capacity, set to 0 for 0 drainage

    private float fps = 0f;
    private Queue<float> frameTimes = new Queue<float>();
    private float totalFrameTime = 0f;

    private string sceneName; // Name of current scene i.e. Orakei, Penrose, CBD
    private DateTime dTime;

    /**
     * This class is a singleton, which is a common pattern in game design.
     * There should only be one instance of this class, and you can access it in a static way using this property.
     */
    private static ColumnManager _instance;

    public static ColumnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType<ColumnManager>();
            }

            return _instance;
        }
    }

    /** 
     <summary>
    Class <c>TerrainDataPoint</c> holds one data points information. This includes its coordinates and its type such as, water, grass, manhole.
    Variable names must be exactly as is for JSON importing to work.
     </summary>
    **/
    [System.Serializable]
    public class TerrainDataPoint
    {
        public float X;
        public float Y;
        public float value; 
        public string type;
    }

    /** 
     <summary>
    Class <c>TerrainData</c> holds a list of <c>TerrainDataPoint</c> instances. 
    Variable name must be exactly as is for JSON importing to work.
     </summary>
    **/
    [System.Serializable]
    public class TerrainData
    {
        public List<TerrainDataPoint> terrainData;
    }

    /** 
     <summary>
    Class <c>WeatherPoint</c> holds one weather points information. This includes its unixtime and its rainfall in millimeters. 
    Variable names must be exactly as is for JSON importing to work.
     </summary>
    **/
    [System.Serializable]
    public class WeatherPoint
    {
        public int time;
        public float rainfall;
    }

    /** 
    <summary>
    Class <c>WeatherData</c> holds a list of <c>WeatherPoint</c> instances. 
    Variable name must be exactly as is for JSON importing to work.
    </summary>
    **/
    [System.Serializable]
    public class WeatherData
    {
        public List<WeatherPoint> weatherData;
    }

    // Start is called before the first frame update
    void Start()
    {
        // Initialise water mesh and gather information from terrain
        foreach (Transform child in waterMeshesObject.transform)
        {
            Mesh waterMesh = new Mesh();
            child.gameObject.GetComponent<MeshFilter>().mesh = waterMesh;

            waterMeshes.Add(waterMesh);
        }

        terrainComp = terrain.GetComponent<Terrain>();

        // Get the size of the terrain object.
        int terrainX = (int)terrainComp.terrainData.size.x;
        int terrainZ = (int)terrainComp.terrainData.size.z;
        dTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        Vector2 end = new Vector2(terrainX, terrainZ);
        Vector2 start = new Vector2(0, 0);

        // Import infrastructure data for the current suburb
        sceneName = SceneManager.GetActiveScene().name.ToLower();
        string json = ReadJson("/Terraintypes/" + sceneName + "_unity.json");
        tData = JsonUtility.FromJson<TerrainData>(json);

        Debug.Log("Points of terrain data loaded: " + tData.terrainData.Count.ToString());
        Debug.Log("Example terrain data: " + tData.terrainData[1].X.ToString() + tData.terrainData[1].Y.ToString() + tData.terrainData[1].type);

        // Import weather data
        string weatherJson = ReadJson("/WeatherData/akl_processed.json");
        wData = JsonUtility.FromJson<WeatherData>(weatherJson);
        weatherIndex = wData.weatherData.FindIndex(x => x.time == weatherStartTime);
        DateTime currentTime = dTime.AddSeconds(wData.weatherData[weatherIndex].time);
        //dateUI.text = "Current Date: " + currentTime.ToString();

        Debug.Log("Points of weather data loaded: " + wData.weatherData.Count.ToString());

        // Create infrastructure and both water resolutions' water
        CreateInfrastructure();
        ConstructGrid(_columnSideLength, start, end); // Make initial low resolution grid.

        if (isMultiResolution)
        {
            ResizeColumns(_detailedSideLength, detailedStart, detailedEnd); // Make high resolution grid.
        }

        Debug.Log("Number of Columns Generated: " + columnDict.Keys.Count.ToString());
        Debug.Log("Number of Pipes Generated: " + pipes.Count.ToString());
    }

    /**
    <summary>
    Method <c>CreateInfrastructure</c> iterates over the drainage data and renders the <c>drainStructure</c> prefab. 
    Will only render the infrastructure types defined in <c>drawArray</c>.
    </summary>
    **/
    private void CreateInfrastructure()
    {
        string[] drawArray = new string[] {"manhole", "inout", "catch"};
        Dictionary<string, int[]> structAdjusts = new Dictionary<string, int[]>(){ {"orakei", new int[] {-45, -20 } }, { "cbd", new int[] {-24, -22 } }, { "penrose", new int[] {-45, -20 } } };
        foreach(TerrainDataPoint dataPoint in tData.terrainData)
        {
            if (sceneName == "cbd" && (dataPoint.type == "water" || dataPoint.type =="grass" ))
            {
                dataPoint.X -= 25;
                dataPoint.Y += 30;
            } else if (sceneName == "orakei" && (dataPoint.type =="water" || dataPoint.type == "grass"))
            {
                dataPoint.Y += 10;
            } else if (sceneName == "penrose" && (dataPoint.type == "water" || dataPoint.type == "grass"))
            {
                dataPoint.X -= 20;
            }

            if ("manhole inout catch".Contains(dataPoint.type))
            {
                dataPoint.X += structAdjusts[sceneName][0];
                dataPoint.Y += structAdjusts[sceneName][1];
            }

            if (drawArray.Contains(dataPoint.type))
            {
                Instantiate(drainStructure, new Vector3(dataPoint.X, terrainComp.SampleHeight(new Vector3(dataPoint.X, 0, dataPoint.Y)), dataPoint.Y), Quaternion.identity);
            }
        }
    }

    /**
     * Construct the pipes between a given column and its existing neighbours with the same resolution.
     */
    private void AddNeighbours(Column column)
    {
        for (int y = -1; y < 2; y++)
        {
            for (int x = -1; x < 2; x++)
            {
                float neighbourX = column.coords.x + x * column.width;
                float neighbourZ = column.coords.z + y * column.width;
                Vector2 neighbourCoord = new Vector2(neighbourX, neighbourZ);

                if (columnDict.ContainsKey(neighbourCoord))
                {
                    Column neighbour = columnDict[neighbourCoord];
                    pipes.Add(new ColumnPipe(column, neighbour));
                }
            }
        }
    }

    /**
     * Construct the pipes between a given column and its existing neighbours that have a different resolution.
     */
    private void AddExternalNeighbours(Column column, List<Column> neighbours)
    {
        foreach (Column neighbour in neighbours)
        {
            if (neighbour.GridCoord.x == column.End.x 
                || neighbour.GridCoord.y == column.End.y 
                || neighbour.End.x == column.GridCoord.x 
                || neighbour.End.y == column.GridCoord.y)
            {
                pipes.Add(new ColumnPipe(column, neighbour));
            }
        }
    }

    /**
     * Every frame, apply rainfall and drainage, flow between pipes, and update the water mesh.
     */
    private void Update()
    {
        // Increment the timer and see if the current rainfall needs to change
        timer += Time.deltaTime;

        fps = 1 / Time.unscaledDeltaTime;
        frameTimes.Enqueue(fps);
        totalFrameTime += fps;

        if (frameTimes.Count > 300)
        {
            totalFrameTime -= frameTimes.Dequeue();
        }

        float averageFPS = totalFrameTime / frameTimes.Count;

        if (timer >= timestep)
        {
            weatherIndex += 1;
            timer -= timestep;
            WeatherPoint currentWeather = wData.weatherData[weatherIndex];
            DateTime currentTime = dTime.AddSeconds(currentWeather.time);
            string rainString;
            if(currentlyRaining){
                if (rainOverride != -1)
                {
                    rainString = "\nCurrent Rainfall: " + rainOverride.ToString() + "mm";
                } else
                {
                    rainString = currentTime.ToString() + "\nCurrent Rainfall: " + currentWeather.rainfall.ToString() + "mm";
                }
            }else{ 
                rainString = "\nNot Currently Raining";
            }
            
            dateUI.text = rainString + "\nHours Elapsed: " + hoursPassed.ToString() + "\nAverage FPS: " + averageFPS.ToString() + "\nFPS: " + fps.ToString();
            hoursPassed += 1;
            intervalCount += 1; 
        }

        // Update the water simulation and the water mesh
        float time = Time.deltaTime/timestep;
        if (blockedDrainage > 1){
            blockedDrainage = 1; 
        }else if(blockedDrainage < 0){ 
            blockedDrainage = 0;
        }
        ApplyRainDrain(time);
        UpdateWater(time);
        MakeWaterMeshes();
    }

    /** 
     <summary>
    Method <c>ApplyRainDrain</c> calculates water flow within a column from its drainage and the current rain.
    Units for the <c>drain</c> and <c>rain</c> variables are meters cubed per second.
     </summary>
    **/
    private void ApplyRainDrain(float time)
    {
        bool check = true;
        if (intervalsOfRain > 0){
            
            if (intervalCount > intervalsOfRain && (intervalCount != intervalsOfRain * timeWithOutRain)){ 
                check = false; 
                currentlyRaining = false;
            }else if(intervalCount == intervalsOfRain * timeWithOutRain){ 
                intervalCount = 0; 
                check = true;
                currentlyRaining = true;
            }
        }
        
        foreach (Column column in columnDict.Values)
        {
            float rain = 0f;
            float waterDrain = 0f; 
            float soilDrain = 0f; 
            float drainDrain = 0f;

            float dim = column.width * column.width; 
            if (check){
                if (rainOverride != -1)
                {
                    rain = dim * (rainOverride / 1000f) * time;
                } else
                {
                    rain = dim * (wData.weatherData[weatherIndex].rainfall / 1000f) * time;
                }
            }
            

            //float drain = dim * (column.drainRate / 1000f) * time;
            float currentWaterAmount = rain + column.waterVolume; // Need to check the amount of water actually in the cell 

            // If it is a water cell then remove all water
            if (column.waterCount >= 1){
                waterDrain = currentWaterAmount; // Set the drain rate of the cell to be equal to the amount of water in the cell 
                currentWaterAmount = 0f; // Make sure the other types of drainage are not affected 
                
            }
            if (column.drainCapactiy > 0){ 
                // Formula for manmade drainage
                drainDrain = (column.drainCount * drainDRate) / 10f * time; 
                
                //if(drainDrain > 0) Debug.Log("Amount being removed: " + drainDrain.ToString());
                if (drainDrain > currentWaterAmount) drainDrain = currentWaterAmount; // if the drains rate is larger than water in the cell set it to the amount in the cell

                if (column.drainCapactiy - drainDrain < 0){ // If there is not enough capacity then set the drain rate to the capacity 
                    drainDrain = column.drainCapactiy; 
                    column.drainCapactiy = 0f; 
                }else{ 
                    column.drainCapactiy -= drainDrain; // If there is enough capacity then decrease it 
                }
            }
            if (column.maxDrainCapacity > 0 && rain <= 0){ //if it is not draining then give the drains back 5% each hour
                float returnedCapacity = column.maxDrainCapacity * 0.05f * time;
                if(returnedCapacity + column.drainCapactiy < column.maxDrainCapacity){
                    column.drainCapactiy += returnedCapacity;
                }else{
                    column.drainCapactiy = column.maxDrainCapacity;
                }
            }
            currentWaterAmount -= drainDrain; // Decrease the amount of water that can be removed from the sim 
            // This is so that the soil doesnt remove too much     
            // Checking the soil Cap
            if (column.soilCapacity > 0){ // If there is still cap calculate how much will be drained 
                soilDrain = (grassDRate * column.soilPercentage * dim)/ 1000f * time; // In L/h 
                if (soilDrain > currentWaterAmount) soilDrain = currentWaterAmount; // If the drain rate is larger than amount of water present, set to amount of current water

                if (column.soilCapacity - soilDrain < 0){ // If the cap is less than the amount to be drained, set
                 
                    soilDrain = column.soilCapacity; // If the amount to be drained > cap, set drain to be cap
                    column.soilCapacity = 0;
                }else{
                    column.soilCapacity -= soilDrain; 
                } 
            
            
            }
            //Debug.Log(soilDrain);
            //if (column.waterVolume * dim > 3f) column.waterVolume = 3f * dim; // Sets the upper limit of the water
            column.ApplyWaterFlow(rain - (blockedDrainage * (drainDrain + soilDrain) + waterDrain));
            
        }
    }

    /** 
    <summary>
    Method <c>ReadJson</c> tries to load the JSON file.
    </summary>
    **/
    private string ReadJson(string fileName)
    {
        string path = Application.dataPath + fileName;
        if (File.Exists(path))
        {
            Debug.Log("Loaded file from: " + path);
            using StreamReader reader = new StreamReader(path);
            return reader.ReadToEnd();
        } else
        {
            Debug.LogWarning("File not found: " + path);
            return "";
        }
    }

    /** 
    <summary>
    Method <c>UpdateWater</c> calculates the flow between columns for a given amount of time.
    </summary>
    **/
    private void UpdateWater(float time)
    {
        foreach (ColumnPipe pipe in pipes)
        {
            pipe.Flow(time);
        }
    }


    /** 
    <summary>
    Method <c>MakeMesh</c> creates the mesh that visualises the water simulation.
    </summary>
    **/
    private void MakeWaterMeshes()
    {
        List<Vector3> vertices = new List<Vector3>();
        Dictionary<Vector2, int> dict = new Dictionary<Vector2, int>(); // Quickly access column coordinates by vertex index when building triangles

        // Get a vertex for the height of every column, and save the index of that vertex.
        int index = 0;
        foreach (KeyValuePair<Vector2, Column> columnPair in columnDict)
        {
            Vector2 coords = columnPair.Key;
            Vector3 vertex = new Vector3(coords.x, columnPair.Value.WaterHeight, coords.y);
            vertices.Add(vertex);
            dict.Add(coords, index);
            index++;
        }

        // Now we're doing the triangles.

        // Initialise a list of triangles for each mesh
        List<int>[] meshesTriangles = new List<int>[waterMeshes.Count];
        for (int i = 0; i < meshesTriangles.Length; i++)
        {
            meshesTriangles[i] = new List<int>();
        }

        for (int i = 0; i < coords.Count-1; i++)
        {

            if (coords[i + 1].x != coords[i].x)
            {
                continue; // Don't put triangles between columns that don't line up on the x axis (handled later).
            }

            // We need four corners to make a square. First two corners are easy, just this column and the column after it on the x axis.
            int[] corners = new int[4];
            corners[0] = dict[coords[i]];
            corners[1] = dict[coords[i + 1]];

            // The hard part is the other two columns that don't line up on the z axis.
            // To search the z axis for the nearest two columns, we save time by guessing all the existing resolutions on the grid (starting with the smallest one).
            foreach (float sideLength in sideLengths)
            {
                // Check if two columns exist that are displaced by that side length
                // If they do, this is the side length we're looking for.
                if (columnDict.ContainsKey(coords[i] + Vector2.right * sideLength) &&
                    columnDict.ContainsKey(coords[i] + Vector2.one * sideLength)
                    ) 
                {

                    // If this side length is a different resolution than the first two columns, (i.e we're at the border between two resolutions)
                    // use the new smaller side length to make the square.
                    // This fills in gaps at the border with smaller squares.
                    if ((coords[i+1].y - coords[i].y) != sideLength)
                    {
                        corners[1] = dict[coords[i] + Vector2.up * sideLength];
                    }

                    // The other two corners are the first two displaced by the side length.
                    corners[2] = dict[coords[i] + Vector2.right * sideLength];
                    corners[3] = dict[coords[i] + Vector2.one * sideLength];

                    // Make two triangles for this square.
                    List<List<int>> square = new List<List<int>>();
                    square.Add( new List<int> { corners[3], corners[2], corners[1] });
                    square.Add( new List<int> { corners[0], corners[1], corners[2] });

                    // Assign triangles to meshes based on mean depth
                    foreach (List<int> triangle in square)
                    {
                        float meanDepth = MeanDepthOfTriangle(vertices, triangle);
                        if (meanDepth > riskThreshold)
                        {
                            int riskLevel = Math.Min((int)(meanDepth / riskLevelDifference), waterMeshes.Count - 1);
                            meshesTriangles[riskLevel].AddRange(triangle);
                        }
                    }

                    break; // Stop iterating over every side length since we found the smallest one that matched.
                }
            }

        }

        // Replace the old meshes.
        for (int i = 0; i < waterMeshes.Count; i++)
        {
            Mesh waterMesh = waterMeshes[i];

            waterMesh.Clear();
            waterMesh.vertices = vertices.ToArray();
            waterMesh.triangles = meshesTriangles[i].ToArray();
        }
    }

    /**
     * Check if there is enough water in a triangle to justify rendering it.
     */
    public float MeanDepthOfTriangle(List<Vector3> vertices, List<int> corners)
    {
        float totalDepth = 0;

        foreach (int corner in corners)
        {
            Vector3 vertex = vertices[corner];
            Column column = columnDict[new Vector2(vertex.x, vertex.z)];
            totalDepth += column.WaterDepth;
        }

        return (totalDepth / corners.Count);
    }

    /**
     * <summary>
     * Build a new grid. A water volume of -1 initialises the water volume from scratch. Otherwise all columns are set to this water volume.
     * Neighbours are columns from another grid that border this one.
     * </summary>
     */
    private void ConstructGrid(float columnSideLength, Vector2 start, Vector2 end, float _waterVolume = -1, List<Column> neighbours = null)
    {
        // Calculate the drain rate for each column
        Dictionary<string, Dictionary<string, float>> drainRates = new Dictionary<string, Dictionary<string, float>>();
        for (int d = 0; d < tData.terrainData.Count; d++)
        {
            // Find the terrain data's nearest column. If it is outside the area being constructed ignore it.
            TerrainDataPoint dataPoint = tData.terrainData[d];
            int columnCoordX = (int)(Math.Round((dataPoint.X / columnSideLength), MidpointRounding.AwayFromZero) * columnSideLength + start.x);
            int columnCoordZ = (int)(Math.Round((dataPoint.Y / columnSideLength), MidpointRounding.AwayFromZero) * columnSideLength + start.y);
            if (columnCoordX > end.x && columnCoordZ > end.y) continue;

            // Add to a list all the drainage values for the column
            string key = columnCoordX.ToString() + "," + columnCoordZ.ToString();
            if (!drainRates.ContainsKey(key)){
                drainRates[key] = new Dictionary<string, float>{
                    {"greenery", 0f}, 
                    {"water", 0f}, 
                    {"manhole", 0f}, 
                    {"inout", 0f},
                    {"catch", 0f}
                };
            } 
             switch (dataPoint.type)
            {
                case "water":
                    drainRates[key]["water"] += waterDRate;
                    break;
                case "greenery":
                    drainRates[key]["greenery"] += dataPoint.value;
                    break;
                
                case "manhole":
                    drainRates[key]["manhole"] += 1f;
                    break;
                case "inout":
                    drainRates[key]["inout"] += 1f;
                    break;
                case "catch":
                    drainRates[key]["catch"] += 1f;
                    break;
            }
        }

        // Iterate over every grid point
        for (float i = start.x; i <= end.x; i += columnSideLength)
        {
            for (float j = start.y; j <= end.y; j += columnSideLength)
            {
                if (columnDict.ContainsKey(new Vector2(i, j)))
                {
                    continue; // Skip columns that already exist
                }

                // Sample the terrain height at this grid point.
                float terrainHeight = terrainComp.SampleHeight(new Vector3(i, 0, j));

                float waterVolume;
                if (_waterVolume == -1)
                {
                    waterVolume = 0; // Initial water volume set to 0.
                } else
                {
                    waterVolume = _waterVolume; // Initial water volume set to parameter value.
                }
                float dRate = 0.0f;
                string key = i.ToString() + "," + j.ToString();
                float soilAmount = 0f; 
                float drainCount = 0f;
                float waterCount = 0f;
                if (drainRates.ContainsKey(key)) // Check if this column has a drain rate. If a column is entierly concrete it will not have a drain rate.
                {
                    // Calculate the average drain rate for this column
                    /* foreach (float x in drainRates[key].values)
                    {
                        dRate += x;
                    }
                    dRate = dRate / drainRates[key].Count; */
                    soilAmount = drainRates[key]["greenery"] / ((_columnSideLength / 10) * (_columnSideLength / 10)); // Averages the soil
                    drainCount = drainRates[key]["manhole"] + drainRates[key]["inout"] + drainRates[key]["catch"];
                    waterCount = drainRates[key]["water"]; 

                }

                // Create a new column at this grid point.
                Column newColumn = new Column(i, terrainHeight, j, dRate, waterVolume, columnSideLength, soilAmount, drainCount, waterCount, (soilAmount * soilCapacity), (drainCount * drainCapacity)); 

                AddNeighbours(newColumn);

                // If this column is at the boundary of this grid, check if we need to connect it with neighbouring columns from other grids.
                if (neighbours != null && (i == start.x || j == start.y || j == end.y - columnSideLength || i == end.x - columnSideLength) )
                {
                    AddExternalNeighbours(newColumn, neighbours);
                }

                columnDict.Add(newColumn.GridCoord, newColumn);
            }
        }

        // Sort the column coordinates and pipes by coordinates
        sideLengths.Add(columnSideLength); // Add this resolution to the set of existing resolutions
        coords = new List<Vector2>(columnDict.Keys);
        coords.Sort(coordComparer);
        pipes.Sort((pipe1, pipe2) => Mathf.Max(pipe2.source.coords.y, pipe2.sink.coords.y).CompareTo(Mathf.Max(pipe1.source.coords.y, pipe1.sink.coords.y)));
    }

    /**
     * <summary>
     * Create a new grid within a grid, with a different resolution
     * <summary>
     */
    public void ResizeColumns(float childWidth, Vector2 start, Vector2 end)
    {
        List<Column> columns = GetColumnsInRange(start, end);

        // Snap to column coordinates
        start = columns[0].GridCoord;
        end = columns[columns.Count - 1].End;

        // List of columns at the boundary between the two resolutions
        List<Column> neighbours = new List<Column>();

        float volume = 0;
        foreach (Column column in columns)
        {
            volume += column.waterVolume;
            columnDict.Remove(column.GridCoord); // Delete old resolution columns

            foreach (ColumnPipe pipe in column.pipes) // Delete old resolution pipes
            {
                pipes.Remove(pipe);
                Column neighbour = pipe.sink == column ? pipe.source : pipe.sink;
                neighbour.pipes.Remove(pipe);

                if (!columns.Contains(neighbour))
                {
                    neighbours.Add(neighbour);
                }
            }
        }

        Vector2 diff = (end - start) / childWidth; // X and Z distances across new grid.
        float childVolume = volume / (diff.x * diff.y); // New volume of each column is the mean water volume in the region

        ConstructGrid(childWidth, start, end, childVolume, neighbours); // Make the new grid

        // Sort the column coordinates and pipes again
        coords = new List<Vector2>(columnDict.Keys);
        coords.Sort(coordComparer);
    }

    /**
     * <summary>
     * Get all columns between the specified X and Z unity scene coordinates.
     * </summary>
     */
    private List<Column> GetColumnsInRange(Vector2 start, Vector2 end)
    {
        // Clean up inputs
        Vector2 tempStart = start;
        Vector2 tempEnd = end;
        start = new Vector2(Mathf.Min(tempStart.x, tempEnd.x), Mathf.Min(tempStart.y, tempEnd.y));
        end = new Vector2(Mathf.Max(tempStart.x, tempEnd.x), Mathf.Max(tempStart.y, tempEnd.y));

        // Initialise Sorters
        List<Column> result = new List<Column>();
        Vector2ORComparer magComparer = new Vector2ORComparer();
        Vector2ANDComparer andComparer = new Vector2ANDComparer();

        // Search the sorted list of column coordinates for the first column in the range.
        int index = coords.BinarySearch(start, magComparer);
        if (index < 0)
        {
            index = ~index;
        }

        // Iterate over every column in the range from there
        // Sorting in 2D is complicated, which is why there need to be a lot of different checks.
        Vector2 temp = coords[index];
        while ( index < coords.Count && magComparer.Compare(temp, end) <= 0)
        {
            temp = coords[index];
            if ( andComparer.Compare(start, temp) <= 0)
            {
                if (andComparer.Compare(temp, end) <= 0)
                {
                    Column column = columnDict[temp];
                    if (andComparer.Compare(column.End, end) <= 0)
                    {
                        result.Add(column);
                    }
                }
            }
            index += 1;
        }

        return result;
    }

    /**<summary>
     * Create a raincloud between the specified X and Z unity scene coordinates.
     * </summary>
     */
    public void CreateCloud(Vector2 start, Vector2 end)
    {
        List<Column> columns = GetColumnsInRange(start, end);

        if (columns.Count < 1)
        {
            return;
        }

        // Increase rainfall in all columns under the cloud
        foreach (Column column in columns)
        {
            column.rainfall += wData.weatherData[weatherIndex].rainfall;
        }

        // Snap cloud to column coordinates
        start = columns[0].GridCoord;
        end = columns[columns.Count - 1].GridCoord;

        // Create cloud mesh
        GameObject cloud = Instantiate(cloudPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        Mesh cloudMesh = new Mesh();
        cloud.GetComponent<MeshFilter>().mesh = cloudMesh;
        cloudMesh.vertices = new Vector3[] { new Vector3(start.x, 200, start.y), new Vector3(start.x, 200, end.y), new Vector3(end.x, 200, start.y), new Vector3(end.x, 200, end.y) };
        cloudMesh.triangles = new int[] { 3,2,1,0,1,2};
    }
}
