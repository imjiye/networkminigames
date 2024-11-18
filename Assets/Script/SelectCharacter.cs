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

    private void Awake()
    {
        if(network == null)
        {
            network = FindObjectOfType<Network>();
            if(network == null)
            {
                Debug.LogError("[SelectCharacter] Network component not found in scene!");
            }
        }
        
    }

    void Start()
    {
        // 네트워크 연결 상태 확인
        if (network != null && !network.IsConnect())
        {
            Debug.LogWarning("[SelectCharacter] Network is not connected!");
        }

        // 현재 씬이 캐릭터 선택 씬인지 확인
        isInSelectionScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "CharcterSelectScene";

        if (isInSelectionScene)
        {
            particle.SetActive(false);
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }

        InitializeButtons();
    }

    private void InitializeButtons()
    {
        if (selectBtn != null)
        {
            selectBtn.onClick.AddListener(() => OnSelect());
        }
        else
        {
            Debug.LogWarning("[SelectCharacter] Select button is not assigned!");
        }

        if (confirmBtn != null)
        {
            confirmBtn.onClick.AddListener(() => ConfirmSelection());
        }
        else
        {
            Debug.LogWarning("[SelectCharacter] Confirm button is not assigned!");
        }
    }

    public void OnSelect()
    {
        if (CharDataManager.instance == null)
        {
            Debug.LogError("[SelectCharacter] CharDataManager instance is null!");
            return;
        }

        if (CharDataManager.instance.Role == UserRole.Host)
        {
            CharDataManager.instance.CurHostCharcter = character;
            DeselectOtherCharacters();
            ActivateCharacter();
        }
        else if (CharDataManager.instance.Role == UserRole.Guest)
        {
            CharDataManager.instance.CurGuestCharcter = character;
            DeselectOtherCharacters();
            ActivateCharacter();
        }
    }

    private void DeselectOtherCharacters()
    {
        if (chars != null)
        {
            foreach (var otherChar in chars)
            {
                if (otherChar != null && otherChar != this)
                {
                    otherChar.OnDeSelect();
                }
            }
        }
    }

    private void ActivateCharacter()
    {
        if (particle != null) particle.SetActive(true);
        gameObject.SetActive(true);
    }

    public void OnDeSelect()
    {
        // 선택 씬에서만 비활성화 적용
        if (isInSelectionScene)
        {
            if (particle != null) particle.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    public void ConfirmSelection()
    {
        if (network == null)
        {
            Debug.LogError("[SelectCharacter] Network component is null!");
            return;
        }

        if (!network.IsConnect())
        {
            Debug.LogError("[SelectCharacter] Network is not connected!");
            return;
        }

        try
        {
            string characterInfo = ((int)character).ToString();
            network.SendMessage(MessageType.CharacterSpawn, characterInfo);
            Debug.Log($"[SelectCharacter] Sending character selection: {characterInfo}");
            UnityEngine.SceneManagement.SceneManager.LoadScene("ChatScene");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SelectCharacter] Error sending character selection: {e.Message}");
        }
    }
}