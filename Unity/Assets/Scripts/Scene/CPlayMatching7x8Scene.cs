﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CPlayMatching7x8Scene : MonoBehaviour {

	[SerializeField]	protected Animator m_Animator;
	[SerializeField]	protected Text m_RoonNameDisplay;
	[SerializeField]	protected CUIPlayerInRoom[] m_DisplayPlayers;

	protected CPlayer m_Player;
	protected CGameManager m_GameManager;

	protected bool m_IsStartGame = false;

	protected virtual void Start() {
		this.m_Player = CPlayer.GetInstance();
		this.m_GameManager = CGameManager.GetInstance();
		this.SetupPlayers();
		InvokeRepeating("SetupPlayers", 0f, 1f);
	}

	protected virtual void SetupPlayers() {
		#if UNITY_DEBUG
		Debug.Log ("SetupPlayers");
		#endif
		var currentRoom = this.m_Player.room;
		var maximumPlayer = currentRoom.roomPlayes.Length > 2 ? 2 : currentRoom.roomPlayes.Length;
		for (int i = 0; i < maximumPlayer; i++) {
			this.m_DisplayPlayers[i].SetPlayerName (currentRoom.roomPlayes[i].name);
		}
		this.m_RoonNameDisplay.text = currentRoom.roomName;
		if (maximumPlayer >= 2) {
			this.PlayAnimStartGame ();
			var turnIndex = this.m_GameManager.turnIndex;
			this.m_DisplayPlayers[0].SetInTurnActive (!turnIndex);
			this.m_DisplayPlayers[1].SetInTurnActive (turnIndex);
		}
	}

	protected virtual void PlayAnimStartGame() {
		if (this.m_IsStartGame)
			return;
		if (this.m_Animator != null) {
			this.m_Animator.SetTrigger ("StartGame");
		}
		Invoke("PlayFirstTurn", 1);
		this.m_IsStartGame = true;
	}

	public virtual void PlayFirstTurn() {
		this.m_GameManager.AnimationYourTurn();
	}

}
