﻿using System;
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
	public Transform trackingSpace;
	public Transform head;
	public LineRenderer laser;
	
	[Serializable]
	public class WireData
	{
		public float x, y, z, u, v, w, cx, cy, cz, cu, cv, cw;
		public bool isTeleporting, isTeleportingFromHead;
		public string id;
		public WireData(float x, float y, float z, float u, float v, float w, string id, float cx, float cy, float cz, float cu, float cv, float cw, bool isTeleporting, bool isTeleportingFromHead)
		{
			this.x = x;
			this.y = y;
			this.z = z;
			this.u = u;
			this.v = v;
			this.w = w;
			this.id = id;
			this.cx = cx;
			this.cy = cy;
			this.cz = cz;
			this.cu = cu;
			this.cv = cv;
			this.cw = cw;
			this.isTeleporting = isTeleporting;
			this.isTeleportingFromHead = isTeleportingFromHead;
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

		var cp = Vector3.zero;
		var cr = Vector3.zero;
		OVRInput.Controller controller = OVRInput.GetConnectedControllers() & (OVRInput.Controller.LTrackedRemote | OVRInput.Controller.RTrackedRemote);
		if (controller != OVRInput.Controller.None)
		{
			cp = trackingSpace.localToWorldMatrix.MultiplyPoint(OVRInput.GetLocalControllerPosition(controller));
			cr = OVRInput.GetLocalControllerRotation(controller).eulerAngles;
		}
		var wd = new WireData(p.x, p.y, p.z, r.x, r.y, r.z, deviceId, cp.x, cp.y, cp.z, cr.x, cr.y, cr.z, laser.enabled, laser.name == "head");
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

	public Color ColorFromId(string id)
	{
		var hash = id.GetHashCode();
		int r = hash & 0xff;
		int g = (hash & 0xff00) >> 8;
		int b = (hash & 0xff0000) >> 16;
		return new Color(r / 255f, g / 255f, b / 255f);
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
					o.GetComponentInChildren<Renderer>().material.SetColor("_EmissionColor", ColorFromId(kv.Key));
				}
				var wd = kv.Value;
				var p = new Vector3(wd.x, wd.y, wd.z);
				var r = new Vector3(wd.u, wd.v, wd.w);
				o.transform.position = p;
				o.transform.rotation = Quaternion.Euler(r);
				var controller = o.transform.Find("GearVrController");
				var cp = new Vector3(wd.cx, wd.cy, wd.cz);
				var cr = new Vector3(wd.cu, wd.cv, wd.cw);
				// controller.position = cp;
				controller.rotation = Quaternion.Euler(cr);
				var l = o.GetComponent<LineRenderer>();
				l.enabled = wd.isTeleporting;
				if (wd.isTeleporting) {
					if (wd.isTeleportingFromHead)
					{
						l.SetPosition(0, o.transform.position);
						l.SetPosition(1, o.transform.position + o.transform.forward * 50000);
					} else
					{
						l.SetPosition(0, controller.position);
						l.SetPosition(1, controller.position + controller.rotation * Vector3.forward * 50000);
					}
				}
			}
		}
	}
}
