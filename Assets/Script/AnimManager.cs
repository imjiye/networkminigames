using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnimManager : MonoBehaviour
{
    [System.Serializable]
    public class AnimationButton
    {
        public string triggerName;
        public Button button;
    }

    public AnimationButton[] animationButtons;
    private CharacterSpawn characterSpawn;
    private Network network;
    private Animator localPlayerAnimator;
    private Animator remotePlayerAnimator;
    private bool isInitialized = false;

    void Start()
    {
        characterSpawn = FindObjectOfType<CharacterSpawn>();
        network = NetworkManager.Instance.GetNetwork();

        foreach (var animBtn in animationButtons)
        {
            string triggerName = animBtn.triggerName;
            animBtn.button.onClick.AddListener(() => PlayAnimation(triggerName));
        }

        StartCoroutine(InitializeAnimators());
    }

    private IEnumerator InitializeAnimators()
    {
        yield return new WaitUntil(() => CharDataManager.instance != null && characterSpawn != null);

        bool isHost = CharDataManager.instance.Role == UserRole.Host;
        int localCharacterIndex = isHost ?
            (int)CharDataManager.instance.CurHostCharcter :
            (int)CharDataManager.instance.CurGuestCharcter;

        // 로컬 플레이어 애니메이터 설정
        GameObject[] localCharacters = isHost ?
            characterSpawn.GetSpawnedHostCharacters() :
            characterSpawn.GetSpawnedGuestCharacters();

        if (localCharacterIndex >= 0 && localCharacterIndex < localCharacters.Length)
        {
            localPlayerAnimator = localCharacters[localCharacterIndex].GetComponent<Animator>();
            Debug.Log($"[AnimManager] Local animator set for character index: {localCharacterIndex}");
        }

        // 리모트 플레이어 애니메이터 찾기 시작
        StartCoroutine(FindRemoteAnimator());
        isInitialized = true;
        StartCoroutine(NetworkMessageHandler());
    }

    private IEnumerator FindRemoteAnimator()
    {
        while (true)
        {
            bool isHost = CharDataManager.instance.Role == UserRole.Host;
            GameObject[] remoteCharacters = isHost ?
                characterSpawn.GetSpawnedGuestCharacters() :
                characterSpawn.GetSpawnedHostCharacters();

            foreach (var character in remoteCharacters)
            {
                if (character.activeInHierarchy)
                {
                    remotePlayerAnimator = character.GetComponent<Animator>();
                    Debug.Log("[AnimManager] Remote animator found!");
                    yield break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void PlayAnimation(string triggerName)
    {
        if (!isInitialized) return;

        if (localPlayerAnimator != null)
        {
            localPlayerAnimator.SetTrigger(triggerName);
            Debug.Log($"[AnimManager] Playing local animation: {triggerName}");
        }

        string role = CharDataManager.instance.Role == UserRole.Host ? "Host" : "Guest";
        int characterIndex = CharDataManager.instance.Role == UserRole.Host ?
            (int)CharDataManager.instance.CurHostCharcter :
            (int)CharDataManager.instance.CurGuestCharcter;

        string animationData = $"{role}|{characterIndex}|{triggerName}";
        network.SendMessage(MessageType.Animation, animationData);
        Debug.Log($"[AnimManager] Sent animation message: {animationData}");
    }

    private IEnumerator NetworkMessageHandler()
    {
        while (true)
        {
            byte[] bytes = new byte[1024];
            NetworkMessage message = network.ReceiveMessage(ref bytes, bytes.Length);

            if (message != null && message.type == MessageType.Animation)
            {
                string[] parts = message.data.Split('|');
                if (parts.Length == 3)
                {
                    string senderRole = parts[0];
                    int characterIndex = int.Parse(parts[1]);
                    string triggerName = parts[2];

                    bool isFromHost = senderRole == "Host";
                    if ((isFromHost && CharDataManager.instance.Role == UserRole.Guest) ||
                        (!isFromHost && CharDataManager.instance.Role == UserRole.Host))
                    {
                        if (remotePlayerAnimator != null)
                        {
                            remotePlayerAnimator.SetTrigger(triggerName);
                            Debug.Log($"[AnimManager] Playing remote animation: {triggerName}");
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.016f);
        }
    }
}
