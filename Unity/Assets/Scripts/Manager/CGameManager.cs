using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSingleton;
using SocketIO;

public class CGameManager : CMonoSingleton<CGameManager> {

	#region Fields
	[Header("Items config")]
	[SerializeField]	protected Sprite[] m_Items;

	[Header("Map config")]
	// TURN INDEX.
	// TRUE is RED. FALSE is BLUE.
	[SerializeField]	protected bool m_TurnIndex = false;
	public bool turnIndex { 
		get { return this.m_TurnIndex; } 
		set { this.m_TurnIndex = value; } 
	}

	[SerializeField]	protected int m_CheckValue = 5;
	[SerializeField]	protected int m_MapColumn = 7;
	[SerializeField]	protected int m_MapRow = 8;
	[SerializeField]	protected GameObject m_ChessRoot;
	[SerializeField]	protected CChess[] m_ListChesses;
	public CChess[] listChesses {
		get { return this.m_ListChesses; }
	}

	protected CChess[,] m_MapChesses;
	public CChess[,] mapChesses {
		get { return this.m_MapChesses; }
	}

	[Header("Turn config")]
	[SerializeField]	protected int m_MatchCount = 0;
	public int matchCount {
		get { return this.m_MatchCount; }
		set { this.m_MatchCount = value; }
	}
	[SerializeField]	protected CChess m_MatchChess1;
	[SerializeField]	protected CChess m_MatchChess2;


	protected CPlayer m_Player;

	protected bool m_IsGameEnd = false;

	#endregion

	#region MonoBehaviour Implementation

	protected override void Awake()
	{
		base.Awake();
	}

	protected virtual void Start() {
		this.m_Player = CPlayer.GetInstance();
		this.m_Player.socket.Off ("receiveMatchingPosition", this.OnReceiveMatchingPosition);
		this.m_Player.socket.On ("receiveMatchingPosition", this.OnReceiveMatchingPosition);
		this.m_ListChesses = this.m_ChessRoot.GetComponentsInChildren<CChess>();
		this.InitGame ();
		this.OnStartGame ();
	}

	#endregion

	#region Main methods

	public virtual void InitGame() {
		this.m_TurnIndex = false;
		this.m_MapChesses = new CChess[this.m_MapColumn, this.m_MapRow];
		var matchingMap = this.m_Player.room.matchingMap;
		for (int y = 0; y < this.m_MapRow; y++)
		{
			for (int x = 0; x < this.m_MapColumn; x++)
			{
				var index = (y * this.m_MapColumn) + x;
				var cell = this.m_ListChesses[index];
				cell.posX = x;
				cell.posY = y;
				this.m_MapChesses[x, y] = cell;
				var itemIndex = matchingMap[index];
				cell.SetItem (this.m_Items[itemIndex]);
			}
		}
		this.m_IsGameEnd = false;
	}

	#endregion

	#region State Game

	public virtual void OnStartGame() {

	}

	public virtual void OnUpdateGame(int x, int y) {
		if (this.IsLocalTurn()) {
			if (this.m_MatchCount == 0) {
				this.m_MatchCount = 1;
				this.m_MatchChess1 = this.m_MapChesses[x, y];
				this.m_MatchChess1.SetState(this.m_TurnIndex ? CChess.EChessState.RED : CChess.EChessState.BLUE, true);
			} else if (this.m_MatchCount == 1) {
				this.m_MatchCount = 2;
				this.m_MatchChess2 = this.m_MapChesses[x, y];
				this.m_MatchChess2.SetState(this.m_TurnIndex ? CChess.EChessState.RED : CChess.EChessState.BLUE, true);
				this.m_Player.SendMatchingSpot(
					this.m_MatchChess1.posX, this.m_MatchChess1.posY,
					this.m_MatchChess2.posX, this.m_MatchChess2.posY
				);
			}
		} else {
			this.m_Player.ShowMessage ("This is not your turn.");
		}
		this.CheckTurn();
	}

	protected virtual void OnReceiveMatchingPosition(SocketIOEvent e) {
		var x1 = int.Parse (e.data.GetField("pos1X").ToString());
		var y1 = int.Parse (e.data.GetField("pos1Y").ToString());
		var x2 = int.Parse (e.data.GetField("pos2X").ToString());
		var y2 = int.Parse (e.data.GetField("pos2Y").ToString());
		var turnIndex = int.Parse (e.data.GetField("turnIndex").ToString());
		var result = int.Parse (e.data.GetField("result").ToString()) == 1;
		var chess1 = this.m_MapChesses[x1, y1];
		var chess2 = this.m_MapChesses[x2, y2];
		this.m_TurnIndex = turnIndex == 1;
		chess1.SetState(this.m_TurnIndex ? CChess.EChessState.RED : CChess.EChessState.BLUE, true);
		chess2.SetState(this.m_TurnIndex ? CChess.EChessState.RED : CChess.EChessState.BLUE, true);
		chess1.UpdateResult (result);
		chess2.UpdateResult (result);
		this.m_MatchChess1 = null;
		this.m_MatchChess2 = null;
		this.m_MatchCount = 0;
		this.CheckTurn();
	}

	public virtual void OnEndGame() {
		#if UNITY_DEBUG
		Debug.Log ("Is end game");
		#endif
		var blueScore = 0;
		var redScore = 0;
		for (int i = 0; i < this.m_ListChesses.Length; i++)
		{
			var cell = this.m_ListChesses[i];
			if (cell.chessState == CChess.EChessState.BLUE) {
				blueScore ++;
			} 
			if (cell.chessState == CChess.EChessState.RED) {
				redScore ++;
			}
		}
		var displayScore = string.Format ("...Score...\nBLUE: {0}\nRED: {1}", blueScore / 2f, redScore / 2f);
		this.m_Player.ShowMessage (displayScore, this.OnResetGame);
		this.m_IsGameEnd = true;
	}

	public virtual void OnResetGame() {
		this.m_Player.LeaveRoom();
	}

	#endregion

	#region Logics game

	public virtual void CheckTurn() {
		var isEndGame = true;
		for (int i = 0; i < this.m_ListChesses.Length; i++)
		{
			var cell = this.m_ListChesses[i];
			if (cell.chessState == CChess.EChessState.None) {
				isEndGame = false;
				break;
			}
		}
		if (isEndGame) {
			this.OnEndGame();
		}
	}

	#endregion

	#region Getter && Setter

	public virtual bool IsLocalTurn() {
		return this.m_TurnIndex == (this.m_Player.playerData.turnIndex == 1);
	}

	public virtual void SetTurn (bool value) {
		this.m_TurnIndex = value;
	}

	public virtual bool IsRed() {
		return this.m_TurnIndex == true;
	}

	public virtual bool IsBlue() {
		return this.m_TurnIndex == false;
	}

	#endregion

}
