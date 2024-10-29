using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Chat : MonoBehaviour
{
    Network network;
    public TMP_InputField id;
    public TMP_InputField chat;
    List<string> list;
    Dictionary<string, GameObject> ChatDict = new Dictionary<string, GameObject>();
    public GameObject hostChat;
    public GameObject guestChat;
    public RectTransform chatContainer;
    public Image backUI;

    void Start()
    {
        network = GetComponent<Network>();
        list = new List<string>();
    }

    public void BeginHost()
    {
        network.HostStart(10000, 10);
        network.name = id.text;
    }

    public void BeginGuest()
    {
        network.GuestStart("127.0.0.1", 10000);
        network.name = id.text;
    }

    void Update()
    {
        if (network != null && network.IsConnect())
        {
            // 연결 상태 확인 후 메시지 수신
            if (network != null && network.IsConnect())
            {
                byte[] bytes = new byte[1024];
                int length = network.Receive(ref bytes, bytes.Length);

                if (length > 0)
                {
                    string str = System.Text.Encoding.UTF8.GetString(bytes);
                    AddTalk(str, false);
                }
                UpdateUI();
            }
        }
    }

    void AddTalk(string str, bool isSent)
    {
        while (list.Count >= 3)
        {
            if (ChatDict.ContainsKey(list[0]))
            {
                Destroy(ChatDict[list[0]]);
                ChatDict.Remove(list[0]);
            }
            list.RemoveAt(0);
        }
        list.Add(str);
        GameObject chatTalk;
        if ((network.IsHost() && isSent) || (!network.IsHost() && isSent))
        {
            chatTalk = Instantiate(hostChat, chatContainer);
        }
        else
        {
            chatTalk = Instantiate(guestChat, chatContainer);
        }
        TextMeshProUGUI chatText = chatTalk.GetComponentInChildren<TextMeshProUGUI>();
        if (chatText != null)
        {
            chatText.text = str;
        }
        ChatDict[str] = chatTalk;
    }
    public void SendTalk()
    {
        string str = network.name + ": " + chat.text;
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
        network.Send(bytes, bytes.Length);
        AddTalk(str, true);
        chat.text = "";
    }

    void UpdateUI()
    {
        if (!backUI.IsActive())
        {
            backUI.gameObject.SetActive(true);
        }
    }
}