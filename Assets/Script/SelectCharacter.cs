using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    //public AudioClip audioClip;
    //public AudioSource audioSource;
    // Start is called before the first frame update
    void Start()
    {
        particle.SetActive(false);
        //audioSource.Stop();
        gameObject.SetActive(false);

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
    //private void OnMouseUpAsButton()
    //{
    //    if(CharDataManager.instance.Role == UserRole.Host)
    //    {
    //        CharDataManager.instance.CurHostCharcter = character;
    //        OnSelect();

    //        // 선택된 캐릭터 정보를 네트워크로 전송
    //        string characterInfo = ((int)character).ToString();
    //        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Character" + characterInfo);
    //        network.Send(bytes, bytes.Length);

    //        for (int i = 0; i < chars.Length; i++)
    //        {
    //            if (chars[i] != this)
    //            {
    //                chars[i].OnDeSelect();
    //            }
    //        }
    //    }
    //    else if(CharDataManager.instance.Role == UserRole.Guest)
    //    {
    //        CharDataManager.instance.CurGuestCharcter = character;
    //        OnSelect();

    //        // 선택된 캐릭터 정보를 네트워크로 전송
    //        string characterInfo = ((int)character).ToString();
    //        byte[] bytes = System.Text.Encoding.UTF8.GetBytes("Character" + characterInfo);
    //        network.Send(bytes, bytes.Length);

    //        for (int i = 0; i < chars.Length; i++)
    //        {
    //            if (chars[i] != this)
    //            {
    //                chars[i].OnDeSelect();
    //            }
    //        }
    //    }
    //}

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
        particle.SetActive(false);
        //audioSource.Stop();
        gameObject.SetActive(false);
    }

    // 캐릭터 선택을 확정하고 채팅 씬으로 이동하는 함수
    public void ConfirmSelection()
    {
        if (network != null)
        {
            // 선택된 캐릭터 정보 전송
            string characterInfo = ((int)character).ToString();
            network.SendMessage(MessageType.CharacterSpawn, characterInfo);
            Debug.Log($"[SelectCharacter] Sending character selection: {characterInfo}");

            // 채팅 씬으로 이동
            UnityEngine.SceneManagement.SceneManager.LoadScene("ChatScene");
        }
        else
        {
            Debug.LogError("[SelectCharacter] Network component is null!");
        }
    }
}