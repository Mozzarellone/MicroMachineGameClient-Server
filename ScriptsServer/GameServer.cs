
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using OpenTK;

namespace MicroMachine_22_05_2019
{
    public class ServerException : Exception
    {
        public ServerException(string message) : base(message)
        {
        }
    }
    public class GameServer
    {
        private delegate void GameCommand(byte[] data, EndPoint sender);
        private Dictionary<byte, GameCommand> commandsTable;
        Dictionary<EndPoint, uint> clientsTable = new Dictionary<EndPoint, uint>(8);
        public Dictionary<EndPoint, uint> ClientsTable { get { return clientsTable; } set { clientsTable = value; } }
        Dictionary<uint, float[]> clientPosition = new Dictionary<uint, float[]>(8);
        public Dictionary<uint, float[]> ClientPosition { get { return clientPosition; } set { clientPosition = value; } }
        Dictionary<uint, int> clientAlive = new Dictionary<uint, int>(8);
        public Dictionary<uint, int> ClientAlive { get { return clientAlive; } set { clientAlive = value; } }
        float[] playersInfo = new float[4] { 0, 0, 0, 0 };
        public float[] PlayersInfo { get { return playersInfo; } set { playersInfo = value; } }
        private ITransport transport;
        uint idCounter = 0;
        public uint IDCounter { get { return idCounter; } set { idCounter = value; } }
        uint id;
        public uint ID { get { return id; } set { id = value; } }
        float spawnPositionCounter = 0;
        bool oneShotReady = false;
        public bool OneShotReady { get { return oneShotReady; } set { oneShotReady = value; } }
        bool oneShotJoin = false;
        public bool OneShotJoin { get { return oneShotJoin; } set { oneShotJoin = value; } }
        int readyCounter = 0;
        public int ReadyCounter { get { return readyCounter; } set { readyCounter = value; } }
        float keepAliveTimer = 0;
        public float KeepAliveTimer { get { return keepAliveTimer; } set { keepAliveTimer = value; } }
        uint PlayerToDestroyID;
        EndPoint PlayerToDestroyEndPoint;
        bool dictionaryRemover = false;
        public bool DictionaryRemover { get { return dictionaryRemover; } set { dictionaryRemover = value; } }
        float distance;
        public float Distance { get { return distance; } set { distance = value; } }

        public GameServer(ITransport Transport)
        {
            this.transport = Transport;
            commandsTable = new Dictionary<byte, GameCommand>();
            commandsTable[0] = Join;
            commandsTable[1] = Ready;
            commandsTable[2] = UpdatePlayerPosition;
            commandsTable[20] = Ranking;
            commandsTable[18] = AlivePlayer;
        }

        private void Join(byte[] data, EndPoint sender)
        {
            if (OneShotJoin == true)
            {
                byte[] StartedAlready = new byte[1];
                StartedAlready[0] = 161;
                transport.Send(StartedAlready, sender);
            }
            else if (idCounter < 8)
            {
                idCounter++;
                if (!ClientsTable.ContainsKey(sender))
                {
                    ClientsTable.Add(sender, idCounter);
                    ClientPosition.Add(idCounter, playersInfo);
                    ClientAlive.Add(idCounter, 1);
                }
                byte[] ResponsePacketPosition = new byte[17];
                ResponsePacketPosition[0] = 2;

                byte[] id;
                byte[] joinPositionX;
                byte[] joinPositionY;
                byte[] joinPositionZ;

                id = BitConverter.GetBytes(idCounter);
                if (idCounter % 2 == 0)
                {
                    joinPositionX = BitConverter.GetBytes(1f);
                    joinPositionY = BitConverter.GetBytes(3.77f);
                    joinPositionZ = BitConverter.GetBytes(-2f - spawnPositionCounter);
                    spawnPositionCounter += 2f;
                }
                else
                {
                    joinPositionX = BitConverter.GetBytes(-1.5f);
                    joinPositionY = BitConverter.GetBytes(3.77f);
                    joinPositionZ = BitConverter.GetBytes(-2f - spawnPositionCounter);
                }
                Buffer.BlockCopy(id, 0, ResponsePacketPosition, 1, 4);
                Buffer.BlockCopy(joinPositionX, 0, ResponsePacketPosition, 5, 4);
                Buffer.BlockCopy(joinPositionY, 0, ResponsePacketPosition, 9, 4);
                Buffer.BlockCopy(joinPositionZ, 0, ResponsePacketPosition, 13, 4);
                transport.Send(ResponsePacketPosition, sender);
            }
            else
            {
                byte[] tooManyPlayers = new byte[1];
                tooManyPlayers[0] = 162;
                transport.Send(tooManyPlayers, sender);
            }
        }

