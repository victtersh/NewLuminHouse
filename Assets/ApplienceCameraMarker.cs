using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplienceCameraMarker : MonoBehaviour
{

    [SerializeField] private Applience ApplienceType;
    // Start is called before the first frame update
    
    public  Applience Applience
    {
        get => ApplienceType;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
