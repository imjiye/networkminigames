using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Chat : MonoBehaviour
{
    Network network;
    public TMP_InputField chat;
    List<string> list;
    Dictionary<string, GameObject> ChatDict = new Dictionary<string, GameObject>();
    public GameObject hostChat;
    public GameObject guestChat;
    public RectTransform chatContainer;
    public Image backUI;
    public GameObject chatbox;

    void Awake()
    {
        // NetworkManager를 통해 Network 컴포넌트 참조 가져오기
        if (NetworkManager.Instance != null)
        {
            network = NetworkManager.Instance.GetNetwork();
        }
        else
        {
            Debug.LogError("NetworkManager instance not found!");
        }
    }

    void Start()
    {
        list = new List<string>();

        // 채팅 UI 초기 설정
        if (chatbox != null)
        {
            chatbox.SetActive(false);
        }
    }

    void Update()
    {
        if (network == null)
        {
            network = NetworkManager.Instance.GetNetwork();
            return;
        }

        if (network.IsConnect())
        {
            if (chatbox != null && !chatbox.activeSelf)
            {
                chatbox.SetActive(true);
            }

            byte[] bytes = new byte[1024];
            NetworkMessage message = network.ReceiveMessage(ref bytes, bytes.Length);

            if (message != null && message.type == MessageType.Chat)
            {
                AddTalk(message.data, false);
            }

            UpdateUI();
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
        if (chat == null || string.IsNullOrEmpty(chat.text))
            return;

        if (network == null || string.IsNullOrEmpty(network.PlayerName))
        {
            Debug.LogWarning("Network component or player name is not properly initialized");
            return;
        }

        string str = network.PlayerName + ": " + chat.text;
        network.SendMessage(MessageType.Chat, str);
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