        private void Ready(byte[] data, EndPoint sender)
        {
            if (OneShotReady == false)
            {
                ReadyCounter++;
            }
        }

        private void UpdatePlayerPosition(byte[] data, EndPoint sender)
        {
            ID = BitConverter.ToUInt32(data, 1);
            float x = BitConverter.ToSingle(data, 5);
            float y = BitConverter.ToSingle(data, 9);
            float z = BitConverter.ToSingle(data, 13);
            float rotY = BitConverter.ToSingle(data, 17);
            playersInfo = new float[4] { x, y, z, rotY };
            if (ClientPosition.ContainsKey(ID))
            {
                ClientPosition[ID][0] = playersInfo[0];
                ClientPosition[ID][1] = playersInfo[1];
                ClientPosition[ID][2] = playersInfo[2];
                ClientPosition[ID][3] = playersInfo[3];
            }
        }

        private void Ranking(byte[] data, EndPoint sender)
        {
            ID = BitConverter.ToUInt32(data, 1);

            foreach (var item in ClientsTable)
            {
                if (item.Value == ID)
                {
                    byte[] WinnerPlayer = new byte[1];
                    WinnerPlayer[0] = 30;
                    transport.Send(WinnerPlayer, sender);
                }
                else
                {
                    byte[] LosePlayer = new byte[1];
                    LosePlayer[0] = 31;
                    transport.Send(LosePlayer, item.Key);
                }

            }
        }
        private void AlivePlayer(byte[] data, EndPoint sender)
        {
            ID = BitConverter.ToUInt32(data, 1);
            ClientAlive[ID] = 1;
        }

        public void Update()
        {
            while (true)
            {
                SingleStep();
                if (ClientsTable.Count > 1 && ReadyCounter == ClientsTable.Count)
                {
                    SendPositionToAllClientExeptMe();

                    if (OneShotReady == false)
                        SendNumberOfClientsToAllClientExeptMe();
                }
                KeepAlive();
                CheckCollision();
            }
        }

        public void SendNumberOfClientsToAllClientExeptMe()
        {
            foreach (var item in ClientsTable)
            {
                byte[] Clients = new byte[5 + ((ClientsTable.Count - 1) * 4)];
                Clients[0] = 255;
                byte[] numberOfClient = BitConverter.GetBytes(ClientAlive.Count);
                Buffer.BlockCopy(numberOfClient, 0, Clients, 1, 4);
                int i = 0;
                foreach (var item2 in ClientAlive)
                {
                    if (item.Value != item2.Key)
                    {
                        byte[] id = BitConverter.GetBytes(item2.Key);
                        Buffer.BlockCopy(id, 0, Clients, 5 + i, 4);
                        i += 4;
                    }
                }
                transport.Send(Clients, item.Key);
            }
            OneShotJoin = true;
            OneShotReady = true;
        }

