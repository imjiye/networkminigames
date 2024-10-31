using UnityEngine;
using UnityEngine.UI;

public class SelectCharacter : MonoBehaviour
{
    public Network network;
    public Character character;
    public GameObject particle;
    public SelectCharacter[] chars;
    public Button selectBtn;
    public Button confirmBtn;

    private bool isInSelectionScene = true; // 캐릭터 선택 씬인지 여부를 추적

    void Start()
    {
        // 현재 씬이 캐릭터 선택 씬인지 확인
        isInSelectionScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "CharcterSelectScene";

        // 선택 씬에서만 초기 비활성화 적용
        if (isInSelectionScene)
        {
            particle.SetActive(false);
            gameObject.SetActive(false);
        }
        else
        {
            // 채팅 씬 등 다른 씬에서는 활성화 상태 유지
            //particle.SetActive(true);
            gameObject.SetActive(true);
        }

        // 캐릭터 선택 버튼에 이벤트 추가
        if (selectBtn != null)
        {
            selectBtn.onClick.AddListener(() => OnSelect());
        }

        // 선택 확정 버튼에 이벤트 추가
        if (confirmBtn != null)
        {
            confirmBtn.onClick.AddListener(() => ConfirmSelection());
        }
    }

    public void OnSelect()
    {
        if (CharDataManager.instance.Role == UserRole.Host)
        {
            CharDataManager.instance.CurHostCharcter = character;

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] != this)
                {
                    chars[i].OnDeSelect();
                }
            }
            particle.SetActive(true);
            gameObject.SetActive(true);
        }
        else if (CharDataManager.instance.Role == UserRole.Guest)
        {
            CharDataManager.instance.CurGuestCharcter = character;

            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] != this)
                {
                    chars[i].OnDeSelect();
                }
            }
            particle.SetActive(true);
            gameObject.SetActive(true);
        }
    }

    public void OnDeSelect()
    {
        // 선택 씬에서만 비활성화 적용
        if (isInSelectionScene)
        {
            particle.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    public void ConfirmSelection()
    {
        if (network != null)
        {
            string characterInfo = ((int)character).ToString();
            network.SendMessage(MessageType.CharacterSpawn, characterInfo);
            Debug.Log($"[SelectCharacter] Sending character selection: {characterInfo}");

            UnityEngine.SceneManagement.SceneManager.LoadScene("ChatScene");
        }
        else
        {
            Debug.LogError("[SelectCharacter] Network component is null!");
        }
    }
}