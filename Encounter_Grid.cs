using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class Encounter_Grid : MonoBehaviour
{
    // Grid settings
    private int width;
    private int height;
    private float cellSize;
    private GameObject parentOBJ;

    // Collections for grid cells and text
    private int[,] gridArray;
    private Dictionary<Vector2Int, GameObject> cellDictionary = new Dictionary<Vector2Int, GameObject>();
    private List<TMP_Text> cellTextList = new List<TMP_Text>();

    // Path and line drawing variables
    public Dictionary<Vector2Int, GameObject> pathedCells = new Dictionary<Vector2Int, GameObject>();
    private int colorInt = 0;
    private int pathRerollCounter = 0;
    private GameObject lineParent;

    //rules
    private int minimumPathedCells = 38; //this needs to be increased "likely" 

    /// Custom constructor to initialize the grid.
    public Encounter_Grid(int width, int height, float cellSize, GameObject cellObj, GameObject parentOBJ)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.parentOBJ = parentOBJ;
        gridArray = new int[width, height];

        // Populate the grid with cells.
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                Vector3 cellPosition = GetWorldPosition(x, z);
                Vector2Int cellPos = new Vector2Int(x, z);
                GameObject cell = Instantiate(cellObj, cellPosition, Quaternion.identity, parentOBJ.transform);
                cell.name = "Cell_" + x + "_" + z;
                cellDictionary.Add(cellPos, cell);

                // Set the default color.
                ToggleColor(cell);

                // Update text for debugging.
                TMP_Text textRef = cell.GetComponentInChildren<TMP_Text>();
                textRef.text = $"{x},{z}";
                cellTextList.Add(textRef);
            }
        }

        //ToggleText(); 
    }

    /// Calculates the world position for a cell.
    /// 
    private float xCellSizeModifier = 1.55f; //using this to make the map longer, by extending the width
    public Vector3 GetWorldPosition(int x, int z)
    {
        Vector3 gridCenterOffset = new Vector3((width - 1) * cellSize / 2f, 0, (height - 1) * cellSize / 2f);
        return parentOBJ.transform.position + new Vector3(x * cellSize * xCellSizeModifier, 0, z * cellSize) - gridCenterOffset;
    }

    /// Generates the path(s) on the grid.
    public void GeneratePath(int numberOfPaths)
    {
        pathedCells.Clear();
        for (int i = 0; i < numberOfPaths; i++)
        {
            GenerateSinglePath();
            colorInt++;
        }

        // Validate that the left and right edges meet requirements.
        if (!ValidatePathEdges(numberOfPaths))
        {
            pathRerollCounter++;
            RerollPath(numberOfPaths);
            return;
        }
        // Validate that the minimum amount of Pathed Cells has been achieved.
        if (pathedCells.Count <= minimumPathedCells)
        {
            pathRerollCounter++;
            Debug.Log("Not enough pathed cells: " + pathedCells.Count);
            RerollPath(numberOfPaths);
            return;
        }
        Debug.Log("CleanRoll: Total Rerolls " + pathRerollCounter);
        pathRerollCounter = 0;
        RemoveNonPathNodes();
        DrawPathLines();
    }

    /// Generates a single path from the left to the right side.
    private void GenerateSinglePath()
    {
        int currentX = 0;
        int currentZ = Random.Range(0, height);
        MarkPathCell(currentX, currentZ);

        while (currentX < width - 1)
        {
            currentX++;
            int verticalMove = Random.Range(0, 3) - 1;

            // Prevent moving out of bounds.
            if (currentZ == 0 && verticalMove == -1) verticalMove = 0;
            if (currentZ == height - 1 && verticalMove == 1) verticalMove = 0;

            currentZ += verticalMove;
            MarkPathCell(currentX, currentZ);
        }
    }

    /// Marks a cell as part of the path.
    private void MarkPathCell(int x, int z)
    {
        gridArray[x, z] = 1;
        Vector2Int pos = new Vector2Int(x, z);
        MarkCellAsPath(pos);
    }

    /// Colors and records a cell as part of the path.
    private void MarkCellAsPath(Vector2Int cellPos)
    {
        Color[] pathColors = new Color[]
        {
            Color.green, Color.blue, Color.red, Color.yellow, Color.white, Color.black
        };

        if (cellDictionary.ContainsKey(cellPos))
        {
            GameObject cell = cellDictionary[cellPos];
            pathedCells.TryAdd(cellPos, cell);
            Renderer rend = cell.GetComponentInChildren<Renderer>();
            if (rend != null)
            {

                if (Application.isPlaying)
                {
                    rend.material.color = pathColors[colorInt];
                }
            }
            //add encounter cell if it doesnt already exist
            if(cell.GetComponent<Encounter_Cell>() == null)
            {
                cell.AddComponent<Encounter_Cell>(); //
            }
        }
    }

    /// Checks if both the left and right edges have enough path cells.
    private bool ValidatePathEdges(int numberOfPaths)
    {
        int leftEdgeCount = pathedCells.Count(kvp => kvp.Key.x == 0);
        int rightEdgeCount = pathedCells.Count(kvp => kvp.Key.x == width - 1);
        return leftEdgeCount >= numberOfPaths && rightEdgeCount >= numberOfPaths;
    }

    /// Resets path colors and re-generates the path.
    private void RerollPath(int numberOfPaths)
    {
        colorInt = 0;
        ResetPathColors();
        GeneratePath(numberOfPaths);
    }

    /// Resets the colors of all path cells to the default.
    private void ResetPathColors()
    {
        foreach (var entry in pathedCells)
        {
            Renderer rend = entry.Value.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.black;
            }
        }
    }

    /// Deactivates any cell that is not part of the path.
    private void RemoveNonPathNodes()
    {
        foreach (GameObject cell in cellDictionary.Values)
        {
            if (!pathedCells.Values.Contains(cell))
            {
                cell.SetActive(false);
            }
        }
    }

    /// Draws lines connecting the path cells.
    private void DrawPathLines()
    {
        // Find the path lines parent.
        lineParent = GameObject.Find("PathLines");
        if (lineParent == null)
        {
            lineParent = new GameObject("PathLines");
        }
        // Clear existing lines.
        for (int i = lineParent.transform.childCount - 1; i >= 0; i--)
        {


            if (Application.isPlaying)
            {
                Destroy(lineParent.transform.GetChild(i).gameObject);

            }
            else
            {
                DestroyImmediate(lineParent.transform.GetChild(i).gameObject);

            }

        }

        // Draw new line segments.
        foreach (var kvp in pathedCells)
        {
            Vector2Int cellPos = kvp.Key;
            GameObject cellObj = kvp.Value;
            Vector3 startPos = cellObj.transform.position;

            // Define neighbor offsets. These include horizontal and diagonal connections.
            Vector2Int[] neighborOffsets = new Vector2Int[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(1, 1),
                new Vector2Int(1, -1)
            };

            foreach (Vector2Int offset in neighborOffsets)
            {
                // If this is a diagonal offset, check for the illegal square configuration.
                if (offset.x != 0 && offset.y != 0)
                {
                    // For diagonal (1,1): check if the cell directly above and directly right exist.
                    if (offset == new Vector2Int(1, 1))
                    {
                        Vector2Int cellAbove = new Vector2Int(cellPos.x, cellPos.y + 1);
                        Vector2Int cellRight = new Vector2Int(cellPos.x + 1, cellPos.y);
                        if (pathedCells.ContainsKey(cellAbove) && pathedCells.ContainsKey(cellRight))
                        {
                            // Skip drawing this diagonal to prevent crossing.
                            continue;
                        }
                    }
                    // For diagonal (1,-1): check if the cell directly below and directly right exist.
                    else if (offset == new Vector2Int(1, -1))
                    {
                        Vector2Int cellBelow = new Vector2Int(cellPos.x, cellPos.y - 1);
                        Vector2Int cellRight = new Vector2Int(cellPos.x + 1, cellPos.y);
                        if (pathedCells.ContainsKey(cellBelow) && pathedCells.ContainsKey(cellRight))
                        {
                            // Skip drawing this diagonal.
                            continue;
                        }
                    }
                }

                // Check for the neighbor in this direction.
                Vector2Int neighborPos = cellPos + offset;
                if (pathedCells.TryGetValue(neighborPos, out GameObject neighborObj))
                {
                    Vector3 endPos = neighborObj.transform.position;
                    GameObject lineSegment = new GameObject($"Line_{cellPos}_{neighborPos}");
                    lineSegment.transform.parent = lineParent.transform;
                    LineRenderer lr = lineSegment.AddComponent<LineRenderer>();
                    lr.positionCount = 2;
                    lr.SetPosition(0, startPos);
                    lr.SetPosition(1, endPos);
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.05f;
                    lr.material = new Material(Shader.Find("Sprites/Default"));
                    lr.startColor = Color.black;
                    lr.endColor = Color.black;

                    Encounter_Cell cellData = cellObj.GetComponent<Encounter_Cell>();
                    if(!cellData.viableNeighborPositions.Contains(neighborPos))
                    {
                        cellData.viableNeighborPositions.Add(neighborPos); //adds neighborPos
                    }

                }
            }
        }

    }

    public IEnumerable<KeyValuePair<Vector2Int, GameObject>> GetPathedCellsSortedByPosition()
    {
        // Sort first by x in descending order (last row first)
        // then by y ascending if needed
        return pathedCells.OrderByDescending(kvp => kvp.Key.x)
                                                .ThenBy(kvp => kvp.Key.y);
    }

    /// Toggles the text (for debugging) on or off.
    public void ToggleText()
    {
        bool isActive = cellTextList[0].gameObject.activeSelf;
        foreach (TMP_Text text in cellTextList)
        {
            text.gameObject.SetActive(!isActive);
        }
    }

    /// Sets the default color for a cell.
    public void ToggleColor(GameObject cellOBJ)
    {
        Renderer rend = cellOBJ.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = Color.black;
        }
    }
}