        public void SendPositionToAllClientExeptMe()
        {
            foreach (var item in ClientsTable)
            {
                byte[] clientsPosition = new byte[(21 * (ClientsTable.Count - 1)) - (ClientsTable.Count - 2)];
                clientsPosition[0] = 100;
                int j = 0;
                foreach (var item2 in ClientAlive)
                {
                    byte[] id;
                    byte[] x;
                    byte[] y;
                    byte[] z;
                    byte[] rotY;
                    id = BitConverter.GetBytes(item2.Key);
                    x = BitConverter.GetBytes(ClientPosition[item2.Key][0]);
                    y = BitConverter.GetBytes(ClientPosition[item2.Key][1]);
                    z = BitConverter.GetBytes(ClientPosition[item2.Key][2]);
                    rotY = BitConverter.GetBytes(ClientPosition[item2.Key][3]);

                    if (item.Value != item2.Key)
                    {
                        Buffer.BlockCopy(id, 0, clientsPosition, 1 + j, 4);
                        Buffer.BlockCopy(x, 0, clientsPosition, 5 + j, 4);
                        Buffer.BlockCopy(y, 0, clientsPosition, 9 + j, 4);
                        Buffer.BlockCopy(z, 0, clientsPosition, 13 + j, 4);
                        Buffer.BlockCopy(rotY, 0, clientsPosition, 17 + j, 4);
                        j += 20;
                    }
                }
                transport.Send(clientsPosition, item.Key);
            }
        }
        public void KeepAlive()
        {
            keepAliveTimer += 0.005f;
            if (keepAliveTimer >= 3)
            {
                foreach (var item in ClientAlive)
                {
                    if (item.Value == 0)
                    {
                        foreach (var item2 in ClientsTable)
                        {
                            if (item.Key != item2.Value)
                            {
                                byte[] DestroyThisPlayer = new byte[5];
                                DestroyThisPlayer[0] = 88;
                                byte[] id = BitConverter.GetBytes(item.Key);
                                Buffer.BlockCopy(id, 0, DestroyThisPlayer, 1, 4);
                                transport.Send(DestroyThisPlayer, item2.Key);
                            }
                            else
                            {
                                PlayerToDestroyEndPoint = item2.Key;
                            }
                        }
                        PlayerToDestroyID = item.Key;
                        DictionaryRemover = true;
                    }
                }
                if (DictionaryRemover)
                {
                    ClientAlive.Remove(PlayerToDestroyID);
                    ClientsTable.Remove(PlayerToDestroyEndPoint);
                    ClientPosition.Remove(PlayerToDestroyID);
                    //idCounter--;
                    if (OneShotReady)
                        ReadyCounter--;
                    DictionaryRemover = false;
                }
                foreach (var item in ClientsTable)
                {
                    ClientAlive[item.Value] = 0;
                    byte[] keepAlive = new byte[1];
                    keepAlive[0] = 18;
                    transport.Send(keepAlive, item.Key);
                }
                keepAliveTimer = 0;
            }
        }

        public void CheckCollision()
        {
            if (ClientsTable.Count > 1)
            {
                foreach (var item in ClientPosition)
                {
                    Vector3 collider1 = new Vector3(item.Value[0], item.Value[1], item.Value[2]);
                    foreach (var item2 in ClientsTable)
                    {
                        if (item.Key != item2.Value)
                        {
                            Vector3 collider2 = new Vector3(ClientPosition[item2.Value][0], ClientPosition[item2.Value][1], ClientPosition[item2.Value][2]);
                            float distance = Vector3.Distance(collider1, collider2);
                            Vector3 Distance = collider2 - collider1;
                            if (distance <= 0.6f)
                            {
                                byte[] CollisionImpact = new byte[13];
                                CollisionImpact[0] = 200;
                                byte[] x = BitConverter.GetBytes(Distance.X);
                                byte[] y = BitConverter.GetBytes(Distance.Y);
                                byte[] z = BitConverter.GetBytes(Distance.Z);
                                Buffer.BlockCopy(x, 0, CollisionImpact, 1, 4);
                                Buffer.BlockCopy(y, 0, CollisionImpact, 5, 4);
                                Buffer.BlockCopy(z, 0, CollisionImpact, 9, 4);
                                transport.Send(CollisionImpact, item2.Key);
                            }
                        }
                    }
                }
            }
        }

        public void SingleStep()
        {
            EndPoint sender = transport.CreateEndPoint();
            byte[] data = transport.Recv(256, ref sender);

            if (data != null)
            {
                byte gameCommand = data[0];
                if (commandsTable.ContainsKey(gameCommand))
                    commandsTable[gameCommand](data, sender);
                else
                {
                    throw new ServerException("Invalid GameCommand");
                }
            }
        }
    }
}

