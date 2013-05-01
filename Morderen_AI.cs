using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/* ================
 *  BRUKSANVISNING
 * ================
 * 
 * *** REQUIREMENTS ***
 * Morderen MÅ ha en RigidBody component, med Mass "0.01", Drag og AngularDrag satt til "0", med Gravity enabled,
 * og alle 3 rotation-restrictions skrudd på. Men ikke position restrictions, de må være skrudd av.
 * Morderen MÅ også ha en Collider av noe slag. Capsule Colliders anbefales.
 * Playeren MÅ ha taggen "Player", og flashlighten hans MÅ ha taggen "Light"
 * NEW!!!
 * Morderen> construction_worker> Skinned Mesh Renderer> Bounds> Center BURDE(må) være 0, 0, 0
 * 
 * *** FEATURES ***
 * Dette pathfinding scriptet bruker nodes og line of sight til å finne objektet med taggen "Player"
 * Det kan bli puttet på et hvilket som helst objekt. Men hvis det det puttes på en instans av Construction Worker
 * MED componenten Animation(ikke Animator) spiller den også animasjonene korrekt.
 * 
 * For å lage pathfinding nodes: bare lag et objekt av hvilken som helst type, og gi det taggen "PathfindingNode"
 * Objektet burde ikke ha noen collision box eller rendering når spillet kjøres. Og den burde sveve ca 1m over gulvet.
 * 
 * NEW!!!
 * Det finnes clutter i levlet som morderen ikke skal kunne gå gjennom, men som likevel lar morderen
 * se playeren på andre siden. Sånn som kasser stabla oppå hverandre.
 * For å få morderen til å oppføre seg ordentlig rundt disse objektene lag et object med en collision box
 * som omslutter kikkehullene. Og gi objectet taggen "Clutter"
 * Da vil Morderen greie å se playeren, men pathfinde rundt istedenfor å prøve å gå gjennom som en retard.
 * 
 */

/* ========
 *  ISSUES
 * ========
 * 
 * Han kan teleportere til nodes som er umulige å komme til enda.
 * Teleportering blir iallefall brukt når checkpoint loades, så dette må fikses!
 * 
 */

public class Morderen_AI : MonoBehaviour {
	
	public enum MorderState
	{
		PATROL_STATE,
		SEE_PLAYER_STATE,
		SEARCH_FOR_PLAYER_STATE,
		BACK_OFF_STATE
	};
	
	// ========= PUBLIC =============
	public MorderState myState = MorderState.PATROL_STATE;
	public bool canKill = true;
	public float moveSpeed = 1.5F;
	public float killRange = 1f;
	public float maximumDistanceFromPlayer = 50F;
	public float flashlightFreezeRange = 10F;
	public float flashlightFreezeDrain = 0.5F;
	public string walkAudioFileName = "Footsteps-SoundBible.com-534261997";
	// =================================
	
	// disse er fine å ha public til debugging
	private bool seePlayer = false;
	private bool sawOrHeardPlayer = false;
	private bool beingLit = false;
	
	private Transform player;
	private Transform flashlight;
	private float distanceToPlayer;
	private Vector3 wherePlayerAt;//Disse vektorene beholder y-verdien til morderen, for å unngå at han flyr osv.
	private Vector3 pointToPlayer;//Vektor som peker fra morderen til playeren.
	private List<Node_Wrapper> nodeList = new List<Node_Wrapper>();
	private List<Node_Wrapper> currentPath = new List<Node_Wrapper>();
	private int pathStage = 0;
	private bool arrivedAtPathEnd = false;
	private  bool pathIsImpossible = false;
	private bool enterState = false;
	private Vector3 movementDestination = Vector3.zero;
	private float fieldOfView = 0.7F;
	private float extraGravity = -1F;
	private int waitToMove = 1000;
	private const int shortWait = 2000;
	private int beingStuckTimer = 0;
	private bool amStuck = false;
	private Vector3 stuckCheckPosition;
	private bool flashlightKills = false;
	private float flashlightKillRange = 4F;
	private float flashlightKillDrain = 10F;
	private Texture blackTex;
	private AudioSource walkingSound;

	
	void Start ()
	{
		player = GameObject.FindWithTag("Player").transform;
		flashlight = GameObject.FindWithTag("Light").transform;
		fieldOfView = 1F - fieldOfView;
		if(fieldOfView > 1 || fieldOfView < 0)
		{
			fieldOfView = 0.2F;
		}
		blackTex = (Texture2D) Resources.Load("BlackScreenTexture", typeof(Texture2D));
		stuckCheckPosition = transform.position;
		AudioSource[] sounds = transform.GetComponents<AudioSource>();
		foreach(AudioSource sound in sounds)
		{
			if(sound.clip.name == walkAudioFileName)
				walkingSound = sound;
		}
		InitializeNodes();
	}
	
