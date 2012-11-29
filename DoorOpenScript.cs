using UnityEngine;
using System.Collections;

public class DoorOpenScript : MonoBehaviour {
	
	public Object target;
	public GameObject source;
	public int triggerCount = 1;
	public bool openDoor = false;
	public bool repeatTrigger = false;
	public bool triggered = false;
	
	void OpenDoor(){
		triggerCount--;
		
		if (!openDoor || repeatTrigger){
			Object currentTarget = target != null ? target : gameObject;
			Behaviour targetBehaviour = currentTarget as Behaviour;
			GameObject targetGameObject = currentTarget as GameObject;
			if (targetBehaviour != null)
				targetGameObject = targetBehaviour.gameObject;
			
			targetGameObject.gameObject.transform.Rotate(new Vector3(0, 0, 100));
			openDoor = true;
		}
	}
	
	void CloseDoor(){
		triggerCount++;
		
		if(openDoor || repeatTrigger){
			Object currentTarget = target != null ? target : gameObject;
			Behaviour targetBehaviour = currentTarget as Behaviour;
			GameObject targetGameObject = currentTarget as GameObject;
			if (targetBehaviour != null)
				targetGameObject = targetBehaviour.gameObject;	
			
			targetGameObject.gameObject.transform.Rotate(new Vector3(0, 0, -100));
			openDoor = false;
		}
	}
	
	void OnTriggerEnter(Collider other){
		triggered = true;
	}
	
	void OnTriggerExit(Collider other){
		triggered = false;
	}
	
	void Update(){
		if(triggered && Input.GetButtonDown("Use")){
			if(!openDoor)
				OpenDoor();
			else
				CloseDoor();
		}
	}
}
