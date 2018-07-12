using System;
using UnityEngine;

[Serializable]
public class CRoom {

	public string roomName;
	public CPlayerData[] roomPlayes;
	public string roomDisplay;
	public int[] matchingMap;

	public CRoom()
	{
		this.roomName = string.Empty;
		this.roomPlayes = new CPlayerData[0];
		this.roomDisplay = string.Empty;
		this.matchingMap = new int[0];
	}
	
}
