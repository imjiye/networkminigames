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
        None = 0, // 게임 시작 전
        Ready,    // 게임 시작 
        Game,     // 게임 플레이 중
        Result,   // 결과 표시하기
        End,      // 게임 종료하기
        Disconnect, // 연결 끊기
    }

    enum Turn
    {
        Host = 0,
        Guest,
    }

    enum Winner
    {
        None = 0, // 아직 시합 중이라 승리자가 없는 상황
        Left, // 호스트 승리
        Right, // 게스트 승리
        Tie, // 무승부
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

    private const int setNum = 3; // 칸의 숫자

    private const float waitTime = 1.0f; // 시합시작 전 신호 표시 시간
    private const float turnTime = 10.0f; // 턴 대기 시간

    private float timer; // 남은 시간
    private float curTime; // 현재 대기 시간
    private float stepcount = 0.0f;

    private Winner winner; // 승자

    private bool isGameOver;

    int[] board = new int[setNum * setNum];

    State state; // 게임 진행 상황

    Mark markTurn; // 현재 턴 체크하기 위한 변수
    Mark hostMark; // 호스트의 마크
    Mark guestMark; // 게스트의 마크

    private static float Space_Width = 400.0f;
    private static float Space_Height = 400.0f;

    private static float Window_Width = 640.0f;
    private static float Window_Height = 480.0f;

    void Awake()
    {
        // NetworkManager를 통해 Network 컴포넌트 참조 가져오기
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

    // 보드랑 마크 그리기
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

    // 게임 시작하기 상태
    void UpdateStart()
    {
        // 시합 시작 신호 표시 대기
        curTime += Time.deltaTime;

        if(curTime > waitTime)
        {
            // 게임 시작 상태로 변경
            state = State.Game;

            // Host가 먼저 게임을 시작하게 함
            markTurn = Mark.Left;

            // 내 마크와 상태의 마크를 설정
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

    // 게임을 진행중인 상태
    void UpdateGame()
    {
        bool bSet = false;

        // 내 차례
        if(markTurn == hostMark)
        {
            bSet = MyTurn();
        }
        // 상대 차례
        else
        {
            bSet = YourTurn();
        }

        // 놓을 곳 검토하기
        if(bSet == false)
        {
            return;
        }

        // 승리조건 확인
        winner = CheckBoard();

        // 승리자가 있는 경우
        if(winner != Winner.None)
        {
            // 게임 종료
            state = State.Result;
        }

        // 턴을 갱신하기
        markTurn = (markTurn == Mark.Left) ? Mark.Right : Mark.Left;
        timer = turnTime;
    }

    // 게임 종료 상태
    void UpdateEnd()
    {
        stepcount += Time.deltaTime;
        if(stepcount > 1.0f)
        {
            ResetGame();
            isGameOver = true;
        }
    }

    // 승리 조건 확인하는 함수
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

        // 가로방향 확인
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

        // 세로방향 확인
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

        // 왼쪽 대각선 확인
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

        // 오른쪽 대각선 확인
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

        // 비겼는지 체크
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

    // 자기 턴 처리하기
    bool MyTurn()
    {
        int index = 0;

        timer -= Time.deltaTime;

        if(timer <= 0.0f)
        {
            // 타임오버
            timer = 0.0f;
            do
            {
                index = UnityEngine.Random.Range(0, 8);
            } while (board[index] != -1);
        }
        else
        {
            // 마우스 완쪽 버튼의 눌린 상태 확인
            bool bClick = Input.GetMouseButtonDown(0);
            if (!bClick)
            {
                // 눌리지 않았다면 아무것도 하지 않음
                return false;
            }

            // 수신한 정보를 바탕으로 선택된 칸으로 변환
            Vector3 pos = Input.mousePosition;

            index = PosToNumber(pos);
            if(index < 0)
            {
                return false;
            }
        }

        // 제대로 선택했다면 칸에 배치하기
        bool bSet = SetStone(index, hostMark);
        if(bSet == false)
        {
            return false;
        }

        // 선택된 칸의 정보 송신
        byte[] data = new byte[1];
        data[0] = (byte)index;
        network.Send(data, data.Length);

        Debug.Log("send: " + index);

        return true;
    }

    // 상대 턴 처리
    bool YourTurn()
    {
        // 상대의 정보 수신
        byte[] data = new byte[1];
        int isSize = network.Receive(ref data, data.Length);

        // 수신중에는 배치하지 못하게 하기
        if (isSize <= 0)
        {
            return false;
        }

        // 수신한 정보를 선택된 칸으로 변환
        int index = (int)data[0];
        Debug.Log("받음: " + index);

        // 칸에 놓기
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

        string message = "회선이 끊겼습니다.";
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
