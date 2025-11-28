using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraCtrl : MonoBehaviour
{
    public float MinX;
    //public float MaxX;
    public float MinY;
    //public float MaxY;
    public GameObject PlayerObject;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (PlayerObject.transform.position.x > MinX)
        {
            transform.position = new Vector3(PlayerObject.transform.position.x, transform.position.y, transform.position.z);
        }
        else if (PlayerObject.transform.position.x <= MinX)
        {
            transform.position = new Vector3(MinX, transform.position.y, transform.position.z);
        }
        //else if (PlayerObject.transform.position.x >= MaxX)
        //{
        //    transform.position = new Vector3(MaxX, transform.position.y, transform.position.z);
        //}

        if (PlayerObject.transform.position.y > MinY)
        {
            transform.position = new Vector3(transform.position.x, PlayerObject.transform.position.y, transform.position.z);
        }
        else if (PlayerObject.transform.position.y <= MinY)
        {
            transform.position = new Vector3(transform.position.x, MinY, transform.position.z);
        }
        //else if (PlayerObject.transform.position.y >= MaxY)
        //{
        //    transform.position = new Vector3(transform.position.x, MaxY, transform.position.z);
        //}
    }
}
