using UnityEngine;
using System.Collections;

public class CustomInteractionScript : MonoBehaviour {
	
	public Object target;
	private bool openDoor = false;
	public bool repeatTrigger = false;
	public bool triggered = false;
	public bool isDoor = true;
	private int pickedPaintings = 0;
	private bool displayMessage = false;
	private bool itemPresent = true;
	string message;
	
	void Start(){
		Screen.showCursor = false;	
	}
	
	void OpenDoor(){
		
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
	
	void PickUpPainting(){
		Object currentTarget = target != null ? target : gameObject;
		Behaviour targetBehaviour = currentTarget as Behaviour;
		GameObject targetGameObject = currentTarget as GameObject;
		if (targetBehaviour != null)
			targetGameObject = targetBehaviour.gameObject;
		targetGameObject.SetActive(false);
		pickedPaintings++;
		itemPresent = false;
		
		message = "Got a painting!";
		displayMessage = true;
		StartCoroutine(wait());
		
	}
	
	void OnGUI(){
		if(displayMessage) {
		 	GUI.Label (new Rect(Screen.width / 2, Screen.height / 4f, 200, 200), message);
		}
		if(triggered && itemPresent){
			GUI.Label (new Rect(Screen.width / 2, Screen.height / 4f, 200, 200), "Press E to interact");	
		}
	}
	
	void OnTriggerEnter(Collider other){
		triggered = true;
	}
	
	void OnTriggerExit(Collider other){
		triggered = false;
	}
	
	void Update() {
		if(triggered && Input.GetButtonDown("Use")){
			if(!openDoor && isDoor)
				OpenDoor();
			else if(openDoor && isDoor)
				CloseDoor();
			else {
				PickUpPainting();
			}
		}
	}
	
	IEnumerator wait(){
		displayMessage = true;
		yield return new WaitForSeconds(3);
		displayMessage = false;
	}
}
