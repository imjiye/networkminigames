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

    private bool isInSelectionScene = true; // ĳ���� ���� ������ ���θ� ����

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
        // ���� ���� ĳ���� ���� ������ Ȯ��
        isInSelectionScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "CharcterSelectScene";
    }

    void Start()
    {
        // ĳ���� ���� ���� ���� ��ư �ʱ�ȭ�� ��Ʈ��ũ üũ�� ����
        if (isInSelectionScene)
        {
            // ��Ʈ��ũ ���� ���� Ȯ��
            if (network != null && !network.IsConnect())
            {
                Debug.LogWarning("[SelectCharacter] Network is not connected!");
            }

            particle.SetActive(false);
            gameObject.SetActive(false);
            InitializeButtons();
        }
        else
        {
            // ĳ���� ���� ���� �ƴ� ��� ��ư �����ʸ� ������� ����
            gameObject.SetActive(true);
            if (selectBtn != null) selectBtn.onClick.RemoveAllListeners();
            if (confirmBtn != null) confirmBtn.onClick.RemoveAllListeners();
        }
    }

    private void InitializeButtons()
    {
        if (selectBtn != null)
        {
            selectBtn.onClick.RemoveAllListeners();
            selectBtn.onClick.AddListener(() => OnSelect());
        }
        else
        {
            Debug.LogWarning("[SelectCharacter] Select button is not assigned!");
        }

        if (confirmBtn != null)
        {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(() => ConfirmSelection());
        }
        else
        {
            Debug.LogWarning("[SelectCharacter] Confirm button is not assigned!");
        }
    }

    private void OnDestroy()
    {
        // ��ũ��Ʈ�� ���ŵ� �� ��� ������ ����
        if (selectBtn != null) selectBtn.onClick.RemoveAllListeners();
        if (confirmBtn != null) confirmBtn.onClick.RemoveAllListeners();
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
        // ���� �������� ��Ȱ��ȭ ����
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