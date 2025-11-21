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
    public float randomScaleOffset = 0.25f;

    [Header("Random Positioning")]
    public bool enableRandomOffset = true; // Enable random position offsets
    public float minOffset = 0.1f; // Minimum random offset
    public float maxOffset = 0.5f; // Maximum random offset
    
    [Header("Alpha Control")]
    public bool enableAlphaControl = true; // Enable alpha control for all shapes
    public float inactiveAlpha = 0.1f; // Alpha when FSR threshold not met
    public float activeAlpha = 0.9f; // Alpha when FSR threshold is exceeded
    public float currentHoldThreshold = 0.5f; // FSR currentHold threshold to trigger alpha change
    public float alphaTransitionSpeed = 5f; // Speed of alpha transitions

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
                
                // Calculate base centered position
                float baseX = col * spacing + offsetX;
                float baseY = row * verticalSpacing + offsetY;
                
                // Add random offset if enabled
                if (enableRandomOffset)
                {
                    float randomOffsetX = Random.Range(-maxOffset, maxOffset);
                    float randomOffsetY = Random.Range(-maxOffset, maxOffset);
                    
                    // Ensure offset is at least minOffset in magnitude
                    if (Mathf.Abs(randomOffsetX) < minOffset)
                    {
                        randomOffsetX = randomOffsetX >= 0 ? minOffset : -minOffset;
                    }
                    if (Mathf.Abs(randomOffsetY) < minOffset)
                    {
                        randomOffsetY = randomOffsetY >= 0 ? minOffset : -minOffset;
                    }
                    
                    baseX += randomOffsetX;
                    baseY += randomOffsetY;
                }
                
                // Set final position
                shape.transform.position = new Vector3(baseX, baseY, 0);
                
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

                    // Set slightly Random Size
                    float randSizeAdjustment = Random.Range(-randomScaleOffset, randomScaleOffset);
                    Vector3 adjustedSize = new Vector3(
                        baseScale.x + randSizeAdjustment, 
                        baseScale.y + randSizeAdjustment, 
                        1);
                    shapeScript.SetBaseScale(adjustedSize);
                    
                    // Assign FSR to the shape if requested
                    if (availableFSRs.Length > 0)
                    {
                        // Get the FSR index for this shape
                        int fsrIndex = fsrIndices[shapeIndex % fsrIndices.Count];
                        FSR assignedFSR = availableFSRs[fsrIndex];
                        
                        // Assign the FSR to the shape
                        shapeScript.AssignFSR(assignedFSR);
                        
                    }
                    
                    // Set alpha settings from ShapeManager
                    shapeScript.SetAlphaSettings(enableAlphaControl, inactiveAlpha, activeAlpha, currentHoldThreshold, alphaTransitionSpeed);
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
        int totalShapes = rows * columns;
        
        // Calculate how many shapes each FSR should control
        int shapesPerFSR = totalShapes / fsrCount;
        int extraShapes = totalShapes % fsrCount;
        
        // Create a list where each FSR appears the appropriate number of times
        for (int fsrIndex = 0; fsrIndex < fsrCount; fsrIndex++)
        {
            int timesToAdd = shapesPerFSR;
            
            // Give extra shapes to the first few FSRs
            if (fsrIndex < extraShapes)
            {
                timesToAdd++;
            }
            
            // Add this FSR the calculated number of times
            for (int i = 0; i < timesToAdd; i++)
            {
                fsrIndices.Add(fsrIndex);
            }
        }
        
        // Shuffle the list to randomize the assignment order
        for (int i = 0; i < fsrIndices.Count; i++)
        {
            int temp = fsrIndices[i];
            int randomIndex = Random.Range(i, fsrIndices.Count);
            fsrIndices[i] = fsrIndices[randomIndex];
            fsrIndices[randomIndex] = temp;
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
        }
    }
    
    public void LogCurrentAssignments()
    {
        ShapeScript[] shapes = GetComponentsInChildren<ShapeScript>();
        
        for (int i = 0; i < shapes.Length; i++)
        {
            FSR assignedFSR = shapes[i].GetAssignedFSR();
            string fsrName = assignedFSR != null ? assignedFSR.name : "None";
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
    }
    
    [ContextMenu("Randomize All Shape Positions")]
    public void RandomizeAllShapePositions()
    {
        ShapeScript[] shapes = GetComponentsInChildren<ShapeScript>();
        int shapeIndex = 0;
        
        // Calculate center offset (same as in CreateShapes)
        float totalWidth = (columns - 1) * spacing;
        float totalHeight = (rows - 1) * verticalSpacing;
        float offsetX = -totalWidth / 2f;
        float offsetY = -totalHeight / 2f;
        
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (shapeIndex < shapes.Length)
                {
                    GameObject shape = shapes[shapeIndex].gameObject;
                    
                    // Calculate base centered position
                    float baseX = col * spacing + offsetX;
                    float baseY = row * verticalSpacing + offsetY;
                    
                    // Add random offset if enabled
                    if (enableRandomOffset)
                    {
                        float randomOffsetX = Random.Range(-maxOffset, maxOffset);
                        float randomOffsetY = Random.Range(-maxOffset, maxOffset);
                        
                        // Ensure offset is at least minOffset in magnitude
                        if (Mathf.Abs(randomOffsetX) < minOffset)
                        {
                            randomOffsetX = randomOffsetX >= 0 ? minOffset : -minOffset;
                        }
                        if (Mathf.Abs(randomOffsetY) < minOffset)
                        {
                            randomOffsetY = randomOffsetY >= 0 ? minOffset : -minOffset;
                        }
                        
                        baseX += randomOffsetX;
                        baseY += randomOffsetY;
                    }
                    
                    // Set final position
                    shape.transform.position = new Vector3(baseX, baseY, 0);
                    
                    shapeIndex++;
                }
            }
        }
        
        Debug.Log($"Randomized positions for {shapes.Length} shapes");
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
    
    // Alpha control API
    public void SetAlphaControl(bool enabled)
    {
        enableAlphaControl = enabled;
        UpdateAllShapeAlphaSettings();
    }
    
    public void SetInactiveAlpha(float alpha)
    {
        inactiveAlpha = Mathf.Clamp01(alpha);
        UpdateAllShapeAlphaSettings();
    }
    
    public void SetActiveAlpha(float alpha)
    {
        activeAlpha = Mathf.Clamp01(alpha);
        UpdateAllShapeAlphaSettings();
    }
    
    public void SetCurrentHoldThreshold(float threshold)
    {
        currentHoldThreshold = threshold;
        UpdateAllShapeAlphaSettings();
    }
    
    public void SetAlphaTransitionSpeed(float speed)
    {
        alphaTransitionSpeed = speed;
        UpdateAllShapeAlphaSettings();
    }
    
    private void UpdateAllShapeAlphaSettings()
    {
        ShapeScript[] shapes = GetComponentsInChildren<ShapeScript>();
        foreach (ShapeScript shape in shapes)
        {
            shape.SetAlphaSettings(enableAlphaControl, inactiveAlpha, activeAlpha, currentHoldThreshold, alphaTransitionSpeed);
        }
    }
}
