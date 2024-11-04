using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using UnityEngine;
using System;

public class Network : MonoBehaviour
{
    bool bHost = false; // 서버 플래그
    bool bConnect = false; // 접속 플래그

    Socket socketListen = null; // 리스닝 소켓
    Socket socket = null; // 클라이언트 접속용 소켓

    Thread thread = null;
    bool bThreadBegin = false;

    Buffer bufferSend; // 송신 버퍼
    Buffer bufferReceive; // 수신 버퍼

    private string playerName;

    public string user;

    private const char MESSAGE_SEPARATOR = '|';  // 메시지 타입과 데이터를 구분하는 문자

    public delegate void EventHandler(NetEventState state);
    private EventHandler m_handler;

    public string PlayerName
    {
        get { return playerName; }
        set { playerName = value; }
    }

    void Start()
    {
        bufferSend = new Buffer();
        bufferReceive = new Buffer();

        // CharDataManager에서 이름 가져오기
        if (CharDataManager.instance != null)
        {
            playerName = CharDataManager.instance.PlayerName;
        }
    }

    public void HostStart(int port, int backlog=10)
    {
        socketListen = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
        socketListen.Bind(ep);
        socketListen.Listen(backlog);

        bHost = true;
        Debug.Log("Host Start");

        StartThread();
    }

    public bool IsHost()
    {
        return bHost;
    }

    bool StartThread()
    {
        ThreadStart threadDelegate = new ThreadStart(ThreadProc);
        thread = new Thread(threadDelegate);
        thread.Start();

        bThreadBegin = true;

        return true;
    }

    public void ThreadProc()
    {
        while (bThreadBegin)
        {
            AcceptGuest();

            if (socket != null && bConnect == true)
            {
                SendUpdate();
                ReceiveUpdate();
            }

            Thread.Sleep(10);
        }
    }

    public void StopServer()
    {
        bThreadBegin = false;
        if(thread != null)
        {
            thread.Join();
            thread = null;
        }

        Disconnect();

        if(socketListen != null)
        {
            socketListen.Close();
            socketListen = null;
        }

        bHost = false;

        Debug.Log("Server stopped.");
    }

    public void GuestStart(string address, int port)
    {
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        socket.Connect(address, port);

        bConnect= true;
        Debug.Log("Guest Start");

        if (m_handler != null)
        {
            NetEventState state = new NetEventState();
            state.type = NetEnvetType.Connect;
            state.result = (bConnect == true) ? NetVEventResult.Sussess : NetVEventResult.Fail;
            m_handler(state);
            Debug.Log("event handler called");
        }

        StartThread();
    }

    void AcceptGuest()
    {
        if (socketListen != null && socketListen.Poll(0, SelectMode.SelectRead))
        {
            socket = socketListen.Accept();
            bConnect = true;

            if (m_handler != null)
            {
                NetEventState state = new NetEventState();
                state.type = NetEnvetType.Connect;
                state.result = (bConnect == true) ? NetVEventResult.Sussess : NetVEventResult.Fail;
                m_handler(state);
            }

            Debug.Log("Guest Connect");
        }
    }

    public bool IsConnect()
    {
        return bConnect;
    }

    public int Send(byte[] bytes, int length)
    {
        return bufferSend.Write(bytes, length);
    }

    public int Receive(ref byte[] bytes, int length)
    {
        return bufferReceive.Read(ref bytes, length);
    }

    public void RegisterEventHandler(EventHandler handler)
    {
        m_handler += handler;
    }

    public void UnregisterEventHandler(EventHandler handler)
    {
        m_handler -= handler;
    }

    void SendUpdate()
    {
        if (socket.Poll(0, SelectMode.SelectWrite))
        {
            byte[] bytes = new byte[1024];

            int length = bufferSend.Read(ref bytes, bytes.Length);
            while (length > 0)
            {
                socket.Send(bytes, length, SocketFlags.None);
                length = bufferSend.Read(ref bytes, bytes.Length);
            }
        }
    }

    void ReceiveUpdate()
    {
        while (socket.Poll(0, SelectMode.SelectRead))
        {
            byte[] bytes = new byte[1024];

            int length = socket.Receive(bytes, bytes.Length, SocketFlags.None);
            if (length > 0)
            {
                bufferReceive.Write(bytes, length);
            }
        }
    }

    public void SendMessage(MessageType type, string data)
    {
        // "타입|데이터" 형식으로 메시지 구성
        string message = $"{(int)type}{MESSAGE_SEPARATOR}{data}";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(message);
        Send(bytes, bytes.Length);
    }

    public NetworkMessage ReceiveMessage(ref byte[] bytes, int length)
    {
        int receivedLength = Receive(ref bytes, length);
        if (receivedLength > 0)
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes, 0, receivedLength).TrimEnd('\0');

            // 구분자로 분리
            string[] parts = message.Split(MESSAGE_SEPARATOR);
            if (parts.Length >= 2)
            {
                try
                {
                    MessageType type = (MessageType)int.Parse(parts[0]);
                    string data = parts[1];
                    return new NetworkMessage(type, data);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to parse message: {e.Message}");
                    return null;
                }
            }
        }
        return null;
    }

    public void Disconnect()
    {
        bConnect = false;

        if(socket != null)
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket = null;

           if(m_handler != null)
            {
                NetEventState state  = new NetEventState();
                state.type = NetEnvetType.Disconnect;
                state.result = NetVEventResult.Sussess;
                m_handler(state);
            }
        }
    }
}
