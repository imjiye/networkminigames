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

    void Start()
    {
        // ���� ���� ĳ���� ���� ������ Ȯ��
        isInSelectionScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "CharcterSelectScene";

        // ���� �������� �ʱ� ��Ȱ��ȭ ����
        if (isInSelectionScene)
        {
            particle.SetActive(false);
            gameObject.SetActive(false);
        }
        else
        {
            // ä�� �� �� �ٸ� �������� Ȱ��ȭ ���� ����
            //particle.SetActive(true);
            gameObject.SetActive(true);
        }

        // ĳ���� ���� ��ư�� �̺�Ʈ �߰�
        if (selectBtn != null)
        {
            selectBtn.onClick.AddListener(() => OnSelect());
        }

        // ���� Ȯ�� ��ư�� �̺�Ʈ �߰�
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
        // ���� �������� ��Ȱ��ȭ ����
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