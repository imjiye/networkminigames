using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Character
{
    ElfMan,
    ElfWoman,
    PlayerMan,
    PlayerWoman,
    WolfMan,
    WolfWoman  
}

public enum UserRole { Host, Guest}

public class CharDataManager : MonoBehaviour
{
    public static CharDataManager instance;

    public Character CurHostCharcter = Character.ElfMan; // 기본값 설정
    public Character CurGuestCharcter = Character.ElfWoman; // 기본값 설정
    public UserRole Role;
    public string PlayerName;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != null) return;

        DontDestroyOnLoad(gameObject);
    }
}