	void Update ()
	{
		wherePlayerAt = new Vector3(player.position.x, transform.position.y, player.position.z);
		distanceToPlayer = Vector3.Distance(transform.position, wherePlayerAt);
		pointToPlayer = wherePlayerAt - transform.position;
		pointToPlayer.Normalize();
		
		// Important information gathering!
		CheckBeingLit();
		CheckSeePlayer();
		
		
		/* ============================
		 *  MORDERENs STATE-MACHINE!!!
		 * ============================
		 */
		// PATROL STATE
		// Morderen wanders randomly, hoping to bump into the player.
		if(myState == MorderState.PATROL_STATE)
		{
			if(enterState || arrivedAtPathEnd || pathIsImpossible || amStuck || distanceToPlayer > maximumDistanceFromPlayer * 1.5F)
			{
				if(arrivedAtPathEnd)
				{
					waitToMove = shortWait;
				}
				while(true)
				{
					int randomIndex = Random.Range(0, nodeList.Count - 1);
					if((nodeList[randomIndex].position - player.position).magnitude < maximumDistanceFromPlayer)
					{
						FindPathTo(nodeList[randomIndex].gameobject.transform);
						break;
					}
				}
			}
			FollowPath();
			WalkForward();
			
			enterState = false;
			// Transitions
			if(beingLit && distanceToPlayer < flashlightFreezeRange)
			{
				myState = MorderState.BACK_OFF_STATE;
				enterState = true;
			}
			else if(seePlayer)
			{
				myState = MorderState.SEE_PLAYER_STATE;
				enterState = true;
			}
			else if(sawOrHeardPlayer)
			{
				myState = MorderState.SEARCH_FOR_PLAYER_STATE;
				enterState = true;
			}
		}
		
		// SEE PLAYER STATE
		// Morderen can see the player, and walks towards them.
		else if(myState == MorderState.SEE_PLAYER_STATE)
		{
			sawOrHeardPlayer = true;
			waitToMove = 0;
			movementDestination = wherePlayerAt;
			
			if(beingLit && distanceToPlayer < flashlightKillRange && flashlightKills && Flashlight.batteryLife > 0)
			{
				Flashlight.batteryLife -= flashlightKillDrain;
			//	TeleportToSafety();
			}
			if(distanceToPlayer<=killRange && seePlayer)
			{
				rigidbody.velocity = Vector3.zero;
				// MURDER DEATH KILL!!!!!!!
				//if(canKill)
					//PlayerScript.LoadLastCheckpoint();
			}else{
				WalkForward();
			}
			
			enterState = false;
			// Transitions
			if(beingLit && distanceToPlayer < flashlightFreezeRange)
			{
				myState = MorderState.BACK_OFF_STATE;
				enterState = true;
			}
			else if(!seePlayer)
			{
				myState = MorderState.SEARCH_FOR_PLAYER_STATE;
				enterState = true;
			}
		}
		
		// SEARCH FOR PLAYER STATE
		// Morderen has some idea of where the player is, and proceeds to search for them.
		else if(myState == MorderState.SEARCH_FOR_PLAYER_STATE)
		{
			if(enterState)
			{
				FindPathTo(player);
				//ExtendCurrentPathTowards(player.position);
				// No no, write a function that logically guess a path extension! Take number of extensions as param.
			}
			
			// If Morderen couldn't find player.
			if(arrivedAtPathEnd || pathIsImpossible || amStuck)
				sawOrHeardPlayer = false;
			
			FollowPath();
			WalkForward();
			
			enterState = false;
			// Transitions
			if(beingLit && distanceToPlayer < flashlightFreezeRange)
			{
				myState = MorderState.BACK_OFF_STATE;
				enterState = true;
				sawOrHeardPlayer = false;
			}
			else if(seePlayer)
			{
				myState = MorderState.SEE_PLAYER_STATE;
				enterState = true;
			}
			else if(!sawOrHeardPlayer)
			{
				myState = MorderState.PATROL_STATE;
				enterState = true;
				waitToMove = shortWait * 2;
			}
		}
		
		// BACK OFF STATE
		// Morderen is sad and scared, and backs away from the player.
		else if(myState == MorderState.BACK_OFF_STATE)
		{
			// walk away from player...
			Flashlight.batteryLife -= Time.deltaTime * flashlightFreezeDrain;
			
			enterState = false;
			// Transitions
			if(!beingLit && distanceToPlayer < flashlightFreezeRange)
			{
				myState = MorderState.SEE_PLAYER_STATE;
				enterState = true;
			}
		}
		
		/* =============
		 *  OTHER STUFF
		 * =============
		 */
		
		// Mute / play walking sound and animation if standing still / walking
		AnimateMurderer();
		PlaySounds();
		rigidbody.AddForce(0, extraGravity, 0);
		
		// Update wait time
		if(waitToMove > 0)
		{
			waitToMove -= (int)(Time.deltaTime * 1000);
			if(waitToMove < 0)
				waitToMove = 0;
		}
		// Update the timer that checks if morderen is stuck
		if(beingStuckTimer > 2000 && waitToMove == 0)
		{
			if((stuckCheckPosition - transform.position).sqrMagnitude < 0.5F)
			{
				amStuck = true;
			}
			stuckCheckPosition = transform.position;
			beingStuckTimer = 0;
		}
		else
		{
			amStuck = false;
			beingStuckTimer += (int)(Time.deltaTime * 1000);
		}
		
		// ****** DEBUG STUFF ******
		for(int i = 0; i < currentPath.Count; i++)
		{
			//Debug.DrawLine(currentPath[i].position, currentPath[i].position + Vector3.up * 20, Color.green);
			if(!(i >= currentPath.Count - 1))
				Debug.DrawLine(currentPath[i].position, currentPath[i+1].position, Color.red);
		}
		// Draws ALL connections between nodes.
		for(int i = 0; i < nodeList.Count; i++)
		{
			for(int j = 0; j < nodeList[i].neighbours.Count; j++)
			{
				Debug.DrawLine(nodeList[i].position, nodeList[i].neighbours[j].position, Color.blue);
			}
		}
		// *** END OF DEBUG STUFF ***
	}
	
