using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Distance : MonoBehaviour
{

    public TextMesh text;
    public GameObject player;
    public GameObject source;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        text.text = $"{Math.Round(Vector3.Distance(player.transform.position, source.transform.position)/5.0f, 2)} pc";
    }
}
