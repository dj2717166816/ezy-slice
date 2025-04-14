using System.Collections;
using System.Collections.Generic;
using DataCaculater;
using UnityEngine;

public class Area : MonoBehaviour
{
    void Start()
    {
        Debug.Log(gameObject.CaculateArea());
        Debug.Log(gameObject.CalculateMeshVolume());
    }
}
