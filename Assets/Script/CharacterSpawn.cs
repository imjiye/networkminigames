using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawn : MonoBehaviour
{
    public GameObject[] hostCharacterPrefabs;
    public GameObject[] guestCharacterPrefabs;
    private GameObject[] spawnedHostCharacters;
    private GameObject[] spawnedGuestCharacters;
    private Network network;
    private int currentHostIndex = -1;
    private int currentGuestIndex = -1;

    void Awake()
    {
        Debug.Log("[CharacterSpawn] Awake called");
        spawnedHostCharacters = new GameObject[hostCharacterPrefabs.Length];
        spawnedGuestCharacters = new GameObject[guestCharacterPrefabs.Length];

        // 모든 캐릭터 프리팹을 미리 인스턴스화
        for (int i = 0; i < hostCharacterPrefabs.Length; i++)
        {
            spawnedHostCharacters[i] = Instantiate(hostCharacterPrefabs[i], transform);
            spawnedHostCharacters[i].SetActive(false);
        }

        for (int i = 0; i < guestCharacterPrefabs.Length; i++)
        {
            spawnedGuestCharacters[i] = Instantiate(guestCharacterPrefabs[i], transform);
            spawnedGuestCharacters[i].SetActive(false);
        }
    }

    void Start()
    {
        Debug.Log("[CharacterSpawn] Start called");
        StartCoroutine(InitializeNetwork());
    }

    private IEnumerator InitializeNetwork()
    {
        yield return new WaitUntil(() => NetworkManager.Instance != null);
        network = NetworkManager.Instance.GetNetwork();
        Debug.Log("[CharacterSpawn] Network connected!");

        yield return new WaitUntil(() => CharDataManager.instance != null);

        if (CharDataManager.instance.Role == UserRole.Host)
        {
            currentHostIndex = (int)CharDataManager.instance.CurHostCharcter;
            Debug.Log($"[CharacterSpawn] Initial Host index set to: {currentHostIndex}");
            UpdateCharacters();
            StartCoroutine(SendCharacterInfo());
        }

        StartCoroutine(ReceiveCharacterInfo());
    }

    private void UpdateCharacters()
    {
        Debug.Log($"[CharacterSpawn] Updating characters - Host: {currentHostIndex}, Guest: {currentGuestIndex}");

        // Host 캐릭터 업데이트
        for (int i = 0; i < spawnedHostCharacters.Length; i++)
        {
            bool shouldBeActive = (i == currentHostIndex);
            spawnedHostCharacters[i].SetActive(shouldBeActive);
            Debug.Log($"[CharacterSpawn] Host character {i} set active: {shouldBeActive}");
        }

        // Guest 캐릭터 업데이트
        for (int i = 0; i < spawnedGuestCharacters.Length; i++)
        {
            bool shouldBeActive = (i == currentGuestIndex);
            spawnedGuestCharacters[i].SetActive(shouldBeActive);
            Debug.Log($"[CharacterSpawn] Guest character {i} set active: {shouldBeActive}");
        }
    }

    private IEnumerator SendCharacterInfo()
    {
        while (true)
        {
            if (CharDataManager.instance.Role == UserRole.Host)
            {
                string message = $"{currentHostIndex}";
                //network.SendMessage(MessageType.CharacterInfo, message);
                Debug.Log($"[CharacterSpawn] Sent character info: {message}");
            }
            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator ReceiveCharacterInfo()
    {
        while (true)
        {
            byte[] bytes = new byte[1024];
            NetworkMessage message = network.ReceiveMessage(ref bytes, bytes.Length);

            if (message != null)
            {
                //if (message.type == MessageType.CharacterInfo)
                //{
                //    if (CharDataManager.instance.Role == UserRole.Guest)
                //    {
                //        currentHostIndex = int.Parse(message.data);
                //        Debug.Log($"[CharacterSpawn] Guest received Host index: {currentHostIndex}");
                //    }
                //    UpdateCharacters();
                //}
                //else if (message.type == MessageType.GuestSelection)
                //{
                //    currentGuestIndex = int.Parse(message.data);
                //    Debug.Log($"[CharacterSpawn] Host received Guest index: {currentGuestIndex}");
                //    UpdateCharacters();
                //}
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    public GameObject[] GetSpawnedHostCharacters() => spawnedHostCharacters;
    public GameObject[] GetSpawnedGuestCharacters() => spawnedGuestCharacters;
}
