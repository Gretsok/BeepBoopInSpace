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

    void OnValidate()
    {

        for(int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            if (i == Slider)
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(false);
            }
        }
    }
}
