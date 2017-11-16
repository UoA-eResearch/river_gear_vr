using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Controller : MonoBehaviour {
	public LineRenderer laser;
	public Transform trackingSpace;
	public Transform head;
	// Update is called once per frame
	void Update()
	{
		var x = Input.GetAxis("Mouse X");
		var y = Input.GetAxis("Mouse Y");
		if (x > .1 && transform.localScale.magnitude < 1000f)
		{
			transform.localScale *= 1.1f;
		} else if (x < -.1 && transform.localScale.magnitude > 1f)
		{
			transform.localScale *= .9f;
		}
		if (y > .1 && transform.position.y < 10000)
		{
			transform.position += Vector3.up;
		}
		else if (y < -.1 && transform.position.y > -10)
		{
			transform.position -= Vector3.up;
		}

		if (Input.GetButtonDown("Fire1"))
		{
			laser.enabled = true;
		}
		if (laser.enabled)
		{
			Vector3 start = Vector3.zero;
			Vector3 end = Vector3.zero;
			OVRInput.Controller controller = OVRInput.GetActiveController() & (OVRInput.Controller.LTrackedRemote | OVRInput.Controller.RTrackedRemote);
			if (controller != OVRInput.Controller.None)
			{
				var orientation = OVRInput.GetLocalControllerRotation(controller);
				var localStartPoint = OVRInput.GetLocalControllerPosition(controller);
				var localEndPoint = localStartPoint + ((orientation * Vector3.forward) * 50000.0f);
				Matrix4x4 localToWorld = trackingSpace.localToWorldMatrix;
				start = localToWorld.MultiplyPoint(localStartPoint);
				end = localToWorld.MultiplyPoint(localEndPoint);
			}
			else
			{
				start = head.position - Vector3.up * transform.localScale.magnitude * .1f;
				end = start + head.forward * 50000f;
			}

			// Create new ray
			RaycastHit hit;
			if (Physics.Raycast(start, end, out hit))
			{
				end = hit.point;
				if (Input.GetButtonUp("Fire1"))
				{
					Debug.Log("Hit " + hit.collider.name);
					transform.position = new Vector3(hit.point.x, transform.position.y, hit.point.z);
				}
			}
			laser.SetPosition(0, start);
			laser.SetPosition(1, end);
			laser.widthMultiplier = transform.localScale.magnitude * .02f;
		}
		if (Input.GetButtonUp("Fire1"))
		{
			laser.enabled = false;
		}
	}
}
