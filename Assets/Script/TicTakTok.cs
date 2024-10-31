using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TicTakTok : MonoBehaviour
{
    enum State
    {
        Start = 0,
        Game,
        End,
    };

    enum Turn
    {
        I = 0,
        You,
    }

    enum Stone
    {
        None = 0,
        White,
        Black,
    }

    Network network;

    public Texture texBoard;
    public Texture texWhite;
    public Texture texBlack;

    int[] board = new int[9];

    State state;

    Stone stoneTurn;
    Stone stoneI;
    Stone stoneYou;
    Stone stoneWinner;

    // Start is called before the first frame update
    void Start()
    {
        network = GetComponent<Network>();

        state = State.Start;

        for(int i =0; i < board.Length; i++)
        {
            board[i] = (int)Stone.None;
        }
    }

    private void OnGUI()
    {
        if (!Event.current.type.Equals(EventType.Repaint))
            return;

        Graphics.DrawTexture(new Rect(0, 0, 400, 400), texBoard);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
