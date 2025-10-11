using UnityEngine;
using System.Collections.Generic;

public class ShapeManager : MonoBehaviour
{
    public GameObject shapePrefab;
    public int rows = 2;
    public int columns = 3;
    public float spacing = 1f; // Distance between shapes
    public float verticalSpacing = 1f; // Vertical distance between shapes (can be different from horizontal)
    
    [Header("Shape Scaling")]
    public Vector3 baseScale = Vector3.one; // Starting size of shapes
    public Vector3 maxScale = Vector3.one * 1.2f; // Maximum size when fully activated

    private static ShapeManager S;
    
    void Awake()
    {
        S = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CreateShapes();
    }

    void CreateShapes()
    {
        // Find all available FSRs in the scene
        FSR[] availableFSRs = FindObjectsOfType<FSR>();
        
        // Create a list of FSR indices for assignment
        List<int> fsrIndices = CreateFSRAssignmentList(availableFSRs.Length);
        
        // Calculate center offset once (outside the loops)
        float totalWidth = (columns - 1) * spacing;
        float totalHeight = (rows - 1) * verticalSpacing;
        float offsetX = -totalWidth / 2f;
        float offsetY = -totalHeight / 2f;
        
        int shapeIndex = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                GameObject shape = Instantiate(shapePrefab, transform);
                
                // Set centered position
                shape.transform.position = new Vector3(
                    col * spacing + offsetX, 
                    row * verticalSpacing + offsetY, 
                    0
                );
                
                // Rotate every other shape 180 degrees
                if (shapeIndex % 2 == 0) // Every even-indexed shape (0, 2, 4, ...)
                {
                    shape.transform.rotation = Quaternion.Euler(0, 0, 180);
                }
                
                // Configure the shape
                ShapeScript shapeScript = shape.GetComponent<ShapeScript>();
                if (shapeScript != null)
                {
                    // Set the scale values from ShapeManager
                    shapeScript.SetBaseScale(baseScale);
                    shapeScript.SetMaxScale(maxScale);
                    
                    // Assign FSR to the shape if requested
                    if (availableFSRs.Length > 0)
                    {
                        // Get the FSR index for this shape
                        int fsrIndex = fsrIndices[shapeIndex % fsrIndices.Count];
                        FSR assignedFSR = availableFSRs[fsrIndex];
                        
                        // Assign the FSR to the shape
                        shapeScript.AssignFSR(assignedFSR);
                        
                        Debug.Log($"Shape {shapeIndex} (Row {row}, Col {col}) assigned to FSR: {assignedFSR.name}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Shape {shapeIndex} doesn't have a ShapeScript component!");
                }
                
                shapeIndex++;
            }
        }
    }
    
    private List<int> CreateFSRAssignmentList(int fsrCount)
    {
        List<int> fsrIndices = new List<int>();
        
            // Create a list that uses all FSRs
            for (int i = 0; i < fsrCount; i++)
            {
                fsrIndices.Add(i);
            }
            
            // Shuffle the list if requested
            
            {
                for (int i = 0; i < fsrIndices.Count; i++)
                {
                    int temp = fsrIndices[i];
                    int randomIndex = Random.Range(i, fsrIndices.Count);
                    fsrIndices[i] = fsrIndices[randomIndex];
                    fsrIndices[randomIndex] = temp;
                }
            }
        
            // Just use FSRs in order
            for (int i = 0; i < fsrCount; i++)
            {
                fsrIndices.Add(i);
            }
        
        
        return fsrIndices;
    }
    
    // Reassign all the FSRs
    public void ReassignAllFSRs()
    {
        ShapeScript[] shapes = GetComponentsInChildren<ShapeScript>();
        FSR[] availableFSRs = FindObjectsOfType<FSR>();
        
        if (availableFSRs.Length == 0)
        {
            Debug.LogError("No FSRs found in scene!");
            return;
        }
        
        List<int> fsrIndices = CreateFSRAssignmentList(availableFSRs.Length);
        
        for (int i = 0; i < shapes.Length; i++)
        {
            int fsrIndex = fsrIndices[i % fsrIndices.Count];
            FSR assignedFSR = availableFSRs[fsrIndex];
            shapes[i].AssignFSR(assignedFSR);
            Debug.Log($"Reassigned Shape {i} to FSR: {assignedFSR.name}");
        }
    }
    
    public void LogCurrentAssignments()
    {
        ShapeScript[] shapes = GetComponentsInChildren<ShapeScript>();
        Debug.Log("=== Current FSR Assignments ===");
        
        for (int i = 0; i < shapes.Length; i++)
        {
            FSR assignedFSR = shapes[i].GetAssignedFSR();
            string fsrName = assignedFSR != null ? assignedFSR.name : "None";
            Debug.Log($"Shape {i}: {shapes[i].name} -> FSR: {fsrName}");
        }
    }
    
    
    public int GetShapeCount()
    {
        return rows * columns;
    }
    
    public int GetTotalShapes()
    {
        return GetComponentsInChildren<ShapeScript>().Length;
    }
    
    public int GetFSRCount()
    {
        return FindObjectsOfType<FSR>().Length;
    }
    
    [ContextMenu("Update All Shape Scales")]
    public void UpdateAllShapeScales()
    {
        ShapeScript[] shapes = GetComponentsInChildren<ShapeScript>();
        foreach (ShapeScript shape in shapes)
        {
            shape.SetBaseScale(baseScale);
            shape.SetMaxScale(maxScale);
        }
        Debug.Log($"Updated scale values for {shapes.Length} shapes. Base: {baseScale}, Max: {maxScale}");
    }
    
    // Public API for runtime scale changes
    public void SetBaseScale(Vector3 scale)
    {
        baseScale = scale;
        UpdateAllShapeScales();
    }
    
    public void SetMaxScale(Vector3 scale)
    {
        maxScale = scale;
        UpdateAllShapeScales();
    }
}
