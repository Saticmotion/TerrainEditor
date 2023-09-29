using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMovement : MonoBehaviour
{
	void Update()
	{
		var keyboard = Keyboard.current;

		var delta = new Vector3();

		if (keyboard.aKey.isPressed)
		{
			delta.x -= 1f;
		}
		if (keyboard.dKey.isPressed)
		{
			delta.x += 1f;
		}
		if (keyboard.wKey.isPressed)
		{
			delta.z += 1f;
		}
		if (keyboard.sKey.isPressed)
		{
			delta.z -= 1f;
		}

		delta.y = -Mouse.current.scroll.ReadValue().y * .1f;

		transform.position += 5 * delta * Time.deltaTime;
	}
}
