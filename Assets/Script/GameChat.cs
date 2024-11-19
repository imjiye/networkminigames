using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameChat : MonoBehaviour
{
    Network network;
    public TMP_InputField chatInput;         // ä�� �Է� �ʵ�

    // ȣ��Ʈ�� �Խ�Ʈ�� ä�� �ؽ�Ʈ
    public GameObject hostChatPrefab;        // ȣ��Ʈ ä�� �ؽ�Ʈ ������
    public GameObject guestChatPrefab;       // �Խ�Ʈ ä�� �ؽ�Ʈ ������

    private GameObject currentHostChat;       // ���� ǥ������ ȣ��Ʈ ä��
    private GameObject currentGuestChat;      // ���� ǥ������ �Խ�Ʈ ä��

    private CharacterSpawn characterSpawn;    // ĳ���� ���� ����
    private float chatDisplayTime = 5f;       // ä�� ǥ�� �ð�
    private Vector3 chatOffset = new Vector3(0, 2.0f, 0); // ĳ���� �Ӹ� �� ������

    void Awake()
    {
        Debug.Log("[GameChat] Awake called");
        if (NetworkManager.Instance != null)
        {
            network = NetworkManager.Instance.GetNetwork();
        }
        else
        {
            Debug.LogError("[GameChat] NetworkManager.Instance is null!");
        }

        // CharacterSpawn ������Ʈ ã��
        characterSpawn = FindObjectOfType<CharacterSpawn>();
        if (characterSpawn == null)
        {
            Debug.LogError("[GameChat] CharacterSpawn not found!");
        }
    }

    void Start()
    {
        Debug.Log("[GameChat] Start called");

        // ä�� �Է� �ʵ� ����
        if (chatInput != null)
        {
            chatInput.onEndEdit.AddListener(OnEndEdit);
            Debug.Log("[GameChat] Chat input field initialized");
        }
        else
        {
            Debug.LogError("[GameChat] Chat input field is not assigned!");
        }

        // ������ üũ
        if (hostChatPrefab == null)
            Debug.LogError("[GameChat] Host chat prefab is not assigned!");
        if (guestChatPrefab == null)
            Debug.LogError("[GameChat] Guest chat prefab is not assigned!");
    }

    // ����Ű �Է� ó��
    private void OnEndEdit(string text)
    {
        if (!string.IsNullOrEmpty(text) &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            Debug.Log("[GameChat] Enter key pressed with text: " + text);
            SendChat();
        }
    }

    void Update()
    {
        if (network == null)
        {
            network = NetworkManager.Instance?.GetNetwork();
            return;
        }

        if (!network.IsConnect()) return;

        // ����Ű ó�� �߰�
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!chatInput.isFocused)
            {
                chatInput.ActivateInputField();
            }
        }

        // �޽��� ���� ó��
        byte[] bytes = new byte[1024];
        NetworkMessage message = network.ReceiveMessage(ref bytes, bytes.Length);

        if (message != null && message.type == MessageType.Chat)
        {
            Debug.Log("[GameChat] Received chat message: " + message.data);
            DisplayReceivedChat(message.data);
        }
    }

    // ä�� ���� �Լ�
    public void SendChat()
    {
        Debug.Log("[GameChat] SendChat called");

        // �Է� �ʵ尡 ��Ȱ��ȭ�Ǿ� �ְų� �ؽ�Ʈ�� ����ִ� ��� ó��
        if (chatInput == null)
        {
            Debug.LogError("[GameChat] Chat input field is null");
            return;
        }

        if (string.IsNullOrEmpty(chatInput.text))
        {
            Debug.Log("[GameChat] Chat input is empty");
            return;
        }

        if (network == null || string.IsNullOrEmpty(network.PlayerName))
        {
            Debug.LogError("[GameChat] Network or player name is null");
            return;
        }

        string chatMessage = network.PlayerName + ": " + chatInput.text;
        Debug.Log("[GameChat] Sending message: " + chatMessage);

        network.SendMessage(MessageType.Chat, chatMessage);
        DisplayLocalChat(chatMessage);

        // �Է� �ʵ� �ʱ�ȭ �� ��Ŀ�� ó��
        string currentText = chatInput.text;
        chatInput.text = "";

        // ���� �����ӿ��� �Է� �ʵ� Ȱ��ȭ
        StartCoroutine(ReactivateInputField());
    }

    // �Է� �ʵ� ��Ȱ��ȭ�� ���� �ڷ�ƾ �߰�
    private IEnumerator ReactivateInputField()
    {
        yield return new WaitForEndOfFrame();
        if (chatInput != null)
        {
            chatInput.ActivateInputField();
            chatInput.Select();
        }
    }

    // ���ŵ� ä�� ǥ��
    private void DisplayReceivedChat(string message)
    {
        Debug.Log("[GameChat] DisplayReceivedChat: " + message);

        if (!message.Contains(":"))
        {
            Debug.LogError("[GameChat] Invalid message format");
            return;
        }

        GameObject[] characters = network.IsHost() ?
            characterSpawn.GetSpawnedGuestCharacters() :
            characterSpawn.GetSpawnedHostCharacters();

        if (characters.Length == 0 || characters[0] == null)
        {
            Debug.LogError("[GameChat] No character found to display chat");
            return;
        }

        // ���� ä�� ����
        if (network.IsHost())
        {
            if (currentGuestChat != null) Destroy(currentGuestChat);
        }
        else
        {
            if (currentHostChat != null) Destroy(currentHostChat);
        }

        // �� ä�� ����
        Vector3 characterHead = characters[0].transform.position + chatOffset;
        GameObject chatObj = Instantiate(
            network.IsHost() ? guestChatPrefab : hostChatPrefab,
            characterHead,
            Quaternion.identity
        );

        // �ؽ�Ʈ ����
        TextMeshProUGUI chatText = chatObj.GetComponentInChildren<TextMeshProUGUI>();
        if (chatText != null)
        {
            chatText.text = message.Split(':')[1].Trim();
            Debug.Log("[GameChat] Chat text set: " + chatText.text);
        }
        else
        {
            Debug.LogError("[GameChat] TextMeshProUGUI component not found on chat prefab");
        }

        // ä�� ������Ʈ ����
        if (network.IsHost())
            currentGuestChat = chatObj;
        else
            currentHostChat = chatObj;

        // ���� �ð� �� ����
        StartCoroutine(RemoveChatAfterDelay(chatObj));
    }

    // ���� ä�� ǥ��
    private void DisplayLocalChat(string message)
    {
        Debug.Log("[GameChat] DisplayLocalChat: " + message);

        GameObject[] characters = network.IsHost() ?
            characterSpawn.GetSpawnedHostCharacters() :
            characterSpawn.GetSpawnedGuestCharacters();

        if (characters.Length == 0 || characters[0] == null)
        {
            Debug.LogError("[GameChat] No local character found to display chat");
            return;
        }

        // ���� ä�� ����
        if (network.IsHost())
        {
            if (currentHostChat != null) Destroy(currentHostChat);
        }
        else
        {
            if (currentGuestChat != null) Destroy(currentGuestChat);
        }

        // �� ä�� ����
        Vector3 characterHead = characters[0].transform.position + chatOffset;
        GameObject chatObj = Instantiate(
            network.IsHost() ? hostChatPrefab : guestChatPrefab,
            characterHead,
            Quaternion.identity
        );

        // �ؽ�Ʈ ����
        TextMeshProUGUI chatText = chatObj.GetComponentInChildren<TextMeshProUGUI>();
        if (chatText != null)
        {
            chatText.text = message.Split(':')[1].Trim();
            Debug.Log("[GameChat] Local chat text set: " + chatText.text);
        }
        else
        {
            Debug.LogError("[GameChat] TextMeshProUGUI component not found on chat prefab");
        }

        // ä�� ������Ʈ ����
        if (network.IsHost())
            currentHostChat = chatObj;
        else
            currentGuestChat = chatObj;

        // ���� �ð� �� ����
        StartCoroutine(RemoveChatAfterDelay(chatObj));
    }

    // ä�� ���� �ڷ�ƾ
    private IEnumerator RemoveChatAfterDelay(GameObject chatObj)
    {
        yield return new WaitForSeconds(chatDisplayTime);
        if (chatObj != null)
        {
            Destroy(chatObj);
        }
    }
}