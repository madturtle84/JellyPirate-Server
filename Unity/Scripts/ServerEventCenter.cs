//#define SERVER_DEBUG

using System.Collections;
using UnityEngine;
using SocketIO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SimpleJSON;
using System;

public class ServerEventCenter : MonoBehaviour {

	// Public Variables
	public int roomNumber=8;
	public string testEvent = "";

	// Private Variables
	private SocketIOComponent socket;
	private bool _isStarted = false;
	private int numOfPlayer = 4; // Maybe it will change ?

	// The delegate used for event callback functions
	// Note: We could have unlimited parameters if using param here, 
	//       but then the callback have to use param as well, which looks more complicated
	public delegate void P2UEventDelegate (string p0="",string p1="",string p2="",string p3="",string p4="",string p5="");
	private Dictionary<string,P2UEventDelegate> p2uEvents = new Dictionary<string,P2UEventDelegate>(); // A collection that store all of the callback functions

	// Same things for local events
	public delegate void LocalEventDelegate(string p0,string p1,string p2,string p3,string p4,string p5);
	private Dictionary<string,LocalEventDelegate> localEvents = new Dictionary<string, LocalEventDelegate>();

	// Getters
	public bool isStarted{
		get { return _isStarted;}
	}

	void Start () {	
		socket=GetComponent<SocketIOComponent>();
		if(!socket) {
			print ("ERROR: Socket component not found!");
			return;
		}
		socket.On("open", TestOpen);
		socket.On("error", TestError);
		socket.On("close", TestClose);
		socket.On ("p2u event", ProcessP2UEvent);	
		socket.On ("unity joined", OnRoomAccess);
		socket.On ("unity denied", OnRoomDenied);
		StartCoroutine(LoginServer(roomNumber));
	}
	
	private IEnumerator LoginServer(int roomNumber){
		//TODO: Server should send a msg back to ensure the connection is established. Then we don't need the delay.
		float loginDelay = 2f;
		float endTime = Time.realtimeSinceStartup + loginDelay;
		while(Time.realtimeSinceStartup<endTime || !socket.IsConnected){
			yield return null;
		}
		//print ("[EVENT CENTER] END of while loop, Sending login request. currentTime = "+Time.realtimeSinceStartup + ", endTime = "+endTime);
		socket.Emit("unity login", new JSONObject(roomNumber));
	}

	private void OnRoomAccess(SocketIOEvent e){
		_isStarted = true; print ("[EventCenter] Room Access Confirmed!");
	}

	private void OnRoomDenied(SocketIOEvent e){
		print ("[EventCenter] Room " +roomNumber+" Access Denied! "+e.ToString());
	}

	private void ProcessP2UEvent(SocketIOEvent e){

		// Initialize local variables
		Dictionary <string, string> data = new Dictionary<string, string>();
		data = e.data.ToDictionary();
		string eventName = data["type"];
		string[] parameters=new string[7];
		int paramIdx=0;

		// Parse json data
		foreach(KeyValuePair<string, string> pair in data){
			parameters[paramIdx]=pair.Value;
			paramIdx++;
		}

		if(p2uEvents[eventName] != null) p2uEvents[eventName].DynamicInvoke(parameters[1],parameters[2],parameters[3],parameters[4],parameters[5],parameters[6]);
	}
	
	public void RegisterP2UEvent(string evtName, P2UEventDelegate callback ){
		//print ("registering event: " +evtName);
		if(!p2uEvents.ContainsKey(evtName))	p2uEvents.Add(evtName,callback);
		else p2uEvents[evtName] += callback;
	}	

	public void UnregisterP2UEvent(string evtName, P2UEventDelegate callback){
		p2uEvents[evtName] -= callback;
	}


	public void SendU2PEvent(int receiverID, string eventName, params string[] pList){
		// Initialize local variables
		Dictionary <string, string> data = new Dictionary<string, string>();

		// Write and pack the parameters to the dictionary data.
		data.Add("type",eventName);
		data.Add ("pid",receiverID.ToString());
		for(int i=0; i<pList.Length; i++){
			data.Add("p"+i.ToString(),pList[i]); // Note that the name of other parameter doesn't matter anymore.
		}

		// Emit event to server
		socket.Emit ("u2p event", new JSONObject(data));
	}

	public void SendU2PEventAll(string eventName, params string[] pList){
		for(int i=0; i<numOfPlayer; i++){
			SendU2PEvent(i,eventName,pList);
		}
	}