	// Paints screen black as murderer approaches.
	void OnGUI()
	{
		Color alpha = new Color(0, 0, 0, 0F);
		if(distanceToPlayer < 8F)
		{
			alpha = new Color(0, 0, 0, (8 - distanceToPlayer) / 8);
		}
		GUI.color = alpha;
	  	GUI.DrawTexture(new Rect(0,0,Screen.width, Screen.height), blackTex);
	}
	
	// Check if the player's noise reaches morderen!
	void OnTriggerStay(Collider collision)
	{
		// If morderen collides with player's "noise" object
		if(collision.gameObject.tag == "Player_noise")
		{
			sawOrHeardPlayer = true;
		}
	}
	
	private void FollowPath()
	{
		if(currentPath.Count >= 1)
		{
			// If pathstep is an illegal number. This should never happen!
			if(pathStage >= currentPath.Count || pathStage < 0 || currentPath.Count == 0)
			{
				Debug.LogError("Pathstep is illegal number, or FollowPath was called without a path!");
			}else{
				
				// If not on last stage of path.
				if(pathStage < currentPath.Count - 1)
				{
					if(IsTransformVisibleFrom(currentPath[pathStage + 1].position, transform))
					{
						pathStage++;
					}else{
						// But am very close to my current node...
						if(Vector3.Distance(currentPath[pathStage].position, transform.position) < 0.5F)
						{
							pathStage++;
						}
					}
				}else{
					if((transform.position - currentPath[pathStage].position).magnitude < 0.5F)
						arrivedAtPathEnd = true;
				}
				
				movementDestination = currentPath[pathStage].position;
			}
		}
	}
	
