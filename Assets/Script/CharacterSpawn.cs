using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class CharacterSpawn : MonoBehaviour
{
    public GameObject[] Player0Prefab;
    public GameObject[] Player1Prefab;

    public static CharacterSpawn Instance;

    private void Awake()
    {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        // Host老 版快
        if (CharDataManager.instance.Role == UserRole.Host)
        {
            SpawnHostCharacter((int)CharDataManager.instance.CurCharcter);
        }
        // Guest老 版快
        else if (CharDataManager.instance.Role == UserRole.Guest)
        {
            SpawnGuestCharacter((int)CharDataManager.instance.CurCharcter);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < Player0Prefab.Length; i++)
        {
            if ((int)CharDataManager.instance.CurCharcter == i)
            {
                Player0Prefab[i].SetActive(true);
            }
            else
            {
                Player0Prefab[i].SetActive(false);
            }
        }
        for (int i = 0; i < Player1Prefab.Length; i++)
        {
            if ((int)CharDataManager.instance.CurCharcter == i)
            {
                Player1Prefab[i].SetActive(true);
            }
            else
            {
                Player1Prefab[i].SetActive(false);
            }
        }
    }

    private void SpawnHostCharacter(int characterIndex)
    {
        for (int i = 0; i < Player0Prefab.Length; i++)
        {
            Player0Prefab[i].SetActive(i == characterIndex);
        }
    }

    private void SpawnGuestCharacter(int characterIndex)
    {
        for (int i = 0; i < Player1Prefab.Length; i++)
        {
            Player1Prefab[i].SetActive(i == characterIndex);
        }
    }
}