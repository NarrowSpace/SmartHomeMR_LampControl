using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class SocketClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private byte[] buffer = new byte[1024];

    public int PORT = 8010;
    public string RASP_IP = "10.0.0.193";

    void Start()
    {
        client = new TcpClient(RASP_IP, PORT);
        stream = client.GetStream();
        stream.BeginRead(buffer, 0, buffer.Length, OnReceive, null);
    }

    private void OnReceive(IAsyncResult ar)
    {
        int bytesRead = stream.EndRead(ar);

        if (bytesRead > 0)
        {
            string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Debug.Log(data);

            // Begin another read
            stream.BeginRead(buffer, 0, buffer.Length, OnReceive, null);
        }
    }

    void OnDestroy()
    {
        stream.Close();
        client.Close();
    }
}
