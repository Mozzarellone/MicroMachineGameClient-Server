
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
public class GameClient : MonoBehaviour
{

    [SerializeField]
    private string address;

    [SerializeField]
    private int port;

    public GameObject objectToMove;
    public GameObject StartTrigger;
    public Canvas CanvasWin;
    public Canvas CanvasLose;
    public Canvas CanvasGameOver;
    public Canvas CanvasAlreadyStarted;
    public Canvas TooManyPlayers;
    public Canvas CanvasStart;
    private Socket socket;
    private IPEndPoint endPoint;
    GameObject[] go = new GameObject[8];
    byte[] recivedPacket = new byte[4096];
    int myId;
    float x, y, z, rotationY = 0;
    float speed = 7f;
    float turnSpeed = 100;
    int numberOfClients = 0;
    uint[] clientsIDArray;
    float UpdatePositionTimer, CloseApplicationTimer, CheckIfOutOfMapTimer = 0;
    bool SendWinnerBool = false;
    bool UnlockSpeed = false;
    bool CloseApplicationBool = false;
    bool OneShotReady = false;
    
    private void Start()
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;
        endPoint = new IPEndPoint(IPAddress.Parse(address), port);
        byte[] joinPacket = new byte[1];
        joinPacket[0] = 0;
        socket.SendTo(joinPacket, endPoint);
    }
    void Update()
    {
        FinalDequeuePackets();
        FinalUpdatePosition();

        if (CloseApplicationBool)
            CloseApplication();


        if (StartTrigger.GetComponent<triggersEvent>().count == 2 && SendWinnerBool == false)
        {
            SendWinner();
            SendWinnerBool = true;
        }
        CheckIfOutOfMap();
    }


    private void FixedUpdate()
    {
        if (Input.GetKey(KeyCode.R) && OneShotReady == false)
        {
            byte[] ready = new byte[1];
            ready[0] = 1;
            socket.SendTo(ready, endPoint);
            OneShotReady = true;
        }

        if (UnlockSpeed)
            UpdateSpeed();
    }

    void FinalDequeuePackets()
    {
        DequeuePackets();

        if (numberOfClients != 0)
        {
            for (int i = 0; i < (numberOfClients - 1) * 10; i++)
            {
                DequeuePackets();
            }
        }
    }


    void FinalUpdatePosition()
    {
        UpdatePositionTimer += Time.deltaTime;
        if (UpdatePositionTimer >= 0.3f && CloseApplicationBool == false)
        {
            if (!Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
            {
                UpdatePosition();
            }
            UpdatePositionTimer = 0;
        }
    }

    void CloseApplication()
    {
        CloseApplicationTimer += Time.deltaTime;
        if (CloseApplicationTimer >= 3.00f)
        {

            Application.Quit();
        }
    }



    void DequeuePackets()
    {
        int rlen = 0;
        try
        {
            rlen = socket.Receive(recivedPacket);
        }
        catch
        {
            return;
        }

        if (rlen > 141)
            return;

        switch (recivedPacket[0])
        {

            case 2:                                               //JOIN

                myId = BitConverter.ToInt32(recivedPacket, 1);
                x = BitConverter.ToSingle(recivedPacket, 5);
                y = BitConverter.ToSingle(recivedPacket, 9);
                z = BitConverter.ToSingle(recivedPacket, 13);
                go[myId - 1] = Instantiate(objectToMove);
                go[myId - 1].transform.position = new Vector3(x, y, z);
                UpdatePosition();
                CanvasStart.gameObject.SetActive(true);
                break;



            case 255:                                               //INSTANTIATE ALL OTHER CLIENTS

                numberOfClients = BitConverter.ToInt32(recivedPacket, 1);
                clientsIDArray = new uint[numberOfClients - 1];
                for (int i = 0; i < numberOfClients - 1; i++)
                    clientsIDArray[i] = BitConverter.ToUInt32(recivedPacket, 5 + (i * 4));

                foreach (var item in clientsIDArray)
                {
                    if (go[item - 1] != go[myId - 1])
                    {
                        go[item - 1] = Instantiate(objectToMove);
                        go[item - 1].name = "Player" + (item);
                    }
                }

                UnlockSpeed = true;
                UpdatePosition();
                CanvasStart.gameObject.SetActive(false);
                break;



            case 100:                                               //UPDATE OTHER CLIENTS POSITIONS

                int j = 0;
                int id;
                for (int i = 0; i < numberOfClients - 1; i++)
                {
                    id = BitConverter.ToInt32(recivedPacket, 1 + j);
                    x = BitConverter.ToSingle(recivedPacket, 5 + j);
                    y = BitConverter.ToSingle(recivedPacket, 9 + j);
                    z = BitConverter.ToSingle(recivedPacket, 13 + j);
                    rotationY = BitConverter.ToSingle(recivedPacket, 17 + j);
                    if (go[id - 1] != null)
                    {
                        go[id - 1].transform.position = new Vector3(x, y, z);
                        go[id - 1].transform.rotation = Quaternion.Euler(new Vector3(0, rotationY, 0));
                    }
                    j += 20;
                }
                break;



            case 30:                                                //WIN

                UnlockSpeed = false;
                if (CanvasLose.gameObject.active == false)
                    CanvasWin.gameObject.SetActive(true);

                CloseApplicationBool = true;
                break;



            case 31:                                                //LOSE

                UnlockSpeed = false;
                if (CanvasWin.gameObject.active == false)
                    CanvasLose.gameObject.SetActive(true);

                CloseApplicationBool = true;
                break;



            case 200:                                                //COLLISION

                
                x = BitConverter.ToSingle(recivedPacket, 1);
                y = BitConverter.ToSingle(recivedPacket, 5);
                z = BitConverter.ToSingle(recivedPacket, 9);
                go[myId - 1].transform.position += new Vector3(x, y, z) * 0.15f;
                UpdatePosition();
                
                break;



            case 161:                                                //GAME ALREADY STARTED

                CanvasAlreadyStarted.gameObject.SetActive(true);
                CloseApplicationBool = true;
                break;



            case 162:                                                //TOO MANY PLAYERS

                TooManyPlayers.gameObject.SetActive(true);
                CloseApplicationBool = true;
                break;



            case 18:                                                //KEEP ALIVE RESPONSE

                byte[] imAlive = new byte[5];
                imAlive[0] = 18;
                byte[] ID = BitConverter.GetBytes(myId);
                Buffer.BlockCopy(ID, 0, imAlive, 1, 4);
                socket.SendTo(imAlive, endPoint);
                break;



            case 88:                                                //DESTROY DEAD CLIENTS

                uint IdToDestroy = BitConverter.ToUInt32(recivedPacket, 1);
                if (numberOfClients > 0)
                    numberOfClients--;

                Destroy(go[IdToDestroy - 1]);
                break;
        }

    }

    void UpdateSpeed()
    {

        if (Input.GetKey(KeyCode.W) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            go[myId - 1].transform.position += go[myId - 1].transform.forward * speed *Time.deltaTime;
            UpdatePosition();
        }
        else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.W))
        {
            go[myId - 1].transform.position += go[myId - 1].transform.forward * speed *Time.deltaTime;
            go[myId - 1].transform.Rotate(go[myId - 1].transform.up, -turnSpeed * Time.deltaTime);
            UpdatePosition();
        }
        else if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.W))
        {
            go[myId - 1].transform.position += go[myId - 1].transform.forward * speed *Time.deltaTime;
            go[myId - 1].transform.Rotate(go[myId - 1].transform.up, turnSpeed * Time.deltaTime);
            UpdatePosition();
        }


        if (Input.GetKey(KeyCode.S) && !Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D))
        {
            go[myId - 1].transform.position += go[myId - 1].transform.forward* - speed * Time.deltaTime;
            UpdatePosition();
        }
        else if (Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.S))
        {
            go[myId - 1].transform.position += go[myId - 1].transform.forward* - speed * Time.deltaTime;
            go[myId - 1].transform.Rotate(go[myId - 1].transform.up, turnSpeed * Time.deltaTime);
            UpdatePosition();
        }
        else if (Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.S))
        {
            go[myId - 1].transform.position += go[myId - 1].transform.forward* - speed * Time.deltaTime;
            go[myId - 1].transform.Rotate(go[myId - 1].transform.up, -turnSpeed * Time.deltaTime);
            UpdatePosition();
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
        RotY = BitConverter.GetBytes(go[myId - 1].transform.localEulerAngles.y);
        setVelocityPacket[0] = 2;
        Buffer.BlockCopy(ID, 0, setVelocityPacket, 1, 4);
        Buffer.BlockCopy(X, 0, setVelocityPacket, 5, 4);
        Buffer.BlockCopy(Y, 0, setVelocityPacket, 9, 4);
        Buffer.BlockCopy(Z, 0, setVelocityPacket, 13, 4);
        Buffer.BlockCopy(RotY, 0, setVelocityPacket, 17, 4);
        socket.SendTo(setVelocityPacket, endPoint);
    }

    void SendWinner()
    {
        byte[] WinnerPacket = new byte[5];
        byte[] ID;
        ID = BitConverter.GetBytes(myId);
        WinnerPacket[0] = 20;
        Buffer.BlockCopy(ID, 0, WinnerPacket, 1, 4);

        socket.SendTo(WinnerPacket, endPoint);
    }

    void CheckIfOutOfMap()
    {
        try
        {
            if (go[myId - 1].transform.position.y <= -1 && go[myId - 1] != null)
            {
                CanvasGameOver.gameObject.SetActive(true);

                CheckIfOutOfMapTimer += Time.deltaTime;
                if (CheckIfOutOfMapTimer >= 0.7f)
                {
                    Application.Quit();
                }
            }
        }
        catch (Exception)
        {
        }
    }
}


