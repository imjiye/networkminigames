using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class GameChat : MonoBehaviour
{
    Network network;
    public TMP_InputField chatInput;         // 채팅 입력 필드

    // 호스트와 게스트의 채팅 텍스트
    public GameObject hostChatPrefab;        // 호스트 채팅 텍스트 프리팹
    public GameObject guestChatPrefab;       // 게스트 채팅 텍스트 프리팹

    private GameObject currentHostChat;       // 현재 표시중인 호스트 채팅
    private GameObject currentGuestChat;      // 현재 표시중인 게스트 채팅

    private CharacterSpawn characterSpawn;    // 캐릭터 스폰 참조
    private float chatDisplayTime = 5f;       // 채팅 표시 시간
    private Vector3 chatOffset = new Vector3(0, 2.0f, 0); // 캐릭터 머리 위 오프셋

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

        // CharacterSpawn 컴포넌트 찾기
        characterSpawn = FindObjectOfType<CharacterSpawn>();
        if (characterSpawn == null)
        {
            Debug.LogError("[GameChat] CharacterSpawn not found!");
        }
    }

    void Start()
    {
        Debug.Log("[GameChat] Start called");

        // 채팅 입력 필드 설정
        if (chatInput != null)
        {
            chatInput.onEndEdit.AddListener(OnEndEdit);
            Debug.Log("[GameChat] Chat input field initialized");
        }
        else
        {
            Debug.LogError("[GameChat] Chat input field is not assigned!");
        }

        // 프리팹 체크
        if (hostChatPrefab == null)
            Debug.LogError("[GameChat] Host chat prefab is not assigned!");
        if (guestChatPrefab == null)
            Debug.LogError("[GameChat] Guest chat prefab is not assigned!");
    }

    // 엔터키 입력 처리
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

        // 엔터키 처리 추가
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!chatInput.isFocused)
            {
                chatInput.ActivateInputField();
            }
        }

        // 메시지 수신 처리
        byte[] bytes = new byte[1024];
        NetworkMessage message = network.ReceiveMessage(ref bytes, bytes.Length);

        if (message != null && message.type == MessageType.Chat)
        {
            Debug.Log("[GameChat] Received chat message: " + message.data);
            DisplayReceivedChat(message.data);
        }
    }

    // 채팅 전송 함수
    public void SendChat()
    {
        Debug.Log("[GameChat] SendChat called");

        // 입력 필드가 비활성화되어 있거나 텍스트가 비어있는 경우 처리
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

        // 입력 필드 초기화 및 포커스 처리
        string currentText = chatInput.text;
        chatInput.text = "";

        // 다음 프레임에서 입력 필드 활성화
        StartCoroutine(ReactivateInputField());
    }

    // 입력 필드 재활성화를 위한 코루틴 추가
    private IEnumerator ReactivateInputField()
    {
        yield return new WaitForEndOfFrame();
        if (chatInput != null)
        {
            chatInput.ActivateInputField();
            chatInput.Select();
        }
    }

    // 수신된 채팅 표시
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

        // 이전 채팅 제거
        if (network.IsHost())
        {
            if (currentGuestChat != null) Destroy(currentGuestChat);
        }
        else
        {
            if (currentHostChat != null) Destroy(currentHostChat);
        }

        // 새 채팅 생성
        Vector3 characterHead = characters[0].transform.position + chatOffset;
        GameObject chatObj = Instantiate(
            network.IsHost() ? guestChatPrefab : hostChatPrefab,
            characterHead,
            Quaternion.identity
        );

        // 텍스트 설정
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

        // 채팅 오브젝트 저장
        if (network.IsHost())
            currentGuestChat = chatObj;
        else
            currentHostChat = chatObj;

        // 일정 시간 후 제거
        StartCoroutine(RemoveChatAfterDelay(chatObj));
    }

    // 로컬 채팅 표시
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

        // 이전 채팅 제거
        if (network.IsHost())
        {
            if (currentHostChat != null) Destroy(currentHostChat);
        }
        else
        {
            if (currentGuestChat != null) Destroy(currentGuestChat);
        }

        // 새 채팅 생성
        Vector3 characterHead = characters[0].transform.position + chatOffset;
        GameObject chatObj = Instantiate(
            network.IsHost() ? hostChatPrefab : guestChatPrefab,
            characterHead,
            Quaternion.identity
        );

        // 텍스트 설정
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

        // 채팅 오브젝트 저장
        if (network.IsHost())
            currentHostChat = chatObj;
        else
            currentGuestChat = chatObj;

        // 일정 시간 후 제거
        StartCoroutine(RemoveChatAfterDelay(chatObj));
    }

    // 채팅 제거 코루틴
    private IEnumerator RemoveChatAfterDelay(GameObject chatObj)
    {
        yield return new WaitForSeconds(chatDisplayTime);
        if (chatObj != null)
        {
            Destroy(chatObj);
        }
    }
}