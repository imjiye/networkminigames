using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using TMPro;

public class SceneManagerCanvas : MonoBehaviour
{
    public static SceneManagerCanvas instance
    {
        get
        {
            return m_instance;
        }
    }

    private static SceneManagerCanvas m_instance;

    public CanvasGroup Fade_img;
    public GameObject Loading;
    public TextMeshProUGUI LoadingText;

    float fadeDuration = 2;

    // Start is called before the first frame update
    void Start()
    {
        if(m_instance != null)
        {
            DestroyImmediate(this.gameObject);
            return;
        }
        m_instance = this;

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Fade_img.DOFade(0, fadeDuration).OnStart(() =>
        {
            Loading.SetActive(false);
        }).OnComplete(() =>
        {
            Fade_img.blocksRaycasts = false;
        });
    }

    public void ChangerScene(string sceneName)
    {
        Fade_img.DOFade(1, fadeDuration).OnStart(() =>
        {
            Fade_img.blocksRaycasts = true;
        }).OnComplete(() =>
        {
            // 로딩화면 띄우고, 씬 로드 시작하기
            StartCoroutine("LoadScene", sceneName);
        });
    }

    IEnumerator LoadScene(string sceneName)
    {
        Loading.SetActive(true);

        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;

        float pastTime = 0;
        float percentage = 0;

        while (!(async.isDone))
        {
            yield return null;

            pastTime += Time.deltaTime;

            if(percentage >= 90)
            {
                percentage = Mathf.Lerp(percentage, 100, pastTime);

                if(percentage == 100)
                {
                    async.allowSceneActivation = true;
                }
            }
            else
            {
                percentage = Mathf.Lerp(percentage, async.progress * 100f, pastTime);
                if(percentage >= 90)
                {
                    pastTime = 0;
                }
            }

            LoadingText.text = percentage.ToString("0") + "%";
            
        }
    }
}
