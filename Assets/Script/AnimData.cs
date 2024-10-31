using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum anim
{
    Belly,
    Crying,
    Excited,
    Macarena,
    Rejected,
    Greeting,
    Waving,
    YMCA
}

public class AnimData : MonoBehaviour
{
    public static AnimData instance;

    public anim curHostAnim;
    public anim curGuestAnim;

    // �ִϸ��̼� ���� �̺�Ʈ
    public event Action<anim> OnHostAnimationChanged;
    public event Action<anim> OnGuestAnimationChanged;

    private Network network;
    private bool isNetworkInitialized = false;

    public anim CurHostAnim
    {
        get { return curHostAnim; }
        set
        {
            if (curHostAnim != value)
            {
                curHostAnim = value;
                OnHostAnimationChanged?.Invoke(curHostAnim);
                // ȣ��Ʈ�� ��� �Խ�Ʈ���� ������� �˸�
                if (CharDataManager.instance.Role == UserRole.Host && isNetworkInitialized)
                {
                    network.SendMessage(MessageType.Animation, $"Host|{((int)curHostAnim).ToString()}");
                }
            }
        }
    }

    public anim CurGuestAnim
    {
        get { return curGuestAnim; }
        set
        {
            if (curGuestAnim != value)
            {
                curGuestAnim = value;
                OnGuestAnimationChanged?.Invoke(curGuestAnim);
                // �Խ�Ʈ�� ��� ȣ��Ʈ���� ������� �˸�
                if (CharDataManager.instance.Role == UserRole.Guest && isNetworkInitialized)
                {
                    network.SendMessage(MessageType.Animation, $"Guest|{((int)curGuestAnim).ToString()}");
                }
            }
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(InitializeNetwork());
        }
        else if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
    }

    private IEnumerator InitializeNetwork()
    {
        while (NetworkManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        network = NetworkManager.Instance.GetNetwork();
        isNetworkInitialized = true;

        // ��Ʈ��ũ �޽��� ���� ����
        StartCoroutine(NetworkUpdateRoutine());
    }

    private IEnumerator NetworkUpdateRoutine()
    {
        while (true)
        {
            if (network != null && network.IsConnect())
            {
                byte[] bytes = new byte[1024];
                NetworkMessage message = network.ReceiveMessage(ref bytes, bytes.Length);

                if (message != null)
                {
                    ProcessNetworkMessage(message);
                }
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void ProcessNetworkMessage(NetworkMessage message)
    {
        if (message == null || message.type != MessageType.Animation) return;

        string[] parts = message.data.Split('|');
        if (parts.Length != 2) return;

        string role = parts[0];
        if (int.TryParse(parts[1], out int animIndex))
        {
            if (role == "Host" && CharDataManager.instance.Role == UserRole.Guest)
            {
                curHostAnim = (anim)animIndex;
                OnHostAnimationChanged?.Invoke(curHostAnim);
                Debug.Log($"[AnimData] Received host animation update: {curHostAnim}");
            }
            else if (role == "Guest" && CharDataManager.instance.Role == UserRole.Host)
            {
                curGuestAnim = (anim)animIndex;
                OnGuestAnimationChanged?.Invoke(curGuestAnim);
                Debug.Log($"[AnimData] Received guest animation update: {curGuestAnim}");
            }
        }
    }

    // ����׸� ���� ���� �ִϸ��̼� ���� �α�
    public void LogAnimationState()
    {
        Debug.Log($"[AnimData] Current Animation State:");
        Debug.Log($"Role: {CharDataManager.instance.Role}");
        Debug.Log($"Host Animation: {curHostAnim}");
        Debug.Log($"Guest Animation: {curGuestAnim}");
        Debug.Log($"Network Initialized: {isNetworkInitialized}");
    }
}