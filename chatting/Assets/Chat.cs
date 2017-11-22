using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using UnityEngine.UI;
using System;
[Serializable]
public struct UIPanelInfo
{
    public ChatState state;
    public GameObject UIpanel;
}
public class Chat : MonoBehaviour
{
    public UIChat chattingwindow;
    public TransportTCP transport;
    private ChatState state = ChatState.HOST_TYPE_SELECT;
    private const int port = 50765;

    private bool isServer = false;

    public UIPanelInfo[] chatStatePanel;
    //public GameObject[] chatStatePanel;
    public InputField hostAddress;
    // Use this for initialization
    void Start()
    {
        GameObject network = new GameObject("Network");
        transport = network.AddComponent<TransportTCP>();
        transport.RegisterEventHandler(OnEventHandling);

        IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        System.Net.IPAddress hostAddress = hostEntry.AddressList[0];

        StartCoroutine(UIroutine());
    }
    IEnumerator UIroutine()
    {
        while(true)
        {
            state.UIchat(chatStatePanel);
            yield return new WaitForSeconds(0.02f);
        }
    }
    // Update is called once per frame
    void Update()
    {
        switch (state)
        {
            case ChatState.HOST_TYPE_SELECT:
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
            chattingwindow.AddMessage(recvMessage);
        }
    }
    void UpdateLeave()
    {
        if (isServer == true)
            transport.StopServer();
        else
            transport.Disconnect();

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
                    Debug.Log("서버입장");
                else
                    Debug.Log("고객");
                break;
            case NetEventType.Disconnect:
                if (transport.IsServer())
                    Debug.Log("서버가 나갔습니다.");
                else
                    Debug.Log("고객이 나갔습니다.");
                break;
        }
    }
}
