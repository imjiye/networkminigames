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
            Debug.LogError("UIManager�� �Ҵ���� �ʾҽ��ϴ�.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // �� ��ȯ
    public void LoadScene(string sceneID)
    {
        SceneManager.LoadScene(sceneID);
    }

    // ���� �����
    public void GameRestart()
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ���� ����
    public void GameEnd()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
