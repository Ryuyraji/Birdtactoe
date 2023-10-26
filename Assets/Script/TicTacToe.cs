using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class TicTacToe : MonoBehaviour
{
    // 게임 상태를 정의하는 열거형
    enum State
    {
        Start = 0,  // 게임 시작 전 상태
        Game,       // 게임 진행 중 상태
        End,        // 게임 종료 상태
    }

    // 바둑돌의 종류를 정의하는 열거형
    enum Stone
    {
        None = 0,   // 빈 칸
        White,      // 흰 돌
        Black,      // 검은 돌
    }

    // 턴을 정의하는 열거형
    enum Turn
    {
        I = 0,      // 내 턴
        You,        // 상대방의 턴
    }

    // Tcp 통신 관련 변수 및 입력 필드
    Tcp tcp;
    public InputField ip;

    // 게임 보드와 바둑돌 텍스처를 저장할 변수들
    public Texture texBoard;
    public Texture texWhite;
    public Texture texBlack;
    public Texture texWhitewin;
    public Texture texBlackwin;
    public AudioClip winSound;
    public AudioClip stoneSound; // 바둑알을 놓을 때 재생할 음향 파일
    public AudioSource stoneSource;
    public AudioSource winSource; 


    // 3x3 크기의 게임 보드를 나타내는 배열
    int[] board = new int[9];

    State state;          // 현재 게임 상태
    Stone stoneTurn;      // 현재 턴에 놓일 바둑돌의 종류
    Stone stoneI;         // 서버(나)의 바둑돌 종류
    Stone stoneYou;       // 클라이언트(상대방)의 바둑돌 종류
    Stone stoneWinner;    // 승리한 플레이어의 바둑돌 종류

    void Start()
    {
        tcp = GetComponent<Tcp>();
        state = State.Start;
        stoneSource = gameObject.AddComponent<AudioSource>();
        winSource = gameObject.AddComponent<AudioSource>();
        stoneSource.clip = stoneSound; // 효과음 파일 지정
        winSource.clip = winSound;
        stoneSource.playOnAwake = false; // 즉시 재생하지 않도록 설정
        winSource.playOnAwake = false;
        // 보드 초기화: 모든 칸을 빈 상태로 설정
        for (int i = 0; i < board.Length; ++i)
        {
            board[i] = (int)Stone.None;
        }
        
    }

    // 서버 시작 버튼 클릭 시 호출되는 함수
    public void ServerStart()
    {
        tcp.StartServer(10000, 10);
    }

    // 클라이언트 시작 버튼 클릭 시 호출되는 함수
    public void ClientStart()
    {
        tcp.Connect(ip.text, 10000);
    }

    void Update()
    {
        if (!tcp.IsConnect()) return;

        // 현재 게임 상태에 따라 업데이트 처리
        if (state == State.Start)
            UpdateStart();

        if (state == State.Game)
            UpdateGame();

        if (state == State.End)
            UpdateEnd();
    }

    // 게임 시작 상태의 업데이트 처리
    void UpdateStart()
    {
        // 게임 모드 상태로 변경
        state = State.Game;
        // 첫 번째 턴은 흰 돌 차례
        stoneTurn = Stone.White;

        // 서버(나)의 바둑돌 종류 설정
        // 서버(나) 바둑돌 = 흰돌
        if (tcp.IsServer())
        {
            stoneI = Stone.White;
            stoneYou = Stone.Black;
        }
        // 클라이언트(상대방)의 바둑돌 종류 설정
        // 클라이언트(상대) 바둑돌 = 검은돌
        else
        {
            stoneI = Stone.Black;
            stoneYou = Stone.White;
        }
    }
   
    // 게임 진행 상태의 업데이트 처리
    void UpdateGame()
    {
        bool bSet = false;

        // 현재 턴에 따라 해당 플레이어의 차례 처리
        if (stoneTurn == stoneI)
            bSet = MyTurn();
        else
            bSet = YourTurn();

        if (bSet == false)
            return;

        // 바둑돌 배치 후 게임 승리 여부 확인
        stoneWinner = CheckBoard();

        // 게임 승리 상태인 경우 게임 종료 상태로 변경
        if (stoneWinner != Stone.None)
        {
            state = State.End;
            Debug.Log("승리: " + (int)stoneWinner);
        }
        if (!winSource.isPlaying)
        {
            winSource.PlayOneShot(winSound);
        }
        // 턴 교체
        stoneTurn = (stoneTurn == Stone.White) ? Stone.Black : Stone.White;
    }

    // 게임 종료 상태의 업데이트 처리 (비어 있음)
    void UpdateEnd()
    {
        // 종료 상태에서 필요한 업데이트 로직을 추가할 수 있음
    }
    void WinSound()
    {
        winSource.Play();
    }
    // 바둑돌을 배치하는 함수
    bool SetStone(int i, Stone stone)
    {
        // 배치하려는 칸에 스톤이 배치되지 않았을 때
        if (board[i] == (int)Stone.None)
        {
            // 칸에 스톤 배치
            board[i] = (int)stone;
            return true;
        }
        
        // 배치하려는 칸에 스톤이 이미 배치되어 있으면 false 리턴
        return false;
    }
    // 틱택토 판에서 배치하려는 위치를 파악
    // 마우스 클릭 시 위치 값에 맞는 board 인덱스 값을 리턴
    // 마우스 클릭 위치를 게임 보드의 인덱스로 변환하는 함수
    int PosToNumber(Vector3 pos)
    {   // 실제 위치값과 마우스의 위치값은 축의 방향이 달라 역으로 계산해야 함
        float x = pos.x - 660;
        float y = Screen.height - 240 - pos.y;

        // 유효한 클릭 위치인지 확인
        if (x < 0.0f || x >= 600.0f) return -1;
        if (y < 0.0f || y >= 600.0f) return -1;

        // 클릭 위치를 게임 보드의 인덱스로 변환
        int h = (int)(x / 200.0f);
        int v = (int)(y / 200.0f);
        int i = v * 3 + h;

        return i;
    }
    // 내 턴 처리 함수
    bool MyTurn()
    {
        // 마우스 왼쪽 버튼이 클릭되었는지 확인
        bool bClick = Input.GetMouseButtonDown(0);


        // 마우스 왼쪽 버튼이 클릭되지 않았으면 false 리턴
        if (!bClick) return false;

        // 마우스 클릭 위치를 게임 보드의 인덱스로 변환
        Vector3 pos = Input.mousePosition;
        int i = PosToNumber(pos);

        // 유효한 클릭 위치인지 확인
        if (i == -1) return false;

        // 바둑돌을 배치하고 상대방에게 클릭한 위치 정보 전송
        bool bSet = SetStone(i, stoneI);
        if (bSet == false) return false;

        byte[] data = new byte[1];
        data[0] = (byte)i;
        tcp.Send(data, data.Length);
        Debug.Log("보냄" + i);
        PlaystoneSound();
        return true;
    }
    void PlaystoneSound()
    {
        if (stoneSource != null && stoneSound != null)
        {
            stoneSource.Play();
        }
    }

    // 상대방 턴 처리 함수
    bool YourTurn()
    {
        // 상대방으로부터 데이터를 수신
        byte[] data = new byte[1];
        int iSize = tcp.Receive(ref data, data.Length);

        // 데이터 수신이 실패하면 false 리턴
        if (iSize <= 0) return false;

        // 수신한 데이터를 게임 보드의 인덱스로 변환
        int i = (int)data[0];
        Debug.Log("받음:" + i);

        // 바둑돌을 배치하고 성공적으로 배치되었으면 true 리턴
        bool ret = SetStone(i, stoneYou);
        if (ret == false) return false;

        return true;
    }

    // 게임 보드에서 승리 조건을 체크하는 함수
    Stone CheckBoard()
    {
        for (int i = 0; i < 2; i++)
        {
            int s;
            if (i == 0)
                s = (int)Stone.White;
            else
                s = (int)Stone.Black;

            // 가로 방향 체크
            if (s == board[0] && s == board[1] && s == board[2])
                return (Stone)s;
            if (s == board[3] && s == board[4] && s == board[5])
                return (Stone)s;
            if (s == board[6] && s == board[7] && s == board[8])
                return (Stone)s;
            // 세로 방향 체크
            if (s == board[0] && s == board[3] && s == board[6])
                return (Stone)s;
            if (s == board[1] && s == board[4] && s == board[7])
                return (Stone)s;
            if (s == board[2] && s == board[5] && s == board[8])
                return (Stone)s;
            // 대각선 방향 체크
            if (s == board[0] && s == board[4] && s == board[8])
                return (Stone)s;
            if (s == board[2] && s == board[4] && s == board[6])
                return (Stone)s;
        }

        // 틱택토 체크 시 일치하는 부분이 없으면 None 리턴
        return Stone.None;
    }

    // 바둑돌을 배치하는 함수
    // 이벤트 발생 시 매 프레임마다 호출 (Update보다 후위)
    private void OnGUI()
    {
        if (!Event.current.type.Equals(EventType.Repaint))
            return;
        // 게임 보드 텍스처 그리기
        Graphics.DrawTexture(new Rect(660, 240, 600, 600), texBoard);

        // 게임 보드에 배치된 바둑돌 텍스처 그리기
        for (int i = 0; i < board.Length; ++i)
        {
            if (board[i] != (int)Stone.None)
            {
                float x = 660 + (i % 3) * 200;
                float y = 240 + (i / 3) * 200;
                Texture tex = (board[i] == (int)Stone.White) ? texWhite : texBlack;
                Graphics.DrawTexture(new Rect(x, y, 200, 200), tex);
            }
        }

        // 게임 종료 상태에서 승리한 플레이어의 표시를 그림
        if (state == State.End)
        {
            if (stoneWinner == Stone.White)
            {
                Graphics.DrawTexture(new Rect(200, 400, 250, 250), texWhite);
                Graphics.DrawTexture(new Rect(200, 200, 250, 250), texWhitewin);
            }
            else if (stoneWinner == Stone.Black)
            {
                Graphics.DrawTexture(new Rect(1400, 400, 250, 250), texBlack);
                Graphics.DrawTexture(new Rect(1400, 200, 250, 250), texBlackwin);
            }
        }


    }
}

