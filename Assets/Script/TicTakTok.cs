using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEditor;

public class TicTakTok : MonoBehaviour
{
    enum State
    {
        Start = 0,
        Game,
        End
    }

    enum Turn
    {
        Host = 0,
        Guest,
    }

    enum Mark
    {
        None = 0,
        White,
        Black,
    }

    Network network;

    public Texture texBoard;
    public Texture texWhite;
    public Texture texBlack;
    public Texture texYou;
    public Texture texhostWin;
    public Texture texguestWin;

    public GameObject resetBtn;

    private float boardSize = 720f; 
    private float cellSize = 240f;  
    private float markSize = 150f;
    private Vector2 boardPosition; 

    // �ٵ��� ���� ��ġ 9���� �����
    int[] board = new int[9];

    private CharacterSpawn characterSpawn;
    private Animator hostAnimator;
    private Animator guestAnimator;

    State state; // ���� ���� ��Ȳ

    Mark markTurn; // ���� �� üũ�ϱ� ���� ����
    Mark markHost; // ȣ��Ʈ�� ��ũ
    Mark markGuest; // �Խ�Ʈ�� ��ũ
    Mark markWinner; // �¸��� ��ũ

    void Awake()
    {
        // NetworkManager�� ���� Network ������Ʈ ���� ��������
        if (NetworkManager.Instance != null)
        {
            network = NetworkManager.Instance.GetNetwork();
        }
        else
        {
            Debug.LogError("NetworkManager instance not found!");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        resetBtn.SetActive(false);
        state = State.Start;

        for (int i = 0; i < board.Length; i++)
        {
            // ���忡 �ƹ��͵� ���� �ʱ�ȭ���ֱ�
            board[i] = (int)Mark.None;
        }

        // ȭ�� �߾� ��ǥ ���
        float screenCenterX = Screen.width * 0.5f;
        float screenCenterY = Screen.height * 0.5f;

        // ������ �»�� ��ǥ ���
        boardPosition = new Vector2(
            screenCenterX - (boardSize * 0.5f),
            screenCenterY - (boardSize * 0.5f)
        );

        characterSpawn = FindObjectOfType<CharacterSpawn>();
        if (characterSpawn == null)
        {
            Debug.LogError("[TicTakTok] CharacterSpawn component not found!");
        }
    }

    // ĳ���� �ִϸ����� ��������
    private void UpdateAnimators()
    {
        if (characterSpawn != null)
        {
            GameObject[] hostCharacters = characterSpawn.GetSpawnedHostCharacters();
            GameObject[] guestCharacters = characterSpawn.GetSpawnedGuestCharacters();

            if (hostCharacters.Length > 0 && hostCharacters[0] != null)
            {
                hostAnimator = hostCharacters[0].GetComponent<Animator>();
                if (hostAnimator == null)
                {
                    Debug.LogError("[TicTakTok] Host character animator not found!");
                }
            }

            if (guestCharacters.Length > 0 && guestCharacters[0] != null)
            {
                guestAnimator = guestCharacters[0].GetComponent<Animator>();
                if (guestAnimator == null)
                {
                    Debug.LogError("[TicTakTok] Guest character animator not found!");
                }
            }
        }
    }

    // ����� ��ũ �׸���
    private void OnGUI()
    {
        if (!Event.current.type.Equals(EventType.Repaint))
            return;

        // �߾ӿ� ���� �׸���
        Graphics.DrawTexture(new Rect(boardPosition.x, boardPosition.y, boardSize, boardSize), texBoard);

        // �� �׸��� - ũ��� ��ġ ����
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] != (int)Mark.None)
            {
                // �� ĭ�� ���� ��� ��ǥ ���
                float cellX = boardPosition.x + (i % 3) * cellSize;
                float cellY = boardPosition.y + (i / 3) * cellSize;

                // ��ũ�� ĭ�� �߾ӿ� ��ġ��Ű�� ���� ������ ���
                float offsetX = (cellSize - markSize) * 0.5f;
                float offsetY = (cellSize - markSize) * 0.5f;

                // ���� ��ũ ��ġ ���
                float markX = cellX + offsetX;
                float markY = cellY + offsetY;

                Texture tex = (board[i] == (int)Mark.White) ? texWhite : texBlack;
                Graphics.DrawTexture(new Rect(markX, markY, markSize, markSize), tex);
            }
        }

        // �� ǥ�õ� ���� ũ��� ����
        if (state == State.Game)
        {
            if (markTurn == Mark.White)
            {
                Graphics.DrawTexture(new Rect(
                    boardPosition.x - cellSize + markSize * 0.5f,
                    boardPosition.y + boardSize - cellSize * 0.5f,
                    markSize,
                    markSize
                ), texWhite);
            }
            else
            {
                Graphics.DrawTexture(new Rect(
                    boardPosition.x + boardSize + (cellSize - markSize) * 0.5f,
                    boardPosition.y + boardSize - cellSize * 0.5f,
                    markSize,
                    markSize
                ), texBlack);
            }
        }

        // �¸� ǥ�õ� ���� ũ��� ����
        if (state == State.End)
        {
            //float winnerX = boardPosition.x + (boardSize - markSize) * 0.5f;
            //float winnerY = boardPosition.y + (boardSize - markSize) * 0.5f;

            if (markWinner == Mark.White)
            {
                Graphics.DrawTexture(new Rect(boardPosition.x, boardPosition.y, boardSize, boardSize), texhostWin);
            }
            else
            {
                Graphics.DrawTexture(new Rect(boardPosition.x, boardPosition.y, boardSize, boardSize), texguestWin);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (network == null)
        {
            Debug.LogWarning("Network is null");
            network = NetworkManager.Instance?.GetNetwork();
            return;
        }

        if (!network.IsConnect())
        {
            Debug.LogWarning("Network is not connected");
            return;
        }

        switch (state)
        {
            case State.Start:
                UpdateStart();
                break;

            case State.Game:
                UpdateGame();
                break;

            case State.End:
                UpdateEnd();
                break;
        }
    }

    // ���� �����ϱ� ����
    void UpdateStart()
    {
        // ���¸� ���ۻ��·� �ʱ�ȭ
        state = State.Game;

        // ù ���� ������ ȭ��Ʈ���� ���� ��������
        markTurn = Mark.White;

        // ���� ������ ���
        if (network.IsHost())
        {
            markHost = Mark.White;
            markGuest = Mark.Black;
        }
        // ���� Ŭ���̾�Ʈ�� ���
        else
        {
            markHost = Mark.Black;
            markGuest = Mark.White;
        }
    }

    // ������ �������� ����
    void UpdateGame()
    {
        bool bSet = false;

        // �� ���� ���� ���� ���� ���� ���
        if (markTurn == markHost)
        {
            bSet = MyTurn();
        }
        else
        {
            bSet = YourTurn();
        }

        // ���õȰ� ���� ��� ����
        if (bSet == false)
        {
            return;
        }

        markWinner = CheckBoard();

        if (markWinner != Mark.None)
        {
            state = State.End;
            // �¸� ������ ��Ʈ��ũ�� ����
            byte[] winData = new byte[2];
            winData[0] = 255; // Ư���� �޽��� Ÿ���� ��Ÿ���� ��
            winData[1] = (byte)markWinner;
            network.Send(winData, winData.Length);
            Debug.Log("�¸�: " + (int)markWinner);
        }

        markTurn = (markTurn == Mark.White) ? Mark.Black : Mark.White;
    }

    // ���� ���� ����
    void UpdateEnd()
    {
        // �ִϸ����Ͱ� ���� ��� ������Ʈ
        if (hostAnimator == null || guestAnimator == null)
        {
            UpdateAnimators();
        }

        // ���а� �����Ǹ� �ִϸ��̼� Ʈ���� ����
        if (network.IsHost())
        {
            // ȣ��Ʈ�� ���
            if (markWinner == markHost)
            {
                // ȣ��Ʈ �¸�
                if (hostAnimator != null) hostAnimator.SetTrigger("Victory");
                if (guestAnimator != null) guestAnimator.SetTrigger("Defeat");
                Debug.Log("[TicTakTok] Host Won!");
            }
            else if (markWinner == markGuest)
            {
                // �Խ�Ʈ �¸�
                if (hostAnimator != null) hostAnimator.SetTrigger("Defeat");
                if (guestAnimator != null) guestAnimator.SetTrigger("Victory");
                Debug.Log("[TicTakTok] Guest Won!");
            }
        }
        else
        {
            // �Խ�Ʈ�� ���
            if (markWinner == markGuest)
            {
                // �Խ�Ʈ �¸�
                if (hostAnimator != null) hostAnimator.SetTrigger("Defeat");
                if (guestAnimator != null) guestAnimator.SetTrigger("Victory");
                Debug.Log("[TicTakTok] Guest Won!");
            }
            else if (markWinner == markHost)
            {
                // ȣ��Ʈ �¸�
                if (hostAnimator != null) hostAnimator.SetTrigger("Victory");
                if (guestAnimator != null) guestAnimator.SetTrigger("Defeat");
                Debug.Log("[TicTakTok] Host Won!");
            }
        }

        // ���º��� ��� ó�� (������)
        if (IsBoardFull() && markWinner == Mark.None)
        {
            if (hostAnimator != null) hostAnimator.SetTrigger("Draw");
            if (guestAnimator != null) guestAnimator.SetTrigger("Draw");
            Debug.Log("[TicTakTok] Draw Game!");
        }

        resetBtn.SetActive(true);
    }

    // �¸� ���� Ȯ���ϴ� �Լ�
    Mark CheckBoard()
    {
        // ���� 2���ϱ� 2�� ���°�
        for (int i = 0; i < 2; i++)
        {
            // ��, ������ ó��
            int s;
            if (i == 0)
                s = (int)Mark.White;
            else
                s = (int)Mark.Black;

            // ���ι��� ó��
            if (s == board[0] && s == board[1] && s == board[2])
                return (Mark)s;
            if (s == board[3] && s == board[4] && s == board[5])
                return (Mark)s;
            if (s == board[6] && s == board[7] && s == board[8])
                return (Mark)s;

            // ���ι��� ó��
            if (s == board[0] && s == board[3] && s == board[6])
                return (Mark)s;
            if (s == board[1] && s == board[4] && s == board[7])
                return (Mark)s;
            if (s == board[2] && s == board[5] && s == board[8])
                return (Mark)s;

            // �밢������ ó��
            if (s == board[0] && s == board[4] && s == board[8])
                return (Mark)s;
            if (s == board[2] && s == board[4] && s == board[6])
                return (Mark)s;
        }
        return Mark.None;
    }

    // ���尡 ���� á���� Ȯ���ϴ� �޼��� (���º� üũ��)
    private bool IsBoardFull()
    {
        foreach (int cell in board)
        {
            if (cell == (int)Mark.None)
                return false;
        }
        return true;
    }

    bool SetStone(int i, Mark stone)
    {
        if (board[i] == (int)Mark.None)
        {
            board[i] = (int)stone;
            return true;
        }
        return false;
    }

    int PosToNumber(Vector3 pos)
    {
        // ���콺 ��ġ���� ������ ��ġ�� ���� ���� �������� ����� ��ġ ���
        float x = pos.x - boardPosition.x;
        float y = Screen.height - pos.y - boardPosition.y;

        // ���� ������ ��� ��� ó��
        if (x < 0.0f || x >= boardSize)
            return -1;

        if (y < 0.0f || y >= boardSize)
            return -1;

        // cellSize�� ������ ���� ��ġ ���
        int h = (int)(x / cellSize);
        int v = (int)(y / cellSize);

        // ��ȿ�� ���� üũ
        if (h >= 3 || v >= 3)
            return -1;

        int i = v * 3 + h;
        return i;
    }

    // �ڱ� �� ó���ϱ�
    bool MyTurn()
    {
        // ���콺 ���� ��ư�� Ŭ�� ���� Ȯ��
        bool bClick = Input.GetMouseButtonDown(0);
        // Ŭ���� �ȵ� ��� ��ȿ���� �����ϱ� ƨ�ܳ���(���)
        if (!bClick)
        {
            return false;
        }

        // pixel coordinates : y���� �츮�� �����ϴ� �Ͱ� �ݴ���
        Vector3 pos = Input.mousePosition;

        int i = PosToNumber(pos);
        if (i == -1)
        {
            return false;
        }

        bool bSet = SetStone(i, markHost);
        if (bSet == false)
        {
            return false;
        }

        // ��Ʈ��ũ�� ���濡�� �� ������ �� ��ȭ�� ������
        byte[] data = new byte[1];
        data[0] = (byte)i;
        network.Send(data, data.Length);

        Debug.Log("���� : " + i);

        return true;
    }

    // ��� �� ó��
    bool YourTurn()
    {
        byte[] data = new byte[2];
        int iSize = network.Receive(ref data, data.Length);

        if (iSize <= 0) return false;

        if (data[0] == 254)
        {
            PerformReset();
            return true;
        }

        // Ư���� �޽������� Ȯ��
        if (data[0] == 255)
        {
            markWinner = (Mark)data[1];
            state = State.End;
            return true;
        }

        int i = (int)data[0];
        Debug.Log("����: " + i);

        bool ret = SetStone(i, markGuest);
        if (ret == false) return false;

        return true;
    }

    public void ResetGame()
    {
        // ���� ������ ���濡�� ����
        byte[] resetData = new byte[2];
        resetData[0] = 254; // ������ ��Ÿ���� Ư���� ��
        resetData[1] = 0;
        network.Send(resetData, resetData.Length);

        // ���� ���� ����
        PerformReset();
    }

    private void PerformReset()
    {
        state = State.Start;

        for (int i = 0; i < board.Length; i++)
        {
            // ���忡 �ƹ��͵� ���� �ʱ�ȭ���ֱ�
            board[i] = (int)Mark.None;
        }

        // ȭ�� �߾� ��ǥ ���
        float screenCenterX = Screen.width * 0.5f;
        float screenCenterY = Screen.height * 0.5f;

        // ������ �»�� ��ǥ ���
        boardPosition = new Vector2(
            screenCenterX - (boardSize * 0.5f),
            screenCenterY - (boardSize * 0.5f)
        );

        characterSpawn = FindObjectOfType<CharacterSpawn>();
        if (characterSpawn == null)
        {
            Debug.LogError("[TicTakTok] CharacterSpawn component not found!");
        }

        if (hostAnimator != null)
        {
            hostAnimator.ResetTrigger("Victory");
            hostAnimator.ResetTrigger("Defeat");
            hostAnimator.ResetTrigger("Draw");
        }

        if (guestAnimator != null)
        {
            guestAnimator.ResetTrigger("Victory");
            guestAnimator.ResetTrigger("Defeat");
            guestAnimator.ResetTrigger("Draw");
        }

        resetBtn.SetActive(false); // ���� �� ��ư ��Ȱ��ȭ
    }
}
