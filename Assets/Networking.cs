using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

public class Networking : MonoBehaviour {

	UdpClient udp;
	public int port = 9001;
	IPEndPoint broadcastIp;
	BinaryFormatter bf = new BinaryFormatter();
	public bool ignoreSelf = true;
	string deviceId;

	[Serializable]
	public class WireData
	{
		public float x, y, z, u, v, w;
		public string id;
		public WireData(float x, float y, float z, float u, float v, float w, string id)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.u = u;
			this.v = v;
			this.w = w;
			this.id = id;
		}
	}

	// Use this for initialization
	void Start () {
		udp = new UdpClient(port);
		broadcastIp = new IPEndPoint(IPAddress.Broadcast, port);
		deviceId = SystemInfo.deviceUniqueIdentifier;
		InvokeRepeating("BroadcastPosition", 0f, 0.1f);
		Debug.Log("Listening on " + port);
		udp.EnableBroadcast = true;
		udp.BeginReceive(new AsyncCallback(OnUdpData), udp);
	}

	void BroadcastPosition()
	{
		MemoryStream ms = new MemoryStream();
		var p = transform.position;
		var r = transform.rotation.eulerAngles;
		var wd = new WireData(p.x, p.y, p.z, r.x, r.y, r.z, deviceId);
		bf.Serialize(ms, wd);
		var bytes = ms.ToArray();
		udp.Send(bytes, bytes.Length, broadcastIp);
		Debug.Log("broadcast " + bytes.Length + " bytes");
	}

	void OnUdpData(IAsyncResult result)
	{
		// this is what had been passed into BeginReceive as the second parameter:
		UdpClient socket = result.AsyncState as UdpClient;
		// points towards whoever had sent the message:
		IPEndPoint source = new IPEndPoint(0, 0);
		// get the actual message and fill out the source:
		byte[] buffer = socket.EndReceive(result, ref source);
		var ms = new MemoryStream(buffer);
		WireData wd = (WireData)bf.Deserialize(ms);
		if (wd.id != deviceId || !ignoreSelf)
		{
			var p = new Vector3(wd.x, wd.y, wd.z);
			var r = new Vector3(wd.u, wd.v, wd.z);
			Debug.Log(wd.id + " is at " + p + " with rotation " + r);
		}
		// schedule the next receive operation once reading is done:
		socket.BeginReceive(new AsyncCallback(OnUdpData), socket);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
