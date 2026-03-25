using UnityEngine;
using System.Collections;

public class Marble : MonoBehaviour
{
    public static Marble S; 
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        S = this;           
    }

    public IEnumerator ResetMarble(){
        yield return new WaitForSeconds(5f);
        transform.position = new Vector3(0, 0, 0);

        Debug.Log("Resetting Marble");

    }                  
}