	// ========= Must do this!  ==================
	public void SendLocalEvent(string eventName, params string[] pList){
		string[] p = new string[6];
		int numList = pList.Length;
		for(int i=0; i<6; i++){
			if(i>numList-1) p[i]="";
			else p[i] = pList[i];
		}
		   
		if(localEvents.ContainsKey(eventName)){
			localEvents[eventName].DynamicInvoke(p[0],p[1],p[2],p[3],p[4],p[5]);
		}
		else print ("[SendLocalEvnet] Key "+eventName+" not existed in current list.");


	}

	public void RegisterLocalEvent(string evtName, LocalEventDelegate callback ){
		if(!localEvents.ContainsKey(evtName)) localEvents.Add(evtName,callback);
		else localEvents[evtName] += callback;
	}




	
	//========= Connection Messages =============
	public void TestOpen(SocketIOEvent e)	{
		print ("[EventCenter] On Opened"+e.ToString());
	}
	
	public void TestError(SocketIOEvent e)	{
		Debug.Log("[SocketIO] Error received: " + e.ToString());
	}
	
	public void TestClose(SocketIOEvent e)	{	
		Debug.Log("[SocketIO] Close received: " + e.ToString());
	}

	//The connection can't destroy when reloading, but we should refresh all event when restarting the game.
	void OnLevelWasLoaded(int level) {
		p2uEvents = new Dictionary<string, P2UEventDelegate>();
		localEvents = new Dictionary<string, LocalEventDelegate>();
	}

	// ============= For debugging ============
	void Update(){
		if(Input.GetKeyDown(KeyCode.F4)) SendU2PEventAll(testEvent);
	}







	// TODO: Delete old stuffs
	//=======================================================================================================
	// == Old stuff, obsoleted ==

	public delegate void ShootEventDelegate(int receiverID, Vector2 shootingDirection); // Think of it like a type definition. Call back function must have the same argument types
	public static event ShootEventDelegate OnShootEvent;
	
	public delegate void CannonEventDelegate(int receiverID, Vector2 shootingDirection); // Think of it like a type definition. Call back function must have the same argument types
	public static event CannonEventDelegate OnCannonRotate;
	
	public delegate void WheelEventDelegate(float currentWheelRotationInDegree, float turningSpeed);
	public static event WheelEventDelegate OnWheelMove;
	
	public delegate void ThrusterEventDelegate(float thrusterPower);
	public static event ThrusterEventDelegate OnThrusterDown;
//	public static event ThrusterEventDelegate OnThrusterHold;
	public static event ThrusterEventDelegate OnThrusterUp;

	public void ProcessP2UEvent_Old(SocketIOEvent e){
		Dictionary <string, string> data = new Dictionary<string, string>();
		data = e.data.ToDictionary();
		string type=data["type"];
		#if SERVER_DEBUG
		print ("[P2U Event] :" +type);
		#endif
		switch(type){
		case "shoot":
//			print ("[P2U Event] :" +type+", start parsing...");
			Vector2 dir = new Vector2(float.Parse(data["dirX"]),float.Parse(data["dirY"]));
			if(OnShootEvent!=null) {
				OnShootEvent(int.Parse(data["pid"]),dir);
			}
			break;
		case "thrusterDown":
			if(OnThrusterDown!=null) OnThrusterDown(float.Parse(data["power"]));
			break;
		case "thrusterUp":
			if(OnThrusterUp!=null) OnThrusterUp(0);
			break;

		case "wheel":
			if(OnWheelMove!=null) OnWheelMove(float.Parse(data["rotation"]),float.Parse(data["speed"]));
			//print ("[OnWheelMoveEvent] "+float.Parse(data["speed"]));
			break;
		case "rotateCannon":
			Vector2 cannonDir = new Vector2(float.Parse(data["dirX"]),float.Parse(data["dirY"]));
			if(OnCannonRotate!=null) OnCannonRotate(int.Parse(data["pid"]),cannonDir);
			break;
		}		
	}


	public void RegisterOnShootEvent(ShootEventDelegate callback ){
		OnShootEvent += callback;
	}

	public void RegisterCannonRotateEvent(CannonEventDelegate callback ){
		OnCannonRotate += callback;
	}

	public void RegisterOnWheelMoveEvent(WheelEventDelegate callback){
		OnWheelMove += callback;
	}

	public void RegisterThrusterDownEvent(ThrusterEventDelegate callback){
		OnThrusterDown+=callback;
	}
	public void RegisterThrusterUpEvent(ThrusterEventDelegate callback){
		OnThrusterUp+=callback;
	}


	// Other events
//	public void SendU2PEvent(string eventName){
//		socket.Emit(eventName);
//	}

}