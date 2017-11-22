using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
public class TransportTCP : MonoBehaviour
{
    //
    // 소켓 접속 관련.
    //
    // 리스닝 소켓.
    private Socket listener = null;

    // 클라이언트와의 접속용 소켓.
    private Socket socket = null;

    // 송신 버퍼.
    private PacketQueue sendQueue;

    // 수신 버퍼.
    private PacketQueue recvQueue;

    // 서버 플래그.	
    private bool isServer = false;

    // 접속 플래그.
    private bool isConnected = false;

    //
    // 이벤트 관련 멤버 변수.
    //
    // 이벤트 통지의 델리게이트.
    public delegate void EventHandler(NetEventState state);
    private EventHandler handler;

    //
    // 스레드 관련 멤버 변수.
    //
    // 스레드 실행 플래그.
    protected bool m_threadLoop = false;

    protected Thread m_thread = null;

    private static int s_mtu = 1400;

    // Use this for initialization
    void Start()
    {

        // 송수신 버퍼를 작성합니다. 
        sendQueue = new PacketQueue();
        recvQueue = new PacketQueue();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // 대기 시작.
    public bool StartServer(int port, int connectionNum)
    {
        Debug.Log("StartServer called.!");
        // 리스닝 소켓을 생성합니다.
        try
        {
            // 소켓을 생성합니다.
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 사용할 포트 번호를 할당합니다.
            listener.Bind(new IPEndPoint(IPAddress.Any, port));
            // 대기를 시작합니다.
            listener.Listen(connectionNum);
        }
        catch
        {
            Debug.Log("StartServer fail");
            return false;
        }
        isServer = true;
        return LaunchThread();
    }

    // 대기 종료.
    public void StopServer()
    {
        m_threadLoop = false;
        if (m_thread != null)
        {
            m_thread.Join();
            m_thread = null;
        }

        Disconnect();

        if (listener != null)
        {
            listener.Close();
            listener = null;
        }
        isServer = false;
        Debug.Log("Server stopped.");
    }

    // 접속.
    public bool Connect(string address, int port)
    {
        Debug.Log("TransportTCP connect called.");
        if (listener != null)
        {
            return false;
        }
        bool ret = false;
        try
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = true;
            socket.Connect(address, port);
            ret = LaunchThread();
        }
        catch
        {
            socket = null;
        }

        if (ret == true)
        {
            isConnected = true;
            Debug.Log("Connection success.");
        }
        else
        {
            isConnected = false;
            Debug.Log("Connect fail");
        }

        if (handler != null)
        {
            // 접속 결과를 통지합니다. 
            NetEventState state = new NetEventState();
            state.type = NetEventType.Connect;
            state.result = (isConnected == true) ? NetEventResult.Success : NetEventResult.Failure;
            handler(state);
            Debug.Log("event handler called");
        }

        return isConnected;
    }

    // 끊기. 
    public void Disconnect()
    {
        isConnected = false;

        if (socket != null)
        {
            // 소켓 클로즈.
            socket.Shutdown(SocketShutdown.Both);
            socket.Close();
            socket = null;
        }
        // 끊김을 통지합니다.
        if (handler != null)
        {
            NetEventState state = new NetEventState();
            state.type = NetEventType.Disconnect;
            state.result = NetEventResult.Success;
            handler(state);
        }
    }
    // 송신처리.
    public int Send(byte[] data, int size)
    {
        if (sendQueue == null)
            return 0;

        return sendQueue.Enqueue(data, size);
    }

    // 수신처리.
    public int Receive(ref byte[] buffer, int size)
    {
        if (recvQueue == null)
            return 0;

        return recvQueue.Dequeue(ref buffer, size);
    }

    // 이벤트 통지 함수 등록.
    public void RegisterEventHandler(EventHandler handler)
    {
        this.handler += handler;
    }

    // 이벤트 통지 함수 삭제.
    public void UnregisterEventHandler(EventHandler handler)
    {
        this.handler -= handler;
    }

    // 스레드 시작 함수.
    bool LaunchThread()
    {
        try
        {
            // Dispatch용 스레드 시작.
            m_threadLoop = true;
            m_thread = new Thread(new ThreadStart(Dispatch));
            m_thread.Start();
        }
        catch
        {
            Debug.Log("Cannot launch thread.");
            return false;
        }

        return true;
    }

    // 스레드 측 송수신 처리.
    public void Dispatch()
    {
        Debug.Log("Dispatch thread started.");

        while (m_threadLoop)
        {
            // 클라이언트로부터의 접속을 기다립니다.
            AcceptClient();

            // 클라이언트와의 송수신 처리를 합니다.
            if (socket != null && isConnected == true)
            {
                // 송신처리.
                DispatchSend();
                // 수신처리.
                DispatchReceive();
            }

            Thread.Sleep(5);
        }
        Debug.Log("Dispatch thread ended.");
    }
    // 클라리언트 접속.
    void AcceptClient()
    {
        if (listener != null && listener.Poll(0, SelectMode.SelectRead))
        {
            // 클라이언트가 접속했습니다.
            socket = listener.Accept();
            isConnected = true;
            NetEventState state = new NetEventState();
            state.type = NetEventType.Connect;
            state.result = NetEventResult.Success;
            handler(state);
            Debug.Log("Connected from client.");
        }
    }
    // 스레트 측 송신처리.
    void DispatchSend()
    {
        try
        {
            // 송신처리.
            if (socket.Poll(0, SelectMode.SelectWrite))
            {
                byte[] buffer = new byte[s_mtu];

                int sendSize = sendQueue.Dequeue(ref buffer, buffer.Length);
                while (sendSize > 0)
                {
                    socket.Send(buffer, sendSize, SocketFlags.None);
                    sendSize = sendQueue.Dequeue(ref buffer, buffer.Length);
                }
            }
        }
        catch
        {
            return;
        }
    }

    // 스레드 측 수신처리.
    void DispatchReceive()
    {
        // 수신처리.
        try
        {
            while (socket.Poll(0, SelectMode.SelectRead))
            {
                byte[] buffer = new byte[s_mtu];

                int recvSize = socket.Receive(buffer, buffer.Length, SocketFlags.None);
                if (recvSize == 0)
                {
                    // 끊기.
                    Debug.Log("Disconnect recv from client.");
                    Disconnect();
                }
                else if (recvSize > 0)
                    recvQueue.Enqueue(buffer, recvSize);
            }
        }
        catch
        {
            return;
        }
    }

    // 서버인지 확인.
    public bool IsServer()
    {
        return isServer;
    }

    // 접속 확인.
    public bool IsConnected()
    {
        return isConnected;
    }
}
