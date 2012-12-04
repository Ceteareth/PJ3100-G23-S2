using UnityEngine;
using System.Collections;

public class FlashlightFollowCamera : MonoBehaviour
{
	/*Dette scriptes legges på flashlighten istedenfor mouselook.
	 *Gjør at flashlighten følger kameraet nøyaktig.*/
	Transform camera;
	void Start ()
	{
		camera = GameObject.FindWithTag("MainCamera").transform;
	}
	
	void FixedUpdate ()
	{
		Vector3 newAngle = camera.eulerAngles - transform.eulerAngles;
		transform.Rotate (newAngle);
		transform.position = camera.position;
	}
}
