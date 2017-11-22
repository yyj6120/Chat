using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
    TransportTCP transport;
    private ChatState state = ChatState.HOST_TYPE_SELECT;
    private const int port = 50765;

    private string chatMessage = "";
    private List<string>[] message;

    private bool isServer = false;
    private static int CHAT_MEMBER_NUM = 2;
    private static int MESSAGE_LINE = 18;

    public InputField hostAddress;
    // Use this for initialization
    void Start()
    {
        GameObject network = new GameObject("Network");
        transport = network.AddComponent<TransportTCP>();
        transport.RegisterEventHandler(OnEventHandling);

        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        System.Net.IPAddress hostAddress = hostEntry.AddressList[0];

        message = new List<string>[CHAT_MEMBER_NUM];
        for (int i = 0; i < CHAT_MEMBER_NUM; ++i)
        {
            message[i] = new List<string>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case ChatState.HOST_TYPE_SELECT:
                for (int i = 0; i < CHAT_MEMBER_NUM; ++i)
                    message[i].Clear();
                break;
            case ChatState.CHATTING:
                UpdateChatting();
                break;
            case ChatState.LEAVE:
                UpdateLeave();
                break;
        }
    }

    void UpdateChatting()
    {
        byte[] buffer = new byte[1400];

        int recvSize = transport.Receive(ref buffer, buffer.Length);
        if (recvSize > 0)
        {
            string recvMessage = System.Text.Encoding.UTF8.GetString(buffer);
            Debug.Log("Recv data:" + recvMessage);
            chatMessage += recvMessage + "   ";

            int id = (isServer == true) ? 1 : 0;
            AddMessage(ref message[id], recvMessage);
        }
    }

    void AddMessage(ref List<string> messages, string str)
    {
        while (messages.Count >= MESSAGE_LINE)
            messages.RemoveAt(0);

        messages.Add(str);
    }

    void UpdateLeave()
    {
        if (isServer == true)
            transport.StopServer();
        else
            transport.Disconnect();

        // 메시지 삭제.
        for (int i = 0; i < 2; ++i)
            message[i].Clear();

        state = ChatState.HOST_TYPE_SELECT;
    }

    public void CreateChattingRoom()
    {
        transport.StartServer(port, 1);
        state = ChatState.CHATTING;
        isServer = true;
    }

    public void ConnetChattingRoom()
    {
        bool ret = transport.Connect(hostAddress.text, port);
        if (ret)
            state = ChatState.CHATTING;
        else
            state = ChatState.ERROR;
    }

    public void OnEventHandling(NetEventState state)
    {
        switch (state.type)
        {
            case NetEventType.Connect:
                if (transport.IsServer())
                    AddMessage(ref message[1], "콩장수가 입장했습니다.");
                else
                    AddMessage(ref message[0], "두부장수와 이야기할 수 있습니다.");
                break;

            case NetEventType.Disconnect:
                if (transport.IsServer())
                    AddMessage(ref message[0], "콩장수가 나갔습니다.");
                else
                    AddMessage(ref message[1], "콩장수가 나갔습니다.");
                break;
        }
    }
}
