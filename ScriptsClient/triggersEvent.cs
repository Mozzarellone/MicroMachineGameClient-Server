using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class triggersEvent : MonoBehaviour
{
    //public Transform player;
    public GameObject waypoint;
    public int count = 0;
    //This is called as soon as the collision starts
   
    void OnTriggerEnter(Collider c)
    {
        //Debug.Log(gameObject.name + " OnTriggerEnter");
        if (c.name=="megalol Variant(Clone)"&& waypoint.GetComponent<triggersEvent2>().trigger==true)
        {
            count++;
            Debug.Log("triggerrrrr");
        }
        
        //Destroy (c.gameObject);
    }
    //This is called if the Trigger c remains the same of the previous frame
    void OnTriggerStay(Collider c)
    {
        //Debug.Log(gameObject.name + " OnTriggerStay");
        //transform.gameObject.SetActive(false);
        //player.gameObject.SetActive(true);
    }
    //This is called as soon as the collision ends
    void OnTriggerExit(Collider c)
    {
        if (c.name == "megalol Variant(Clone)")
        {

            waypoint.GetComponent<triggersEvent2>().trigger = false;
        }
    }
}