	// Returns true if murderer has a clear line of sight to player. False if not.
	private void CheckSeePlayer()
	{
		if(Vector3.Dot(transform.forward, (player.position - transform.position).normalized) > fieldOfView)
		{
			if(IsTransformVisibleFrom((transform.position + Vector3.up), player, new string[]{"Morderen"}))
			{
				seePlayer = true;
				return;
			}
			else if(IsTransformVisibleFrom((transform.position + Vector3.up), player, new string[]{"Morderen", "Clutter"}))
			{
				seePlayer = false;
				sawOrHeardPlayer = true;
				return;
			}
		}
		seePlayer = false;
		return;
	}
	
	// Lets you define a point and an object, and returns whether the defined object is visible from the point.
	private bool IsTransformVisibleFrom(Vector3 start, Transform goal)
	{
		Vector3 direction = goal.position - start;
		RaycastHit[] hits = Physics.RaycastAll(start, direction, direction.magnitude);
		for(int i = 0; i < hits.Length; i++)
		{
			if(hits[i].transform != goal)
			{
				//Debug.DrawLine (start, goal.position, Color.red);
				return false;
			}
		}
		//Debug.DrawLine (start, goal.position, Color.green);
		return true;
	}
	
	// Same as above, but lets you ignore specified objects as well.
	private bool IsTransformVisibleFrom(Vector3 start, Transform goal, string[] ignoreList)
	{
		Vector3 direction = goal.position - start;
		RaycastHit[] hits = Physics.RaycastAll(start, direction, direction.magnitude);
		for(int i = 0; i < hits.Length; i++)
		{
			if(hits[i].transform != goal)
			{
				//Debug.DrawLine (start, goal.position, Color.red);
				bool ignoreCollision = false;
				for(int j = 0; j < ignoreList.Length; j++)
				{
					if(hits[i].transform.tag == ignoreList[j])
						ignoreCollision = true;
				}
				if(!ignoreCollision)
					return false;
			}
		}
		//Debug.DrawLine (start, goal.position, Color.green);
		return true;
	}
	
	// This method does the same as the two above, but takes two vectors instead of one vector and one transform.
	private bool LineOfSight(Vector3 start, Vector3 goal, string[] ignoreList)
	{
		Vector3 direction = goal - start;
		RaycastHit[] hits = Physics.RaycastAll(start, direction, direction.magnitude);
		for(int i = 0; i < hits.Length; i++)
		{
			//Debug.DrawLine (start, goal.position, Color.red);
			bool ignoreCollision = false;
			for(int j = 0; j < ignoreList.Length; j++)
			{
				if(hits[i].transform.tag == ignoreList[j])
					ignoreCollision = true;
			}
			if(!ignoreCollision)
				return false;
		}
		//Debug.DrawLine (start, goal.position, Color.green);
		return true;
	}
	
	private void AnimateMurderer()
	{
		if(rigidbody.velocity.magnitude > 0.1F)
		{
			if(animation != null)
				animation.CrossFade("walk");
		}else{
			if(animation != null)
				animation.CrossFade("idle");
		}
	}
	
