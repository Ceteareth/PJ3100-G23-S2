using UnityEngine;
using System.Collections;

public class CustomInteractionScript : MonoBehaviour {
	
	public Object target;
	private bool openDoor = false;
	public bool triggered = false;
	public bool isDoor = true;
	private bool displayMessage = false;
	private bool itemPresent = true;
	string message;
	private Transform player;
	private Vector3 nextPos;
	private float distance;
	public bool hasKey;
	private bool busyMessage = false;

	void Start(){
		Screen.showCursor = false;
		player = GameObject.FindWithTag("Player").transform;
	}
	
	void OpenDoor(){
		if (!openDoor){
			Object currentTarget = target != null ? target : gameObject;
			Behaviour targetBehaviour = currentTarget as Behaviour;
			GameObject targetGameObject = currentTarget as GameObject;	
			
			if (targetBehaviour != null)
				targetGameObject = targetBehaviour.gameObject;
			
			if(targetGameObject.tag.ToLower() == "maindoor" && CharacterInventory.hasKey){
				targetGameObject.gameObject.transform.Rotate(new Vector3(0, 0, 100));
				openDoor = true;
			}
			
			else if(targetGameObject.tag.ToLower() == "maindoor" && !CharacterInventory.hasKey){
				message = "Need a key";
				displayMessage = true;
				StartCoroutine(wait());
			}
			
			else if(targetGameObject.tag.ToLower() != "maindoor") {
				targetGameObject.gameObject.transform.Rotate(new Vector3(0, 0, 100));
				openDoor = true;
			}
		}
	}
	
	void CloseDoor(){
		
		if(openDoor){
			Object currentTarget = target != null ? target : gameObject;
			Behaviour targetBehaviour = currentTarget as Behaviour;
			GameObject targetGameObject = currentTarget as GameObject;
			if (targetBehaviour != null)
				targetGameObject = targetBehaviour.gameObject;	
			
			targetGameObject.gameObject.transform.Rotate(new Vector3(0, 0, -100));
			openDoor = false;
		}
	}
	
	void PickUpItem(){
		Object currentTarget = target != null ? target : gameObject;
		Behaviour targetBehaviour = currentTarget as Behaviour;
		GameObject targetGameObject = currentTarget as GameObject;
		if (targetBehaviour != null)
			targetGameObject = targetBehaviour.gameObject;
		string itemType = targetGameObject.tag;
		
		if(itemType.ToLower().Equals("key")){
			CharacterInventory.hasKey = true;
		}
		
		itemPresent = false;
		
		message = "Got a " + itemType + "!";
		
		StartCoroutine(wait());
		targetGameObject.transform.localScale = new Vector3(0, 0, 0);
	}
	
	void DragItem(){
		Object currentTarget = target != null ? target : gameObject;
		Behaviour targetBehaviour = currentTarget as Behaviour;
		GameObject targetGameObject = currentTarget as GameObject;
		if (targetBehaviour != null)
			targetGameObject = targetBehaviour.gameObject;
		
		targetGameObject.gameObject.transform.rotation = Quaternion.Slerp(targetGameObject.gameObject.transform.rotation, Quaternion.LookRotation(player.position - targetGameObject.gameObject.transform.position), 1.0f*Time.deltaTime);
		targetGameObject.gameObject.transform.position += targetGameObject.gameObject.transform.forward * 1.0f * Time.deltaTime;
	}
	
	void OnGUI(){
		if(displayMessage) {
		 	GUI.Label (new Rect(Screen.width / 2, Screen.height / 4f, 200, 200), message);
		}
		if(triggered && itemPresent && !busyMessage){
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
				PickUpItem();
			}
		}
	}
	
	IEnumerator wait(){
		busyMessage = true;
		displayMessage = true;
		yield return new WaitForSeconds(3);
		displayMessage = false;
		busyMessage = false;
	}
}
