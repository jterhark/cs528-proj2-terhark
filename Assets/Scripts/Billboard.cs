﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    private GameObject head;
    
    // Start is called before the first frame update
    void Start()
    {
        head = GameObject.FindGameObjectWithTag("player_head");
    }

    // Update is called once per frame
    void Update()
    {
        this.gameObject.transform.LookAt(head.transform);
    }
}