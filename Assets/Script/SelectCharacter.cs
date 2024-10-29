using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
public class SelectCharacter : MonoBehaviour
{
    public Network network;
    public Character character;
    public GameObject particle;
    public SelectCharacter[] chars;
    //public AudioClip audioClip;
    //public AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        particle.SetActive(false);
        //audioSource.Stop();
        gameObject.SetActive(false);
        if (CharDataManager.instance.CurCharcter == character)
        {
            OnSelect();
        }
        else
        {
            OnDeSelect();
        }

    }
    private void OnMouseUpAsButton()
    {
        CharDataManager.instance.CurCharcter = character;
        OnSelect();

        // ���õ� ĳ���� ������ ��Ʈ��ũ�� ����
        string characterInfo = ((int)character).ToString();
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Character" + characterInfo);
        network.Send(bytes, bytes.Length);

        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] != this)
            {
                chars[i].OnDeSelect();
            }
        }
    }
    public void OnSelect()
    {
        CharDataManager.instance.CurCharcter = character;
        for (int i = 0; i < chars.Length; i++)
        {
            if (chars[i] != this)
            {
                chars[i].OnDeSelect();
            }
        }
        particle.SetActive(true);
        //audioSource.PlayOneShot(audioClip);
        gameObject.SetActive(true);
    }
    public void OnDeSelect()
    {
        particle.SetActive(false);
        //audioSource.Stop();
        gameObject.SetActive(false);
    }

    public void SelectCharacterBtn(Character character)
    {
        CharDataManager.instance.CurCharcter = character;
        // ä�� ������ �̵�
        UnityEngine.SceneManagement.SceneManager.LoadScene("ChatScene");
    }
}