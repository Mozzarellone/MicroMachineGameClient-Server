using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;

public class client : MonoBehaviour
{

    [SerializeField]
    private string address;

    [SerializeField]
    private int port;

    public GameObject objectToMove;

    private Socket socket;
    private IPEndPoint endPoint;
    GameObject[] go = new GameObject[8];
    bool xd = false;
    byte[] recivedPacket = new byte[4096];
    int myId;
    float x = 0;
    float y = 0;
    float z = 0;
    float rotY = 0;
    float speed = 7f;
    float turnSpeed = 75f;
    int numberOfClients;
    float timer = 0;
    private void Start()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        endPoint = new IPEndPoint(IPAddress.Parse(address), port);
        // send the join packet
        byte[] joinPacket = new byte[1];
        joinPacket[0] = 0;
        socket.SendTo(joinPacket, endPoint);
    }

    void Update()
    {
        DequeuePackets();
        timer += Time.deltaTime;
        if (timer >= 0.5f)
        {
            if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            {

                UpdatePosition();

            }
            timer = 0;
        }
        DequeuePackets();
    }
    private void FixedUpdate()
    {
        UpdateSpeed();
    }
    void DequeuePackets()
    {
        int rlen = 0;

        try
        {
            rlen = socket.Receive(recivedPacket);
            //if (rlen > 17)
            //    return;

            if (recivedPacket[0] == 2)
            {
                myId = BitConverter.ToInt32(recivedPacket, 1);
                x = BitConverter.ToSingle(recivedPacket, 5);
                y = BitConverter.ToSingle(recivedPacket, 9);
                z = BitConverter.ToSingle(recivedPacket, 13);
                go[myId - 1] = Instantiate(objectToMove);
                go[myId - 1].transform.position = new Vector3(x, y, z);
                //Debug.Log(myId);
            }
            if (recivedPacket[0] == 255)
            {
                numberOfClients = BitConverter.ToInt32(recivedPacket, 1);
                for (int i = 0; i < numberOfClients; i++)
                {
                    if (go[i] != go[myId - 1])
                    {
                        go[i] = Instantiate(objectToMove);
                    }
                }
            }
            if (recivedPacket[0] == 100)
            {
                int j = 0;
                int id;
                for (int i = 0; i < numberOfClients - 1; i++)
                {
                    id = BitConverter.ToInt32(recivedPacket, 1 + j);
                    x = BitConverter.ToSingle(recivedPacket, 5 + j);
                    y = BitConverter.ToSingle(recivedPacket, 9 + j);
                    z = BitConverter.ToSingle(recivedPacket, 13 + j);
                    rotY = BitConverter.ToSingle(recivedPacket, 17 + j);
                    try
                    {
                        go[id - 1].transform.position = new Vector3(x, y, z);
                        go[id - 1].transform.rotation = Quaternion.Euler(new Vector3(0, rotY*180, 0));
                    }
                    catch
                    {


                    }
                    j += 20;
                }




            }
        }
        catch
        {
            return;
        }
    }


    void UpdateSpeed()
    {

        //if (Input.GetKey(KeyCode.W))
        //{

        //    go[myId - 1].transform.position += go[myId - 1].transform.forward * speed* Time.deltaTime;
        //    UpdatePosition();

        //}
        //if (Input.GetKey(KeyCode.A))
        //{

        //    go[myId - 1].transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
        //    UpdatePosition();


        //}
        //else if (Input.GetKey(KeyCode.D))
        //{

        //    go[myId - 1].transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
        //    UpdatePosition();

        //}
        if (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {

            go[myId - 1].transform.position += go[myId - 1].transform.forward * speed * Time.deltaTime;
            UpdatePosition();

        }
        else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.W))
        {
            go[myId - 1].transform.position += go[myId - 1].transform.forward * speed * Time.deltaTime;
            // go[myId - 1].transform.Rotate(Vector3.up, -turnSpeed * Time.deltaTime);
            go[myId - 1].transform.rotation *= Quaternion.Euler(Vector3.up * -turnSpeed * Time.deltaTime);
            Debug.Log( go[myId - 1].transform.rotation.ToEuler());
            UpdatePosition();
        }
        else if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.W))
        {
            go[myId - 1].transform.position += go[myId - 1].transform.forward * speed * Time.deltaTime;
            //go[myId - 1].transform.Rotate(Vector3.up, turnSpeed * Time.deltaTime);
            go[myId - 1].transform.rotation *= Quaternion.Euler(Vector3.up * turnSpeed * Time.deltaTime);
            UpdatePosition();
        }



        if (Input.GetKey(KeyCode.P) && xd == false)
        {
            byte[] cometepare = new byte[1];
            cometepare[0] = 255;
            socket.SendTo(cometepare, endPoint);
            xd = true;
        }

    }

    void UpdatePosition()
    {
        byte[] setVelocityPacket = new byte[21];

        byte[] ID;
        byte[] X;
        byte[] Y;
        byte[] Z;
        byte[] RotY;
        ID = BitConverter.GetBytes(myId);
        X = BitConverter.GetBytes(go[myId - 1].transform.position.x);
        Y = BitConverter.GetBytes(go[myId - 1].transform.position.y);
        Z = BitConverter.GetBytes(go[myId - 1].transform.position.z);
        RotY = BitConverter.GetBytes(go[myId - 1].transform.rotation.y);
        setVelocityPacket[0] = 2;
        Buffer.BlockCopy(ID, 0, setVelocityPacket, 1, 4);
        Buffer.BlockCopy(X, 0, setVelocityPacket, 5, 4);
        Buffer.BlockCopy(Y, 0, setVelocityPacket, 9, 4);
        Buffer.BlockCopy(Z, 0, setVelocityPacket, 13, 4);
        Buffer.BlockCopy(RotY, 0, setVelocityPacket, 17, 4);
        socket.SendTo(setVelocityPacket, endPoint);
    }

}