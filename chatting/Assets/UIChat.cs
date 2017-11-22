using UnityEngine;
using UnityEngine.UI;
using System;

public class UIChat : MonoBehaviour
{
    public Chat chat;
    public GameObject panel;
    public InputField messageInput;
    public Button sendButton;
    public Transform content;
    public ScrollRect scrollRect;
    public GameObject textPrefab;
    public KeyCode[] activationKeys = { KeyCode.Return, KeyCode.KeypadEnter };
    public int keepHistory = 100; // only keep 'n' messages

    void Update()
    {

        if (Utils.AnyKeyUp(activationKeys))
            messageInput.Select();

        messageInput.onEndEdit.SetListener((value) =>
        {
            // submit key pressed? then submit and set new input text
            if (Utils.AnyKeyDown(activationKeys))
            {
                string message = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + messageInput.text;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
                chat.transport.Send(buffer, buffer.Length);
                AddMessage(message); 
                messageInput.text = "";
                messageInput.MoveTextEnd(false);
            }

            UIUtils.DeselectCarefully();
        });

        sendButton.onClick.SetListener(() =>
        {
            string message = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + messageInput.text;
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(message);
            chat.transport.Send(buffer, buffer.Length);
            AddMessage(message);
            messageInput.text = "";
            messageInput.MoveTextEnd(false);
        });
    }

    public void AddMessage(string msg)
    {
        if (content.childCount >= keepHistory)
        {
            for (int i = 0; i < content.childCount / 2; ++i)
                Destroy(content.GetChild(i).gameObject);
        }
        // instantiate and initialize text prefab
        var go = Instantiate(textPrefab);
        go.transform.SetParent(content.transform, false);
        go.GetComponent<Text>().text = msg;
    }

    void AutoScroll()
    {
        // update first so we don't ignore recently added messages, then scroll
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0;
    }
}
