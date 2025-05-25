using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    private void Start()
    {
        LineRenderer lr = gameObject.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default")); // Ö§³ÖÑÕÉ«
        lr.widthMultiplier = 1e-4f;
        lr.positionCount = 2;
        lr.SetPosition(0, new Vector3(0.1790009f, 0.0000019f, -9.3659260f));
        lr.SetPosition(1, new Vector3(0.1790009f, 0.0000000f, -9.3649020f));
        lr.startColor = Color.yellow;
        lr.endColor = Color.yellow;
    }

}
