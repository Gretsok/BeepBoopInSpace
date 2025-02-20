using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;



[ExecuteInEditMode]
public class TextureSwitcher : MonoBehaviour
{


    [Range(0, 9)]
    public int Slider;
    public GameObject[] allChildren;

    void Awake()
    {
        allChildren = new GameObject[transform.childCount];



    }

    void OnValidate()
    {

        for (int i = 0; i < allChildren.Length; i++)
        {
            allChildren[i] = transform.GetChild(i).gameObject;
            transform.GetChild(i).gameObject.SetActive(false);
            transform.GetChild(Slider).gameObject.SetActive(true);
        }

    }
}
