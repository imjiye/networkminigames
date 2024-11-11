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
        None = 0, // ���� ���� ��
        Ready,    // ���� ���� 
        Game,     // ���� �÷��� ��
        Result,   // ��� ǥ���ϱ�
        End,      // ���� �����ϱ�
        Disconnect, // ���� ����
    }

    enum Turn
    {
        Host = 0,
        Guest,
    }

    enum Winner
    {
        None = 0, // ���� ���� ���̶� �¸��ڰ� ���� ��Ȳ
        Left, // ȣ��Ʈ �¸�
        Right, // �Խ�Ʈ �¸�
        Tie, // ���º�
    }

    enum Mark
    {
        None = 0,
        Left,
        Right,
    }

    Network network;

    public Texture texBoard;
    public Texture texLeft;
    public Texture texRight;
    public Texture texYou;
    public Texture texWin;
    public Texture texLose;

    private const int setNum = 3; // ĭ�� ����

    private const float waitTime = 1.0f; // ���ս��� �� ��ȣ ǥ�� �ð�
    private const float turnTime = 10.0f; // �� ��� �ð�

    private float timer; // ���� �ð�
    private float curTime; // ���� ��� �ð�
    private float stepcount = 0.0f;

    private Winner winner; // ����

    private bool isGameOver;

    int[] board = new int[setNum * setNum];

    State state; // ���� ���� ��Ȳ

    Mark markTurn; // ���� �� üũ�ϱ� ���� ����
    Mark hostMark; // ȣ��Ʈ�� ��ũ
    Mark guestMark; // �Խ�Ʈ�� ��ũ

    private static float Space_Width = 400.0f;
    private static float Space_Height = 400.0f;

    private static float Window_Width = 640.0f;
    private static float Window_Height = 480.0f;

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
        if(network != null)
        {
            network.RegisterEventHandler(EventCallback);
        }

        ResetGame();
        isGameOver = false;
        timer = turnTime;
    }

    // ����� ��ũ �׸���
    private void OnGUI()
    {
        switch(state)
        {
            case State.Ready:
                DrawBoardAndMarks();
                break;

            case State.Game:
                DrawBoardAndMarks();
                if(markTurn == hostMark)
                {
                    DrawTime();
                }
                break;

            case State.Result:
                DrawBoardAndMarks();
                DrawWinner();
                {
                    GUISkin skin = GUI.skin;
                    GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
                    style.normal.textColor = Color.white;
                    style.fontSize = 25;

                    if(GUI.Button(new Rect(Screen.width/2-100, Screen.height/2, 200, 100), "End", style))
                    {
                        state = State.End;
                        stepcount = 0.0f;
                    }
                }
                break;

            case State.End:
                DrawBoardAndMarks();
                DrawWinner();
                break;

            case State.Disconnect:
                DrawBoardAndMarks();
                NotifyDisconnection();
                break;

            default:
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (network == null)
        {
            network = NetworkManager.Instance.GetNetwork();
            return;
        }

        if (!network.IsConnect()) return;

        switch (state)
        {
            case State.Ready:
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
        // ���� ���� ��ȣ ǥ�� ���
        curTime += Time.deltaTime;

        if(curTime > waitTime)
        {
            // ���� ���� ���·� ����
            state = State.Game;

            // Host�� ���� ������ �����ϰ� ��
            markTurn = Mark.Left;

            // �� ��ũ�� ������ ��ũ�� ����
            if (network.IsHost())
            {
                hostMark = Mark.Left;
                guestMark = Mark.Right;
            }
            else
            {
                hostMark = Mark.Right;
                guestMark = Mark.Left;
            }
        }       
    }

    // ������ �������� ����
    void UpdateGame()
    {
        bool bSet = false;

        // �� ����
        if(markTurn == hostMark)
        {
            bSet = MyTurn();
        }
        // ��� ����
        else
        {
            bSet = YourTurn();
        }

        // ���� �� �����ϱ�
        if(bSet == false)
        {
            return;
        }

        // �¸����� Ȯ��
        winner = CheckBoard();

        // �¸��ڰ� �ִ� ���
        if(winner != Winner.None)
        {
            // ���� ����
            state = State.Result;
        }

        // ���� �����ϱ�
        markTurn = (markTurn == Mark.Left) ? Mark.Right : Mark.Left;
        timer = turnTime;
    }

    // ���� ���� ����
    void UpdateEnd()
    {
        stepcount += Time.deltaTime;
        if(stepcount > 1.0f)
        {
            ResetGame();
            isGameOver = true;
        }
    }

    // �¸� ���� Ȯ���ϴ� �Լ�
    Winner CheckBoard()
    {
        string spaceString = "";
        for(int i = 0; i < board.Length; i++)
        {
            spaceString += board[i] + "|";
            if(i % setNum == setNum - 1)
            {
                spaceString += " ";
            }
        }
        Debug.Log(spaceString);

        // ���ι��� Ȯ��
        for(int y = 0; y < setNum; y++)
        {
            int mark = board[y * setNum];
            int num = 0;
            for(int x = 0; x < setNum; x++)
            {
                int index = y * setNum + x;
                if(mark == board[index])
                {
                    ++num;
                }
            }

            if(mark != -1 && num == setNum)
            {
                return (mark == 0) ? Winner.Left : Winner.Right;
            }
        }

        // ���ι��� Ȯ��
        for(int x = 0; x < setNum; x++)
        {
            int mark = board[x];
            int num = 0;
            for(int y = 0;y < setNum; y++)
            {
                int index = y * setNum + x;
                if(mark == board[index])
                {
                    ++num;
                }
            }

            if(mark != -1 && num == setNum)
            {
                return (mark == 0) ? Winner.Left : Winner.Right;
            }
        }

        // ���� �밢�� Ȯ��
        {
            int mark = board[0];
            int num = 0;
            for (int xy = 0; xy < setNum; xy++)
            {
                int index = xy * setNum + xy;
                if (mark == board[index])
                {
                    ++num;
                }
            }

            if (mark != -1 && num == setNum)
            {
                return (mark == 0) ? Winner.Left : Winner.Right;
            }
        }

        // ������ �밢�� Ȯ��
        {
            int mark = board[setNum - 1];
            int num = 0;
            for (int xy = 0; xy < setNum; xy++)
            {
                int index = xy * setNum - xy - 1;
                if (mark == board[index])
                {
                    ++num;
                }
            }

            if (mark != -1 && num == setNum)
            {
                return (mark == 0) ? Winner.Left : Winner.Right;
            }
        }

        // ������ üũ
        {
            int num = 0;
            foreach(int b in board)
            {
                if(b == -1)
                {
                    ++num;
                }
            }
            if(num == 0)
            {
                return Winner.Tie;
            }
        }
        return Winner.None;
    }

    void ResetGame()
    {
        markTurn = Mark.Left;
        state = State.None;

        for(int i = 0; i < board.Length; i++)
        {
            board[i] = -1;
        }
    }

    void DrawBoardAndMarks()
    {
        float sx = Space_Width;
        float sy = Space_Height;

        Rect rect = new Rect(Screen.width / 2 - Window_Width * 0.5f, Screen.height / 2 - Window_Width * 0.5f, 
            Window_Width, Window_Height);
        Graphics.DrawTexture(rect, texBoard);

        float left = ((float)Screen.width - sx) * 0.5f;
        float top = ((float)Screen.height - sy) * 0.5f;

        for(int i = 0; i < board.Length;i++)
        {
            if(board[i] != -1)
            {
                int x = i % setNum;
                int y = i / setNum;

                float divide = (float)setNum;
                float px = left + x * sx / divide;
                float py = top + y * sy / divide;

                Texture texture = (board[i] == 0) ?  texLeft : texRight;

                float ofs = sx / divide * 0.1f;

                Graphics.DrawTexture(new Rect(px+ofs, py+ofs, sx * 0.8f / divide, sy * 0.8f / divide), texture);
            }
        }

        if(hostMark == markTurn)
        {
            float offset = (hostMark == Mark.Left) ? -94.0f : sx + 36.0f;
            rect = new Rect(left + offset, top + 5.0f, 68.0f, 136.0f);
            Graphics.DrawTexture(rect, texYou);
        }
    }

    void DrawTime()
    {
        GUIStyle style = new GUIStyle();
        style.fontSize = 35;
        style.fontStyle = FontStyle.Bold;

        string str = "Time : " + timer.ToString("F3");

        style.normal.textColor = (timer > 5.0f) ? Color.white : Color.red;
        GUI.Label(new Rect(222, 5, 200, 100), str, style);
    }

    void DrawWinner()
    {
        float sx = Space_Width;
        float sy = Space_Height;
        float left = ((float)Screen.width - sx) * 0.5f;
        float top = ((float)Screen.height - sy) * 0.5f;

        float offset = (hostMark == Mark.Left) ? -94.0f : sx + 36.0f;
        Rect rect = new Rect(left + offset, top + 5.0f, 68.0f, 136.0f);
        Graphics.DrawTexture(rect, texYou);

        rect.y += 140.0f;

        if((hostMark == Mark.Left && winner == Winner.Left) ||(hostMark == Mark.Right && winner == Winner.Right))
        {
            Graphics.DrawTexture(rect, texWin);
        }

        if((hostMark == Mark.Left && winner == Winner.Right) || (hostMark == Mark.Right && winner == Winner.Left))
        {
            Graphics.DrawTexture(rect, texLose);
        }
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
        float sx = Space_Width;
        float sy = Space_Width;

        float left = ((float)Screen.width - sx) * 0.5f;
        float top = ((float)Screen.height + sy) * 0.5f;

        float px = pos.x - left;
        float py = top - pos.y;

        if(px < 0.0f || px >= sx)
        {
            return -1;
        }

        if(py < 0.0f || py >= sy)
        {
            return -1;
        }

        float divide = (float)setNum;
        int h = (int)(px * divide / sx);
        int v = (int)(py * divide / sy);

        int index = v * setNum + h;

        return index;
    }

    // �ڱ� �� ó���ϱ�
    bool MyTurn()
    {
        int index = 0;

        timer -= Time.deltaTime;

        if(timer <= 0.0f)
        {
            // Ÿ�ӿ���
            timer = 0.0f;
            do
            {
                index = UnityEngine.Random.Range(0, 8);
            } while (board[index] != -1);
        }
        else
        {
            // ���콺 ���� ��ư�� ���� ���� Ȯ��
            bool bClick = Input.GetMouseButtonDown(0);
            if (!bClick)
            {
                // ������ �ʾҴٸ� �ƹ��͵� ���� ����
                return false;
            }

            // ������ ������ �������� ���õ� ĭ���� ��ȯ
            Vector3 pos = Input.mousePosition;

            index = PosToNumber(pos);
            if(index < 0)
            {
                return false;
            }
        }

        // ����� �����ߴٸ� ĭ�� ��ġ�ϱ�
        bool bSet = SetStone(index, hostMark);
        if(bSet == false)
        {
            return false;
        }

        // ���õ� ĭ�� ���� �۽�
        byte[] data = new byte[1];
        data[0] = (byte)index;
        network.Send(data, data.Length);

        Debug.Log("send: " + index);

        return true;
    }

    // ��� �� ó��
    bool YourTurn()
    {
        // ����� ���� ����
        byte[] data = new byte[1];
        int isSize = network.Receive(ref data, data.Length);

        // �����߿��� ��ġ���� ���ϰ� �ϱ�
        if (isSize <= 0)
        {
            return false;
        }

        // ������ ������ ���õ� ĭ���� ��ȯ
        int index = (int)data[0];
        Debug.Log("����: " + index);

        // ĭ�� ����
        bool ret = SetStone(index, guestMark);
        if (ret == false)
        {
            return false;
        }
        return true;
    }

    void NotifyDisconnection()
    {
        GUISkin skin = GUI.skin;
        GUIStyle style = new GUIStyle(GUI.skin.GetStyle("button"));
        style.normal.textColor = Color.white;
        style.fontSize = 25;

        float sx = 450;
        float sy = 200;
        float px = Screen.width / 2 - sx * 0.5f;
        float py = Screen.height / 2 - sy * 0.5f;

        string message = "ȸ���� ������ϴ�.";
        if(GUI.Button(new Rect(px, py, sx, sy), message, style))
        {
            ResetGame();
            isGameOver = true;
        }
    }

    public bool IsGameOver()
    {
        return isGameOver;
    }

    public void EventCallback(NetEventState progress)
    {
        switch (progress.type)
        {
            case NetEnvetType.Disconnect:
                if (state < State.Result && isGameOver == false)
                {
                    state = State.Disconnect;
                }
                break;
        }
    }
}
