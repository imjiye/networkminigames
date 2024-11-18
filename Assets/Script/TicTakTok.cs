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

    // 바둑이 놓일 위치 9개로 만들기
    int[] board = new int[9];

    private CharacterSpawn characterSpawn;
    private Animator hostAnimator;
    private Animator guestAnimator;

    State state; // 게임 진행 상황

    Mark markTurn; // 현재 턴 체크하기 위한 변수
    Mark markHost; // 호스트의 마크
    Mark markGuest; // 게스트의 마크
    Mark markWinner; // 승리자 마크

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
        resetBtn.SetActive(false);
        state = State.Start;

        for (int i = 0; i < board.Length; i++)
        {
            // 보드에 아무것도 없게 초기화해주기
            board[i] = (int)Mark.None;
        }

        // 화면 중앙 좌표 계산
        float screenCenterX = Screen.width * 0.5f;
        float screenCenterY = Screen.height * 0.5f;

        // 보드의 좌상단 좌표 계산
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

    // 캐릭터 애니메이터 가져오기
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

    // 보드랑 마크 그리기
    private void OnGUI()
    {
        if (!Event.current.type.Equals(EventType.Repaint))
            return;

        // 중앙에 보드 그리기
        Graphics.DrawTexture(new Rect(boardPosition.x, boardPosition.y, boardSize, boardSize), texBoard);

        // 돌 그리기 - 크기와 위치 조정
        for (int i = 0; i < board.Length; i++)
        {
            if (board[i] != (int)Mark.None)
            {
                // 각 칸의 왼쪽 상단 좌표 계산
                float cellX = boardPosition.x + (i % 3) * cellSize;
                float cellY = boardPosition.y + (i / 3) * cellSize;

                // 마크를 칸의 중앙에 위치시키기 위한 오프셋 계산
                float offsetX = (cellSize - markSize) * 0.5f;
                float offsetY = (cellSize - markSize) * 0.5f;

                // 최종 마크 위치 계산
                float markX = cellX + offsetX;
                float markY = cellY + offsetY;

                Texture tex = (board[i] == (int)Mark.White) ? texWhite : texBlack;
                Graphics.DrawTexture(new Rect(markX, markY, markSize, markSize), tex);
            }
        }

        // 턴 표시도 같은 크기로 조정
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

        // 승리 표시도 같은 크기로 조정
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

    // 게임 시작하기 상태
    void UpdateStart()
    {
        // 상태를 시작상태로 초기화
        state = State.Game;

        // 첫 턴의 시작은 화이트돌을 가진 서버부터
        markTurn = Mark.White;

        // 내가 서버인 경우
        if (network.IsHost())
        {
            markHost = Mark.White;
            markGuest = Mark.Black;
        }
        // 내가 클라이언트인 경우
        else
        {
            markHost = Mark.Black;
            markGuest = Mark.White;
        }
    }

    // 게임을 진행중인 상태
    void UpdateGame()
    {
        bool bSet = false;

        // 내 돌과 현재 턴인 돌이 같을 경우
        if (markTurn == markHost)
        {
            bSet = MyTurn();
        }
        else
        {
            bSet = YourTurn();
        }

        // 세팅된게 없는 경우 리턴
        if (bSet == false)
        {
            return;
        }

        markWinner = CheckBoard();

        if (markWinner != Mark.None)
        {
            state = State.End;
            // 승리 정보를 네트워크로 전송
            byte[] winData = new byte[2];
            winData[0] = 255; // 특별한 메시지 타입을 나타내는 값
            winData[1] = (byte)markWinner;
            network.Send(winData, winData.Length);
            Debug.Log("승리: " + (int)markWinner);
        }

        markTurn = (markTurn == Mark.White) ? Mark.Black : Mark.White;
    }

    // 게임 종료 상태
    void UpdateEnd()
    {
        // 애니메이터가 없는 경우 업데이트
        if (hostAnimator == null || guestAnimator == null)
        {
            UpdateAnimators();
        }

        // 승패가 결정되면 애니메이션 트리거 설정
        if (network.IsHost())
        {
            // 호스트인 경우
            if (markWinner == markHost)
            {
                // 호스트 승리
                if (hostAnimator != null) hostAnimator.SetTrigger("Victory");
                if (guestAnimator != null) guestAnimator.SetTrigger("Defeat");
                Debug.Log("[TicTakTok] Host Won!");
            }
            else if (markWinner == markGuest)
            {
                // 게스트 승리
                if (hostAnimator != null) hostAnimator.SetTrigger("Defeat");
                if (guestAnimator != null) guestAnimator.SetTrigger("Victory");
                Debug.Log("[TicTakTok] Guest Won!");
            }
        }
        else
        {
            // 게스트인 경우
            if (markWinner == markGuest)
            {
                // 게스트 승리
                if (hostAnimator != null) hostAnimator.SetTrigger("Defeat");
                if (guestAnimator != null) guestAnimator.SetTrigger("Victory");
                Debug.Log("[TicTakTok] Guest Won!");
            }
            else if (markWinner == markHost)
            {
                // 호스트 승리
                if (hostAnimator != null) hostAnimator.SetTrigger("Victory");
                if (guestAnimator != null) guestAnimator.SetTrigger("Defeat");
                Debug.Log("[TicTakTok] Host Won!");
            }
        }

        // 무승부인 경우 처리 (선택적)
        if (IsBoardFull() && markWinner == Mark.None)
        {
            if (hostAnimator != null) hostAnimator.SetTrigger("Draw");
            if (guestAnimator != null) guestAnimator.SetTrigger("Draw");
            Debug.Log("[TicTakTok] Draw Game!");
        }

        resetBtn.SetActive(true);
    }

    // 승리 조건 확인하는 함수
    Mark CheckBoard()
    {
        // 돌이 2개니까 2번 도는것
        for (int i = 0; i < 2; i++)
        {
            // 흰돌, 검은돌 처리
            int s;
            if (i == 0)
                s = (int)Mark.White;
            else
                s = (int)Mark.Black;

            // 가로방향 처리
            if (s == board[0] && s == board[1] && s == board[2])
                return (Mark)s;
            if (s == board[3] && s == board[4] && s == board[5])
                return (Mark)s;
            if (s == board[6] && s == board[7] && s == board[8])
                return (Mark)s;

            // 세로방향 처리
            if (s == board[0] && s == board[3] && s == board[6])
                return (Mark)s;
            if (s == board[1] && s == board[4] && s == board[7])
                return (Mark)s;
            if (s == board[2] && s == board[5] && s == board[8])
                return (Mark)s;

            // 대각선방향 처리
            if (s == board[0] && s == board[4] && s == board[8])
                return (Mark)s;
            if (s == board[2] && s == board[4] && s == board[6])
                return (Mark)s;
        }
        return Mark.None;
    }

    // 보드가 가득 찼는지 확인하는 메서드 (무승부 체크용)
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
        // 마우스 위치에서 보드의 위치를 빼서 보드 내에서의 상대적 위치 계산
        float x = pos.x - boardPosition.x;
        float y = Screen.height - pos.y - boardPosition.y;

        // 보드 영역을 벗어난 경우 처리
        if (x < 0.0f || x >= boardSize)
            return -1;

        if (y < 0.0f || y >= boardSize)
            return -1;

        // cellSize로 나누어 격자 위치 계산
        int h = (int)(x / cellSize);
        int v = (int)(y / cellSize);

        // 유효한 범위 체크
        if (h >= 3 || v >= 3)
            return -1;

        int i = v * 3 + h;
        return i;
    }

    // 자기 턴 처리하기
    bool MyTurn()
    {
        // 마우스 왼쪽 버튼의 클릭 여부 확인
        bool bClick = Input.GetMouseButtonDown(0);
        // 클릭이 안된 경우 유효하지 않으니까 튕겨내기(방어)
        if (!bClick)
        {
            return false;
        }

        // pixel coordinates : y값이 우리가 생각하는 것과 반대임
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

        // 네트워크로 상대방에게 내 턴으로 한 변화를 보내줌
        byte[] data = new byte[1];
        data[0] = (byte)i;
        network.Send(data, data.Length);

        Debug.Log("보냄 : " + i);

        return true;
    }

    // 상대 턴 처리
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

        // 특별한 메시지인지 확인
        if (data[0] == 255)
        {
            markWinner = (Mark)data[1];
            state = State.End;
            return true;
        }

        int i = (int)data[0];
        Debug.Log("받음: " + i);

        bool ret = SetStone(i, markGuest);
        if (ret == false) return false;

        return true;
    }

    public void ResetGame()
    {
        // 리셋 정보를 상대방에게 전송
        byte[] resetData = new byte[2];
        resetData[0] = 254; // 리셋을 나타내는 특별한 값
        resetData[1] = 0;
        network.Send(resetData, resetData.Length);

        // 실제 리셋 로직
        PerformReset();
    }

    private void PerformReset()
    {
        state = State.Start;

        for (int i = 0; i < board.Length; i++)
        {
            // 보드에 아무것도 없게 초기화해주기
            board[i] = (int)Mark.None;
        }

        // 화면 중앙 좌표 계산
        float screenCenterX = Screen.width * 0.5f;
        float screenCenterY = Screen.height * 0.5f;

        // 보드의 좌상단 좌표 계산
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

        resetBtn.SetActive(false); // 리셋 후 버튼 비활성화
    }
}
