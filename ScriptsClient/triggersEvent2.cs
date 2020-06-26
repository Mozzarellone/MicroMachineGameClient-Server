using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggersEvent2 : MonoBehaviour
{
    //public Transform player;
    public bool trigger= true;
    //This is called as soon as the collision starts
    void OnTriggerEnter(Collider c)
    {
        
        if (c.name=="megalol Variant(Clone)")
        {
            trigger=true;
        }
        
        //Destroy (c.gameObject);
    }
    //This is called if the Trigger c remains the same of the previous frame
    void OnTriggerStay(Collider c)
    {
        
        //transform.gameObject.SetActive(false);
        //player.gameObject.SetActive(true);
    }
    //This is called as soon as the collision ends
    void OnTriggerExit(Collider c)
    {
        
    }
}