	private void PlaySounds()
	{
		if(rigidbody.velocity.magnitude > 0.1F)
		{
			if(walkingSound != null)
				walkingSound.mute = false;
		}else{
			if(walkingSound != null)
				walkingSound.mute = true;
		}
	}
	
	// Returns true if murderer is currently being lit by flashlight. False if not.
	private void CheckBeingLit()
	{
		if(!flashlight.light.enabled)
		{
			beingLit = false;
			return;
		}
		//List of objects for the flashlight to ignore.
		string[] ignoreList = new string[]{"Player", "Morderen", "Clutter"};
		// Since the murderer is so not spherical, I take two points on him and test for line of sight.
		if(LineOfSight(flashlight.position, transform.position + (Vector3.up * 0.5F), ignoreList)
			|| LineOfSight(flashlight.position, transform.position + (Vector3.down * 0.5F), ignoreList))
		{
			// And with those same two points I test their closeness to where the flashlight is looking.
			if(Vector3.Dot(((transform.position + (Vector3.up * 0.5F)) - flashlight.position).normalized, flashlight.forward) > 0.97F
				|| Vector3.Dot(((transform.position + (Vector3.down * 0.5F)) - flashlight.position).normalized, flashlight.forward) > 0.97F)
			{
				// If line of sight is clear, AND flashlight is pointing pretty close to morderen. Return true.
				beingLit = true;
				return;
			}
		}
		// If not... Aint being lit.
		beingLit = false;
		return;
	}
	
	// This creates the pathfinding network. Only needs to be run once to establish network.
	private void InitializeNodes()
	{
		// Clear old list
		nodeList.Clear();
		
		// Find all nodes
		GameObject[] objects = GameObject.FindGameObjectsWithTag("PathfindingNode");
		for(int i = 0; i < objects.Length; i++)
		{
			nodeList.Add(new Node_Wrapper(objects[i]));
		}
		
		// Find all paths
		foreach(Node_Wrapper node in nodeList)
		{
			node.LocatePaths(nodeList);
		}
	}
	
	// Re-finds connections between existing nodes.
	private void UpdateNodes()
	{
		foreach(Node_Wrapper node in nodeList)
		{
			node.LocatePaths(nodeList);
		}
	}
	
