using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    public static UIManager instance
    {
        get
        {
            if(m_instance == null)
            {
                m_instance = FindObjectOfType<UIManager>();
            }
            return m_instance;
        }
    }

    private static UIManager m_instance;

    public UIManager uiManager;

    // Start is called before the first frame update
    void Start()
    {
        if (uiManager == null)
        {
            Debug.LogError("UIManager가 할당되지 않았습니다.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // 씬 전환
    public void LoadScene(string sceneID)
    {
        SceneManager.LoadScene(sceneID);
    }

    // 게임 재시작
    public void GameRestart()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 게임 종료
    public void GameEnd()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void SelectHost()
    {
        CharDataManager.instance.Role = UserRole.Host;
        // 다음 캐릭터 선택 씬으로 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("CharcterSelectScene");
    }

    public void SelectGuest()
    {
        CharDataManager.instance.Role = UserRole.Guest;
        // 다음 캐릭터 선택 씬으로 이동
        UnityEngine.SceneManagement.SceneManager.LoadScene("CharcterSelectScene");
    }
}
