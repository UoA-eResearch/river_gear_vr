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
	public GameObject playerPrefab;
	public Dictionary<string, WireData> currentData;
	public Transform head;

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
		currentData = new Dictionary<string, WireData>();
		InvokeRepeating("BroadcastPosition", 0f, 0.1f);
		Debug.Log("Listening on " + port);
		udp.EnableBroadcast = true;
		udp.BeginReceive(new AsyncCallback(OnUdpData), udp);
	}

	void BroadcastPosition()
	{
		MemoryStream ms = new MemoryStream();
		var p = head.position;
		var r = head.rotation.eulerAngles;
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
		try
		{
			// points towards whoever had sent the message:
			IPEndPoint source = new IPEndPoint(0, 0);
			// get the actual message and fill out the source:
			byte[] buffer = socket.EndReceive(result, ref source);
			Debug.Log("Got " + buffer.Length + " bytes from " + source);
			var ms = new MemoryStream(buffer);
			WireData wd = (WireData)bf.Deserialize(ms);
			currentData[wd.id] = wd;
		} catch (Exception e)
		{
			Debug.LogError(e.Message);
		}
		// schedule the next receive operation once reading is done:
		socket.BeginReceive(new AsyncCallback(OnUdpData), socket);
	}
	
	// Update is called once per frame
	void Update () {
		foreach(var kv in currentData)
		{
			if (kv.Key != deviceId || !ignoreSelf)
			{
				var o = GameObject.Find(kv.Key);
				if (o == null)
				{
					o = Instantiate(playerPrefab);
					o.name = kv.Key;
				}
				var wd = kv.Value;
				var p = new Vector3(wd.x, wd.y, wd.z);
				var r = new Vector3(wd.u, wd.v, wd.w);
				o.transform.position = p;
				o.transform.rotation = Quaternion.Euler(r);
			}
		}
	}
}
