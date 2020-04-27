using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Culling : MonoBehaviour
{
    public Camera target;
    
    // Start is called before the first frame update
    void Start()
    {
        var group = new CullingGroup();
        group.targetCamera = target;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