	// Murderer finds the most effective path from himself to the defined object.
	// The function overwrites murderer's current path with the new one it finds.
	private void FindPathTo(Transform destination)
	{
		if(nodeList.Count < 1)
		{
			pathIsImpossible = true;
			return;
		}
		
		// Find the nodes closest to start and goal
		Node_Wrapper startNode = null;
		Node_Wrapper goalNode = null;
		foreach(Node_Wrapper node in nodeList)
		{
			// Set startnode if this node is closer than the closest so far.
			if(IsTransformVisibleFrom(node.position, transform) || (transform.position - node.position).sqrMagnitude < 0.5F)
			{
				if(startNode != null)
				{
					if((transform.position - node.position).sqrMagnitude < (transform.position - startNode.position).sqrMagnitude)
					{
						startNode = node;
					}
				}else{
					startNode = node;
				}
			}
			// Set goalnode if this node is closer than the closest so far.
			if(IsTransformVisibleFrom(node.position, destination, new string[]{"Door"}) || (destination.position - node.position).sqrMagnitude < 0.5F)
			{
				if(goalNode != null)
				{
					if((destination.position - node.position).sqrMagnitude < (destination.position - goalNode.position).sqrMagnitude)
					{
						goalNode = node;
					}
				}else{
					goalNode = node;
				}
			}
		}
		if(startNode == null || goalNode == null)
		{
			currentPath.Clear();
			pathIsImpossible = true;
			return;
		}
		
		// A* algorithm
		int failSafe = 0;
		List<Node_Wrapper> openList = new List<Node_Wrapper>();
		List<Node_Wrapper> closedList = new List<Node_Wrapper>();
		openList.Add(startNode);
		while(openList.Count > 0 && failSafe < 1000F)
		{
			failSafe++;
			
			// This is to find element with lowest score.
			Node_Wrapper bestNode = null;
			float bestPathScore = float.MaxValue;
			foreach(Node_Wrapper node in openList)
			{
				float nodeScore = node.distanceTraveled + ((node.position - goalNode.position).sqrMagnitude / 2);
				if(nodeScore < bestPathScore)
				{
					bestNode = node;
					bestPathScore = nodeScore;
				}
			}
			
			// Find all neighbours of that node, that haven't been tried out yet.
			foreach(Node_Wrapper node in bestNode.neighbours)
			{
				if(!openList.Contains(node) && !closedList.Contains(node))
				{
					node.parentNode = bestNode;
					node.distanceTraveled = bestNode.distanceTraveled + (bestNode.position - node.position).sqrMagnitude;
					openList.Add(node);
				}
				else if(node.distanceTraveled > bestNode.distanceTraveled + (bestNode.position - node.position).sqrMagnitude)
				{
					node.parentNode = bestNode;
					node.distanceTraveled = bestNode.distanceTraveled + (bestNode.position - node.position).sqrMagnitude;
				}
			}
			
			if(bestNode.position != goalNode.position)
			{
				closedList.Add(bestNode);
				openList.Remove(bestNode);
			}
			else
			{
				currentPath = bestNode.PathHere();// JIPPI!
				pathStage = 0;
				arrivedAtPathEnd = false;
				pathIsImpossible = false;
				break;
			}
		}
		
		// Is the path impossible??
		if(openList.Count <= 0 || failSafe >= 1000F)
		{
			pathIsImpossible = true;
		}
		// Cleans up!
		foreach(Node_Wrapper node in nodeList)
		{
			node.parentNode = null;
			node.distanceTraveled = 0F;
		}
	}
	
	// This takes your current path, if you have one, and extends it one stage towards a destination.
	// NEEDS WORK !!!
	private void ExtendCurrentPathTowards(Vector3 destination)
	{
		// If you actually HAVE a current path
		if(currentPath.Count > 1)
		{
			// NEEDS CHANGE. This function just walks to player with small chance of error. Not good.
			// He should actually try to figure out where to go.
			Node_Wrapper bestExtension = currentPath[currentPath.Count - 1];
			foreach(Node_Wrapper node in currentPath[currentPath.Count - 1].neighbours)
			{
				if(Vector3.Distance(node.position, destination) < Vector3.Distance(bestExtension.position, destination)
					&& Random.Range(1, 20) < 11 + (2))// This adds random chance that he'll walk wrong.
				{
					bestExtension = node;
				}
			}
			currentPath.Add(bestExtension);
		}
	}
	
