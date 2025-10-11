using UnityEngine;
using System.Collections.Generic;

public class ShapeFSRManager : MonoBehaviour
{
    [Header("Assignment Settings")]
    [SerializeField] private bool assignOnStart = true;
    [SerializeField] private bool useRandomAssignment = true;
    [SerializeField] private bool allowMultipleShapesPerFSR = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private List<ShapeScript> shapes = new List<ShapeScript>();
    private List<FSR> fsrs = new List<FSR>();
    
    void Start()
    {
        if (assignOnStart)
        {
            AssignFSRsToShapes();
        }
    }
    
    [ContextMenu("Assign FSRs to Shapes")]
    public void AssignFSRsToShapes()
    {
        FindAllShapesAndFSRs();
        
        if (shapes.Count == 0)
        {
            Debug.LogWarning("No shapes found in scene!");
            return;
        }
        
        if (fsrs.Count == 0)
        {
            Debug.LogWarning("No FSRs found in scene!");
            return;
        }
        
        if (useRandomAssignment)
        {
            AssignRandomly();
        }
        else
        {
            AssignSequentially();
        }
        
        if (showDebugInfo)
        {
            LogAssignments();
        }
    }
    
    [ContextMenu("Reassign All Randomly")]
    public void ReassignAllRandomly()
    {
        FindAllShapesAndFSRs();
        AssignRandomly();
        
        if (showDebugInfo)
        {
            LogAssignments();
        }
    }
    
    private void FindAllShapesAndFSRs()
    {
        // Find all shapes in the scene
        shapes.Clear();
        ShapeScript[] foundShapes = FindObjectsOfType<ShapeScript>();
        shapes.AddRange(foundShapes);
        
        // Find all FSRs in the scene
        fsrs.Clear();
        FSR[] foundFSRs = FindObjectsOfType<FSR>();
        fsrs.AddRange(foundFSRs);
    }
    
    private void AssignRandomly()
    {
        if (allowMultipleShapesPerFSR)
        {
            // Each shape gets a random FSR (multiple shapes can share FSRs)
            foreach (ShapeScript shape in shapes)
            {
                int randomIndex = Random.Range(0, fsrs.Count);
                shape.AssignFSR(fsrs[randomIndex]);
            }
        }
        else
        {
            // Each shape gets a unique FSR (if we have enough FSRs)
            if (fsrs.Count < shapes.Count)
            {
                Debug.LogWarning($"Not enough FSRs ({fsrs.Count}) for all shapes ({shapes.Count}). Some shapes will share FSRs.");
            }
            
            // Create a shuffled list of FSR indices
            List<int> fsrIndices = new List<int>();
            for (int i = 0; i < fsrs.Count; i++)
            {
                fsrIndices.Add(i);
            }
            
            // Shuffle the indices
            for (int i = 0; i < fsrIndices.Count; i++)
            {
                int temp = fsrIndices[i];
                int randomIndex = Random.Range(i, fsrIndices.Count);
                fsrIndices[i] = fsrIndices[randomIndex];
                fsrIndices[randomIndex] = temp;
            }
            
            // Assign FSRs to shapes
            for (int i = 0; i < shapes.Count; i++)
            {
                int fsrIndex = fsrIndices[i % fsrIndices.Count]; // Cycle through FSRs if we have more shapes
                shapes[i].AssignFSR(fsrs[fsrIndex]);
            }
        }
    }
    
    private void AssignSequentially()
    {
        // Assign FSRs to shapes in order
        for (int i = 0; i < shapes.Count; i++)
        {
            int fsrIndex = i % fsrs.Count; // Cycle through FSRs if we have more shapes
            shapes[i].AssignFSR(fsrs[fsrIndex]);
        }
    }
    
    private void LogAssignments()
    {
        Debug.Log("=== FSR to Shape Assignments ===");
        foreach (ShapeScript shape in shapes)
        {
            FSR assignedFSR = shape.GetAssignedFSR();
            string fsrName = assignedFSR != null ? assignedFSR.name : "None";
            Debug.Log($"Shape '{shape.name}' -> FSR '{fsrName}'");
        }
    }
    
    // Public API
    public void AddShape(ShapeScript shape)
    {
        if (!shapes.Contains(shape))
        {
            shapes.Add(shape);
        }
    }
    
    public void RemoveShape(ShapeScript shape)
    {
        shapes.Remove(shape);
    }
    
    public void AddFSR(FSR fsr)
    {
        if (!fsrs.Contains(fsr))
        {
            fsrs.Add(fsr);
        }
    }
    
    public void RemoveFSR(FSR fsr)
    {
        fsrs.Remove(fsr);
    }
    
    public List<ShapeScript> GetShapes()
    {
        return new List<ShapeScript>(shapes);
    }
    
    public List<FSR> GetFSRs()
    {
        return new List<FSR>(fsrs);
    }
    
    public int GetShapeCount()
    {
        return shapes.Count;
    }
    
    public int GetFSRCount()
    {
        return fsrs.Count;
    }
}
