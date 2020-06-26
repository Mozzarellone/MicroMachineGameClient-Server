using System;
using System.Collections.Generic;
using System.Net;
namespace MicroMachine_22_05_2019
{
    public interface ITransport
    {
        void Bind(string address, int port);
        bool Send(byte[] data, EndPoint endPoint);
        //void Send(byte[] data, EndPoint endPoint);
        byte[] Recv(int bufferSize, ref EndPoint sender);
        EndPoint CreateEndPoint();
    }
}
