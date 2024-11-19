using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawn : MonoBehaviour
{
    public GameObject[] hostCharacterPrefabs;
    public GameObject[] guestCharacterPrefabs;
    private GameObject currentHostCharacter;
    private GameObject currentGuestCharacter;

    private Network network;

    private int currentHostIndex = -1;
    private int currentGuestIndex = -1;

    private bool isInitialized = false;
    private bool hasReceivedInitialData = false;

    public Transform hostSpawnPoint;
    public Transform guestSpawnPoint;

    void Awake()
    {
        Debug.Log("[CharacterSpawn] Awake called");

        if (hostSpawnPoint == null)
        {
            hostSpawnPoint = transform;
            Debug.LogWarning("[CharacterSpawn] Using default host spawn point");
        }
        if (guestSpawnPoint == null)
        {
            guestSpawnPoint = transform;
            Debug.LogWarning("[CharacterSpawn] Using default guest spawn point");
        }
    }

    void Start()
    {
        Debug.Log("[CharacterSpawn] Start called");
        StartCoroutine(InitializeNetwork());
    }

    private IEnumerator InitializeNetwork()
    {
        Debug.Log("[CharacterSpawn] Starting network initialization");

        // NetworkManager 대기
        while (NetworkManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        network = NetworkManager.Instance.GetNetwork();

        // CharDataManager 대기
        while (CharDataManager.instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // 연결 대기
        while (!network.IsConnect())
        {
            yield return new WaitForSeconds(0.1f);
        }

        // 초기 캐릭터 정보 설정
        if (network.IsHost())
        {
            currentHostIndex = (int)CharDataManager.instance.CurHostCharcter;
            SpawnHostCharacter(currentHostIndex);

            // 게스트 캐릭터 정보 요청
            network.SendMessage(MessageType.CharacterInfo, currentHostIndex.ToString());

            // 저장된 게스트 캐릭터 정보가 있다면 스폰
            if (CharDataManager.instance.CurGuestCharcter != 0)
            {
                currentGuestIndex = (int)CharDataManager.instance.CurGuestCharcter;
                SpawnGuestCharacter(currentGuestIndex);
            }
        }
        else
        {
            currentGuestIndex = (int)CharDataManager.instance.CurGuestCharcter;
            SpawnGuestCharacter(currentGuestIndex);

            // 호스트 캐릭터 정보 요청
            network.SendMessage(MessageType.GuestSelection, currentGuestIndex.ToString());

            // 저장된 호스트 캐릭터 정보가 있다면 스폰
            if (CharDataManager.instance.CurHostCharcter != 0)
            {
                currentHostIndex = (int)CharDataManager.instance.CurHostCharcter;
                SpawnHostCharacter(currentHostIndex);
            }
        }

        isInitialized = true;
        StartCoroutine(NetworkUpdateRoutine());
        StartCoroutine(WaitForInitialData());
    }

    private IEnumerator WaitForInitialData()
    {
        float timeout = 5f; // 5초 타임아웃
        float elapsed = 0f;

        while (!hasReceivedInitialData && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 타임아웃 후에도 데이터를 받지 못했다면 다시 요청
        if (!hasReceivedInitialData)
        {
            if (network.IsHost())
            {
                network.SendMessage(MessageType.CharacterInfo, currentHostIndex.ToString());
            }
            else
            {
                network.SendMessage(MessageType.GuestSelection, currentGuestIndex.ToString());
            }
        }
    }

    // 현재 스폰된 호스트 캐릭터 배열 반환
    public GameObject[] GetSpawnedHostCharacters()
    {
        if (currentHostCharacter != null)
        {
            return new GameObject[] { currentHostCharacter };
        }
        return new GameObject[0];
    }

    // 현재 스폰된 게스트 캐릭터 배열 반환
    public GameObject[] GetSpawnedGuestCharacters()
    {
        if (currentGuestCharacter != null)
        {
            return new GameObject[] { currentGuestCharacter };
        }
        return new GameObject[0];
    }

    private void SpawnHostCharacter(int index)
    {
        if (index >= 0 && index < hostCharacterPrefabs.Length)
        {
            if (currentHostCharacter != null)
            {
                Destroy(currentHostCharacter);
            }
            currentHostCharacter = Instantiate(hostCharacterPrefabs[index], hostSpawnPoint.position, hostSpawnPoint.rotation);

            currentHostCharacter.SetActive(true);
        }
    }

    private void SpawnGuestCharacter(int index)
    {
        if (index >= 0 && index < guestCharacterPrefabs.Length)
        {
            if (currentGuestCharacter != null)
            {
                Destroy(currentGuestCharacter);
            }
            currentGuestCharacter = Instantiate(guestCharacterPrefabs[index], guestSpawnPoint.position, guestSpawnPoint.rotation);

            currentGuestCharacter.SetActive(true);
        }

    }

    private IEnumerator NetworkUpdateRoutine()
    {
        while (true)
        {
            if (isInitialized && network != null && network.IsConnect())
            {
                byte[] bytes = new byte[1024];
                NetworkMessage receivedMessage = network.ReceiveMessage(ref bytes, bytes.Length);

                if (receivedMessage != null)
                {
                    ProcessNetworkMessage(receivedMessage);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void ProcessNetworkMessage(NetworkMessage message)
    {
        if (message == null) return;

        Debug.Log($"[CharacterSpawn] Processing message: Type={message.type}, Data={message.data}");

        switch (message.type)
        {
            case MessageType.CharacterSpawn:
            case MessageType.CharacterInfo:
                if (int.TryParse(message.data, out int characterIndex))
                {
                    if (!network.IsHost())
                    {
                        currentHostIndex = characterIndex;
                        CharDataManager.instance.CurHostCharcter = (Character)characterIndex;
                        SpawnHostCharacter(currentHostIndex);
                        // 응답으로 게스트 캐릭터 정보 전송
                        network.SendMessage(MessageType.GuestSelection, currentGuestIndex.ToString());
                        hasReceivedInitialData = true;
                    }
                }
                break;
            case MessageType.GuestSelection:
                if (network.IsHost() && int.TryParse(message.data, out int guestIndex))
                {
                    currentGuestIndex = guestIndex;
                    CharDataManager.instance.CurGuestCharcter = (Character)guestIndex;
                    SpawnGuestCharacter(currentGuestIndex);
                    hasReceivedInitialData = true;
                }
                break;
        }
    }

    void OnDestroy()
    {
        // 씬 전환 시 현재 캐릭터 정보를 CharDataManager에 저장
        if (currentHostIndex >= 0)
        {
            CharDataManager.instance.CurHostCharcter = (Character)currentHostIndex;
        }
        if (currentGuestIndex >= 0)
        {
            CharDataManager.instance.CurGuestCharcter = (Character)currentGuestIndex;
        }
    }

}