	// Makes murderer walk towards the point "whereToGo". Call this ONCE in Update() after you've figured out where to go.
	private void WalkForward()
	{
		// Now with crappy obstacle avoidance
		Vector3 walkDirection = (movementDestination - transform.position).normalized;
		Debug.DrawLine (transform.position, movementDestination, Color.yellow);
		rigidbody.velocity = Vector3.zero;
		
		// Seek towards movementDestination, with obstacle avoidance.
		if(waitToMove == 0)
		{
			Vector3 left = new Vector3(0 - walkDirection.z, walkDirection.y, walkDirection.x) * 0.1F;
			RaycastHit rayHit = new RaycastHit();
			if(Vector3.Dot(walkDirection, transform.forward) > 0.7F)
			{
				for(int i = 1; i <= 6; i++)
				{
					for(int j = 1; j <= 6; j++)
					{
						if(Physics.Linecast(transform.position + left * i, (transform.position + left * i) + (transform.forward * (j*0.2F)), out rayHit))
						{
							if(rayHit.transform != transform && rayHit.transform != player.transform)
							{
								Debug.DrawLine(transform.position + left * i, (transform.position + left * i) + (transform.forward * (j*0.2F)));
								walkDirection -= left * (2.4F - j*0.4F);//could do movespeed +1
							}
						}
						if(Physics.Linecast(transform.position - left * i, (transform.position - left * i) + (transform.forward * (j*0.2F)), out rayHit))
						{
							if(rayHit.transform != transform && rayHit.transform != player.transform)
							{
								Debug.DrawLine(transform.position - left * i, (transform.position - left * i) + (transform.forward * (j*0.2F)));
								walkDirection += left * (2.4F - j*0.4F); // These 2.4's used to be 3. Making them 2 might mess up morderen.
								// Seems like it didn't. But the number signifies how much the murderer goes out of his way to avoid crap.
							}
						}
					}
				}
				rigidbody.velocity = walkDirection*moveSpeed;
				rigidbody.AddForce(Vector3.up);
			}
			// Face in direction of whereToGo.
			// Correction for avoidance is that morderen doesn't LERP as much when avoiding stuff.
			float correctionForAvoidance = Vector3.Dot((movementDestination - transform.position).normalized, walkDirection.normalized);
			correctionForAvoidance = correctionForAvoidance * correctionForAvoidance; // Squared for greater effect!
			Vector3 facingDirection = new Vector3(walkDirection.x, 0, walkDirection.z);
			transform.forward = Vector3.Lerp(transform.forward, facingDirection, 0.2F * correctionForAvoidance);
		}
	}
	
	private void TeleportToSafety()
	{
		if(nodeList.Count < 1)
			return;
		
		float RandomFleeDistance = maximumDistanceFromPlayer;
		Node_Wrapper bestEscapeNode = nodeList[0];
		foreach(Node_Wrapper node in nodeList)
		{
			if(!IsTransformVisibleFrom(node.position, player))
			{
				if(Mathf.Abs(Vector3.Distance(node.position, player.position) - RandomFleeDistance) 
					< Mathf.Abs(Vector3.Distance(bestEscapeNode.position, player.position) - RandomFleeDistance))
				{
					bestEscapeNode = node;
				}
			}
		}
		transform.position = bestEscapeNode.position;
		return;
	}
}

public class Node_Wrapper
{
	public GameObject gameobject;
	public Vector3 position;
	public Node_Wrapper thisNode;
	public Node_Wrapper parentNode = null;
	public float distanceTraveled = 0F;
	public List<Node_Wrapper> neighbours = new List<Node_Wrapper>();
	private const float MAX_NEIGHBOUR_DISTANCE = 40F; // This is in meters.
	
	public Node_Wrapper(GameObject node)
	{
		gameobject = node;
		thisNode = this;
		position = node.transform.position;
	}
	
	public void LocatePaths(List<Node_Wrapper> nodeList)
	{
		neighbours.Clear();
		// Add tags here!
		string[] ignoreList = new string[]{"Player", "Morderen", "Door"};
		// Objects with tags added to this list will be ignored when the nodes build the path network.
		foreach(Node_Wrapper node in nodeList)
		{
			if(node == this || (node.position - this.position).sqrMagnitude > MAX_NEIGHBOUR_DISTANCE * MAX_NEIGHBOUR_DISTANCE)
				continue;
			RaycastHit[] hits = Physics.RaycastAll(position, node.position - position, (node.position - position).magnitude);
			bool collided = false;
			for(int i = 0; i < hits.Length ; i++)
			{
				
				bool ignoreCollision = false;
				for(int j = 0; j < ignoreList.Length; j++)
				{
					if(hits[i].transform.tag == ignoreList[j])
						ignoreCollision = true;
				}
				if(!ignoreCollision)
					collided = true;	
			}
			if(!collided)
				neighbours.Add(node);
		}
	}
	
	public List<Node_Wrapper> PathHere()
	{
		List<Node_Wrapper> output;
		if(parentNode != null)
		{
			output = parentNode.PathHere();
		}else{
			output = new List<Node_Wrapper>();
		}
		output.Add(this);
		return output;
	}
}
