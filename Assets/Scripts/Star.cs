using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Star : MonoBehaviour
{
    // Start is called before the first frame update
    public int? HipparcosId { get; set; }
    public double? RightAscension { get; set; }
    public double? Declination { get; set; }
    public string Name { get; set; }
    public double? Distance { get; set; }
    public double? VisualMagnitude { get; set; }
    public string SpectralType { get; set; }
    public Vector3 CartesianPosition {
        set { this.gameObject.transform.position = value; }
    }
    public Vector3 Velocity { get; set; }



    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
