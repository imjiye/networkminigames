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

        // NetworkManager ���
        while (NetworkManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        network = NetworkManager.Instance.GetNetwork();

        // CharDataManager ���
        while (CharDataManager.instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // ���� ���
        while (!network.IsConnect())
        {
            yield return new WaitForSeconds(0.1f);
        }

        // �ʱ� ĳ���� ���� ����
        if (network.IsHost())
        {
            currentHostIndex = (int)CharDataManager.instance.CurHostCharcter;
            SpawnHostCharacter(currentHostIndex);

            // �Խ�Ʈ ĳ���� ���� ��û
            network.SendMessage(MessageType.CharacterInfo, currentHostIndex.ToString());

            // ����� �Խ�Ʈ ĳ���� ������ �ִٸ� ����
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

            // ȣ��Ʈ ĳ���� ���� ��û
            network.SendMessage(MessageType.GuestSelection, currentGuestIndex.ToString());

            // ����� ȣ��Ʈ ĳ���� ������ �ִٸ� ����
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
        float timeout = 5f; // 5�� Ÿ�Ӿƿ�
        float elapsed = 0f;

        while (!hasReceivedInitialData && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ÿ�Ӿƿ� �Ŀ��� �����͸� ���� ���ߴٸ� �ٽ� ��û
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

    // ���� ������ ȣ��Ʈ ĳ���� �迭 ��ȯ
    public GameObject[] GetSpawnedHostCharacters()
    {
        if (currentHostCharacter != null)
        {
            return new GameObject[] { currentHostCharacter };
        }
        return new GameObject[0];
    }

    // ���� ������ �Խ�Ʈ ĳ���� �迭 ��ȯ
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
                        // �������� �Խ�Ʈ ĳ���� ���� ����
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
        // �� ��ȯ �� ���� ĳ���� ������ CharDataManager�� ����
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