using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class AnimManager : MonoBehaviour
{
    [System.Serializable]
    public class AnimationButton
    {
        public anim animationType;  // enum anim 사용
        public Button button;
    }

    public AnimationButton[] animationButtons;
    private CharacterSpawn characterSpawn;
    private Network network;
    private Animator localPlayerAnimator;
    private Animator remotePlayerAnimator;
    private bool isInitialized = false;
    private float remoteAnimatorCheckInterval = 0.5f;

    // 애니메이션 enum을 트리거 이름으로 변환하는 딕셔너리
    private readonly Dictionary<anim, string> animTriggerMap = new Dictionary<anim, string>
    {
        { anim.Belly, "Belly" },
        { anim.Crying, "Crying" },
        { anim.Excited, "Excited" },
        { anim.Macarena, "Macarena" },
        { anim.Rejected, "Rejected" },
        { anim.Greeting, "Greeting" },
        { anim.Waving, "Waving" },
        { anim.YMCA, "YMCA" }
    };

    void Start()
    {
        Debug.Log("[AnimManager] Starting initialization");
        StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        while (AnimData.instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        // CharacterSpawn 찾기
        while (characterSpawn == null)
        {
            characterSpawn = FindObjectOfType<CharacterSpawn>();
            yield return new WaitForSeconds(0.1f);
        }

        // 버튼 이벤트 설정
        foreach (var animBtn in animationButtons)
        {
            if (animBtn.button != null)
            {
                anim animType = animBtn.animationType;
                animBtn.button.onClick.AddListener(() => PlayAnimation(animType));
                Debug.Log($"[AnimManager] Button setup for animation: {animType}");
            }
        }

        // 애니메이션 이벤트 리스너 설정
        if (CharDataManager.instance.Role == UserRole.Host)
        {
            AnimData.instance.OnHostAnimationChanged += OnHostAnimationChanged;
        }
        else
        {
            AnimData.instance.OnGuestAnimationChanged += OnGuestAnimationChanged;
        }

        yield return StartCoroutine(InitializeAnimators());
    }

    private void PlayAnimation(anim animationType)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[AnimManager] Not yet initialized!");
            return;
        }

        // AnimData를 통해 애니메이션 상태 변경
        if (CharDataManager.instance.Role == UserRole.Host)
        {
            AnimData.instance.CurHostAnim = animationType;
        }
        else
        {
            AnimData.instance.CurGuestAnim = animationType;
        }

        // 로컬 애니메이터에 트리거 설정
        if (localPlayerAnimator != null && animTriggerMap.TryGetValue(animationType, out string triggerName))
        {
            localPlayerAnimator.SetTrigger(triggerName);
            Debug.Log($"[AnimManager] Playing local animation: {triggerName}");
        }
    }

    private void OnHostAnimationChanged(anim animationType)
    {
        if (CharDataManager.instance.Role == UserRole.Guest && remotePlayerAnimator != null)
        {
            if (animTriggerMap.TryGetValue(animationType, out string triggerName))
            {
                remotePlayerAnimator.SetTrigger(triggerName);
                Debug.Log($"[AnimManager] Playing remote host animation: {triggerName}");
            }
        }
    }

    private void OnGuestAnimationChanged(anim animationType)
    {
        if (CharDataManager.instance.Role == UserRole.Host && remotePlayerAnimator != null)
        {
            if (animTriggerMap.TryGetValue(animationType, out string triggerName))
            {
                remotePlayerAnimator.SetTrigger(triggerName);
                Debug.Log($"[AnimManager] Playing remote guest animation: {triggerName}");
            }
        }
    }


    private IEnumerator InitializeAnimators()
    {
        // CharDataManager 초기화 대기
        while (CharDataManager.instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1f);

        bool isHost = CharDataManager.instance.Role == UserRole.Host;

        // 로컬 플레이어 애니메이터 설정
        while (localPlayerAnimator == null)
        {
            GameObject[] localCharacters = isHost ?
                characterSpawn.GetSpawnedHostCharacters() :
                characterSpawn.GetSpawnedGuestCharacters();

            if (localCharacters.Length > 0 && localCharacters[0] != null)
            {
                localPlayerAnimator = localCharacters[0].GetComponent<Animator>();
                Debug.Log("[AnimManager] Local animator initialized");
            }
            yield return new WaitForSeconds(0.5f);
        }

        // 리모트 플레이어 애니메이터 설정
        yield return StartCoroutine(FindRemoteAnimator());

        isInitialized = true;
        Debug.Log("[AnimManager] Initialization complete");
        StartCoroutine(NetworkMessageHandler());
    }

    private IEnumerator RemoteAnimatorCheck()
    {
        while (true)
        {
            if (remotePlayerAnimator == null)
            {
                yield return StartCoroutine(FindRemoteAnimator());
            }
            yield return new WaitForSeconds(remoteAnimatorCheckInterval);
        }
    }

    private IEnumerator FindRemoteAnimator()
    {
        bool isHost = CharDataManager.instance.Role == UserRole.Host;

        while (remotePlayerAnimator == null)
        {
            GameObject[] remoteCharacters = isHost ?
                characterSpawn.GetSpawnedGuestCharacters() :
                characterSpawn.GetSpawnedHostCharacters();

            if (remoteCharacters.Length > 0 && remoteCharacters[0] != null)
            {
                remotePlayerAnimator = remoteCharacters[0].GetComponent<Animator>();
                if (remotePlayerAnimator != null)
                {
                    Debug.Log("[AnimManager] Remote animator found!");
                    break;
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void PlayAnimation(string triggerName)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[AnimManager] Not yet initialized!");
            return;
        }

        if (localPlayerAnimator != null)
        {
            localPlayerAnimator.SetTrigger(triggerName);
            Debug.Log($"[AnimManager] Playing local animation: {triggerName}");

            // 네트워크 메시지 전송
            string role = CharDataManager.instance.Role == UserRole.Host ? "Host" : "Guest";
            int characterIndex = CharDataManager.instance.Role == UserRole.Host ?
                (int)CharDataManager.instance.CurHostCharcter :
                (int)CharDataManager.instance.CurGuestCharcter;

            string animationData = $"{role}|{characterIndex}|{triggerName}";
            network.SendMessage(MessageType.Animation, animationData);
            Debug.Log($"[AnimManager] Sent animation message: {animationData}");
        }
        else
        {
            Debug.LogError("[AnimManager] Local animator is null!");
        }
    }

    private IEnumerator NetworkMessageHandler()
    {
        Debug.Log("[AnimManager] Starting network message handler");

        while (true)
        {
            if (network != null && network.IsConnect())
            {
                byte[] bytes = new byte[1024];
                NetworkMessage message = network.ReceiveMessage(ref bytes, bytes.Length);

                if (message != null && message.type == MessageType.Animation)
                {
                    string[] parts = message.data.Split('|');
                    if (parts.Length == 3)
                    {
                        string senderRole = parts[0];
                        string triggerName = parts[2];

                        bool isFromHost = senderRole == "Host";
                        bool shouldPlayAnimation = (isFromHost && CharDataManager.instance.Role == UserRole.Guest) ||
                                                (!isFromHost && CharDataManager.instance.Role == UserRole.Host);

                        if (shouldPlayAnimation)
                        {
                            if (remotePlayerAnimator != null)
                            {
                                Debug.Log($"[AnimManager] Playing remote animation: {triggerName}");
                                remotePlayerAnimator.SetTrigger(triggerName);
                            }
                            else
                            {
                                Debug.LogWarning("[AnimManager] Remote animator is null, attempting to find it again");
                                StartCoroutine(FindRemoteAnimator());
                            }
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.016f);
        }
    }

    void OnDestroy()
    {
        // 이벤트 리스너 제거
        if (CharDataManager.instance.Role == UserRole.Host)
        {
            AnimData.instance.OnHostAnimationChanged -= OnHostAnimationChanged;
        }
        else
        {
            AnimData.instance.OnGuestAnimationChanged -= OnGuestAnimationChanged;
        }

        StopAllCoroutines();
    }
}