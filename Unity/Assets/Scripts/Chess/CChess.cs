using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Animator))]
public class CChess : MonoBehaviour {

	public enum EChessState: byte {
		None = 0,
		RED = 1,
		BLUE = 2
	}
	[SerializeField]	protected int m_X = 0;
	public int posX { 
		get { return this.m_X; } 
		set { this.m_X = value; }
	}
	[SerializeField]	protected int m_Y = 0;
	public int posY { 
		get { return this.m_Y; } 
		set { this.m_Y = value; }
	}
	[SerializeField]	protected EChessState m_ChessState = EChessState.None;
	public EChessState chessState { 
		get { return this.m_ChessState; } 
		set { this.m_ChessState = value; } 
	}

	[SerializeField]	protected Image m_ItemObject;
	[SerializeField]	protected GameObject m_RedObject;
	[SerializeField]	protected GameObject m_BlueObject;
	[SerializeField]	protected bool m_IsDisplay = false;
	public bool isDisplay { 
		get { return this.m_IsDisplay; } 
		set { this.m_IsDisplay = value; }
	}

	protected CGameManager m_GameManager;
	protected Button m_Button;
	protected Animator m_Animator;

	protected virtual void Awake()
	{
		this.m_Button = this.GetComponent<Button> ();
		this.m_Animator = this.GetComponent<Animator> ();
	}

	protected virtual void Start()
	{
		this.InitChess();
		this.m_GameManager = CGameManager.GetInstance ();
		this.m_Button.interactable = true;
		this.m_Button.onClick.RemoveListener (this.ChangeState);
		this.m_Button.onClick.AddListener (this.ChangeState);
	}

	public virtual void InitChess() {
		// UPDATE STATE
		this.m_RedObject.SetActive (false);
		this.m_BlueObject.SetActive (false);
	}

	public virtual void SetItem(Sprite value) {
		this.m_ItemObject.sprite = value;
	}

	public virtual void ChangeState() {
		// UPDATE GAMEMANAGER 
		this.m_GameManager.OnUpdateGame (this.posX, this.posY);
	}

	public virtual void PlayAnimation(bool value) {
		if (this.m_Animator != null) {
			this.m_Animator.SetBool("IsDisplay", value);
		}
	}

	public virtual void SetState(EChessState value, bool isDisplay = false) {
		if (this.m_ChessState != EChessState.None)
			return;
		// CHANGE STATE
		this.m_ChessState = value;
		// UPDATE STATE
		if (this.m_RedObject != null)
			this.m_RedObject.SetActive (this.m_ChessState == EChessState.RED);
		if (this.m_BlueObject != null)
			this.m_BlueObject.SetActive (this.m_ChessState == EChessState.BLUE);
		// DISABLE BUTTON
		this.m_Button.interactable = false;
		// PLAY ANIMATION
		this.PlayAnimation(isDisplay);
	}

	public virtual void UpdateResult(bool value) {
		// DISABLE BUTTON
		this.m_Button.interactable = !value;
		if (value == false) {
			this.m_ChessState = EChessState.None;
			this.m_RedObject.SetActive (false);
			this.m_BlueObject.SetActive (false);
		}
		StartCoroutine (this.HandleUpdateResult(value));
	}

	private WaitForSeconds m_DelayOneSecond = new WaitForSeconds(1f);
	private IEnumerator HandleUpdateResult(bool value) {
		yield return this.m_DelayOneSecond;
		this.PlayAnimation (value);
	}

	public override bool Equals(object obj)
	{
		if (obj == null || GetType() != obj.GetType())
		{
			return false;
		}
		return base.Equals (obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			int hash = 17;
			hash = hash * 23 + this.posX.GetHashCode();
			hash = hash * 23 + this.posY.GetHashCode();
			return hash;
		}
	}

}
