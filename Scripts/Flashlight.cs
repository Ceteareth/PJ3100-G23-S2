using UnityEngine;
using System.Collections;

public class Flashlight : MonoBehaviour
{
	void Update ()
	{
		if(Input.GetButtonDown("Fire1")){
			transform.audio.Play();
			if(transform.light.intensity > 0)
				transform.light.intensity = 0;
			else
				transform.light.intensity = 1.42f;
		}
	}
}

