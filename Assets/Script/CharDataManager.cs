using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum Character
{
    ElfMan,
    ElfWoman,
    PlayerMan,
    PlayerWoman,
    WolfMan,
    WolfWoman
}

public enum UserRole { Host, Guest }

public class CharDataManager : MonoBehaviour
{
    public static CharDataManager instance;

    public Character curHostCharcter;
    public Character curGuestCharcter;
    public UserRole curUserRole;

    // 캐릭터 변경 이벤트
    public event Action<Character> OnHostCharacterChanged;
    public event Action<Character> OnGuestCharacterChanged;

    private Network network;
    private bool isNetworkInitialized = false;

    public Character CurHostCharcter
    {
        get { return curHostCharcter; }
        set
        {
            if (curHostCharcter != value)
            {
                curHostCharcter = value;
                OnHostCharacterChanged?.Invoke(curHostCharcter);
                // 호스트인 경우 게스트에게 변경사항 알림
                if (Role == UserRole.Host && isNetworkInitialized)
                {
                    network.SendMessage(MessageType.CharacterInfo, ((int)curHostCharcter).ToString());
                }
            }
        }
    }

    public Character CurGuestCharcter
    {
        get { return curGuestCharcter; }
        set
        {
            if (curGuestCharcter != value)
            {
                curGuestCharcter = value;
                OnGuestCharacterChanged?.Invoke(curGuestCharcter);
                // 게스트인 경우 호스트에게 변경사항 알림
                if (Role == UserRole.Guest && isNetworkInitialized)
                {
                    network.SendMessage(MessageType.GuestSelection, ((int)curGuestCharcter).ToString());
                }
            }
        }
    }

    public UserRole Role { get; set; }
    public string PlayerName { get; set; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializeNetwork());
        }
        else if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator InitializeNetwork()
    {
        while (NetworkManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        network = NetworkManager.Instance.GetNetwork();
        isNetworkInitialized = true;

        // 네트워크 메시지 수신 시작
        StartCoroutine(NetworkUpdateRoutine());
    }

    private IEnumerator NetworkUpdateRoutine()
    {
        while (true)
        {
            if (network != null && network.IsConnect())
            {
                byte[] bytes = new byte[1024];
                NetworkMessage message = network.ReceiveMessage(ref bytes, bytes.Length);

                if (message != null)
                {
                    ProcessNetworkMessage(message);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void ProcessNetworkMessage(NetworkMessage message)
    {
        if (message == null) return;

        switch (message.type)
        {
            case MessageType.CharacterInfo:
                if (Role == UserRole.Guest && int.TryParse(message.data, out int hostIndex))
                {
                    curHostCharcter = (Character)hostIndex;
                    OnHostCharacterChanged?.Invoke(curHostCharcter);
                    Debug.Log($"[CharDataManager] Received host character update: {curHostCharcter}");
                }
                break;

            case MessageType.GuestSelection:
                if (Role == UserRole.Host && int.TryParse(message.data, out int guestIndex))
                {
                    curGuestCharcter = (Character)guestIndex;
                    OnGuestCharacterChanged?.Invoke(curGuestCharcter);
                    Debug.Log($"[CharDataManager] Received guest character update: {curGuestCharcter}");
                }
                break;
        }
    }

    // 캐릭터 정보 상태 로깅을 위한 디버그 메서드
    public void LogCharacterState()
    {
        Debug.Log($"[CharDataManager] Current State:");
        Debug.Log($"Role: {Role}");
        Debug.Log($"Host Character: {curHostCharcter}");
        Debug.Log($"Guest Character: {curGuestCharcter}");
        Debug.Log($"Network Initialized: {isNetworkInitialized}");
    }
}