using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Camerafoll : MonoBehaviour
{
    GameObject parentObj;
    public Vector3 offset;
    public float Speed;

    
    void Update()
    {
        if (parentObj==null)
        {
            parentObj = GameObject.Find("megalol Variant(Clone)");
        }
        if (parentObj)
        {
            transform.position = parentObj.transform.position + offset;
            //transform.rotation=parentObj.transform.rotation;
        }
    }
}
