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
 * 
 * *** FEATURES ***
 * Dette pathfinding scriptet bruker nodes og line of sight til å finne objektet med taggen "Player"
 * Det kan bli puttet på et hvilket som helst objekt. Men hvis det det puttes på en instans av Construction Worker
 * MED componenten Animation(ikke Animator) spiller den også animasjonene korrekt.
 * 
 * For å lage pathfinding nodes; bare lag et objekt av hvilken som helst type, og gi det taggen "PathfindingNode"
 * Objektet kan godt ha en mesh så det er lettere for level designere å se den! Fordi...
 * Hele dritten blir deaktivert uansett etter den er registrert i node-listen til morderen.
 * 
 */

public class Morder_Pathfinding : MonoBehaviour {
	private Transform player;
	private Transform flashlight;
	private List<Node_Wrapper> nodeList = new List<Node_Wrapper>();
	private List<Node_Wrapper> currentPath = new List<Node_Wrapper>();
	int pathStage = 0;
	/* Hvis du skrur AV DirectMovement blir movement apply som Force istedenfor directe velocity.
	 * Det er BUGGY!
	 */
	public bool directMovement = true;
	public bool seePlayer = false;
	private bool sawPlayer = false;
	public bool beingLit = false;//Sier om morderen blir belyst av flashlight.
	public float distance;
	public float moveSpeed = 1.5F;
	public float personalSpace = 1.5f;
	public float extraGravity = -1F;
	Vector3 wherePlayerAt;//Disse vektorene beholder y-verdien til morderen, for å unngå at han flyr osv.
	Vector3 pointToPlayer;//Vektor som peker fra morderen til playeren.
	Vector3 whereToGo = Vector3.zero;

	void Start ()
	{
		player = GameObject.FindWithTag("Player").transform;
		flashlight = GameObject.FindWithTag("Light").transform;
		UpdateNodes();
	}
	
	void Update ()
	{
		wherePlayerAt = new Vector3(player.position.x, transform.position.y, player.position.z);
		//wherePlayerAt.y += 1;// WHY doesn't his work????
		distance = Vector3.Distance(transform.position, wherePlayerAt);
		pointToPlayer = wherePlayerAt - transform.position;
		pointToPlayer.Normalize();
		beingLit = CheckBeingLit();
		seePlayer = CheckSeePlayer();
		
		if(seePlayer)//seePlayer
		{
			//FindPathTo(player);// Might be too taxing, should maybe make special justLostSightOfPlayer case.
			currentPath.Clear(); // Hmmmm!
			whereToGo = wherePlayerAt;
			pathStage = 0;
		}else{
			if(currentPath.Count >= 1)
			{
				// If pathstep is an illegal number
				if(pathStage >= currentPath.Count || pathStage < 0 || currentPath.Count == 0)
				{
					FindPathTo(player);
					pathStage = 0;
				}else{
					if(pathStage < currentPath.Count - 1)
					{
						if(IsTransformVisibleFrom(currentPath[pathStage + 1].position, transform))
						{
							++pathStage;
						}
					}
					whereToGo = currentPath[pathStage].position;
					if((currentPath[pathStage].position - transform.position).magnitude < personalSpace / 5F)
						pathStage++;
				}
			}else{
				// Hvis du ikke kan se playeren, og ikke har noen path, finn en path.
				FindPathTo(player);
			}
		}
		
		// *** DEBUG STUFF ***
		//Debug.DrawLine(wherePlayerAt, transform.position, Color.white);
		for(int i = 0; i < currentPath.Count; i++)
		{
			//Debug.DrawLine(currentPath[i].position, currentPath[i].position + Vector3.up * 20, Color.green);
			if(!(i >= currentPath.Count - 1))
				Debug.DrawLine(currentPath[i].position, currentPath[i+1].position, Color.red);
		}
		// *** END OF DEBUG STUFF ***
		
		// Perform walk to get to whereToGo.
		if(distance<=personalSpace && seePlayer)
		{
			rigidbody.velocity = Vector3.zero;
			if(animation != null)
				animation.CrossFade("idle");
		}else{
			WalkForward();
		}
		
		rigidbody.AddForce(0, extraGravity, 0);
		sawPlayer = seePlayer;
	}
	
	bool CheckSeePlayer()
	{
		RaycastHit rayHit = new RaycastHit();
		if(Physics.Linecast(transform.position + Vector3.up, player.position, out rayHit))// Ikke bruk wherePlayerAt her!
		{
			//Debug.DrawLine (transform.position, wherePlayerAt, Color.green);
			if(rayHit.transform == player) return true;
		}
		if(sawPlayer && distance < 4F)
		{
			return true;
		}
		return false;
	}
	
	bool IsTransformVisibleFrom(Vector3 start, Transform goal)
	{
		RaycastHit rayHit = new RaycastHit();
		if(Physics.Linecast(start, goal.position, out rayHit))
		{
			if(rayHit.transform != goal) return false;
			else return true;
		}
		return false;
	}
	
	bool CheckBeingLit()
	{
		if(flashlight.light.intensity < 0.1) return false;
		Ray rayLook;
		RaycastHit rayHit = new RaycastHit();
		for(float i = 0; i < 0.07F; i += 0.01F)//Alle disse tallene er for å dekke riktig område på skjermen.
		{
			for(float j = 0; j < 0.17F; j += 0.02F)//Ditto
			{
				rayLook = Camera.main.ViewportPointToRay(new Vector3(0.465F+i,0.42F+j,0F));//Her og.
				if(Physics.Raycast(rayLook, out rayHit, 20))//20 er hvor mange meter unna flashlighten stopper morderen.
				{
					//Debug.DrawLine (rayLook.origin, rayHit.point, Color.red);
					if(rayHit.transform == transform) return true;
				}
			}
		}
		return false;
	}
	
	void UpdateNodes()
	{
		// Clear old list
		nodeList.Clear();
		
		// Find all nodes
		GameObject[] objects = GameObject.FindGameObjectsWithTag("PathfindingNode");
		for(int i = 0; i < objects.Length; i++)
		{
			nodeList.Add(new Node_Wrapper(objects[i]));
			objects[i].SetActive(false);
		}
		
		// Find all paths
		foreach(Node_Wrapper node in nodeList)
		{
			node.LocatePaths(nodeList);
		}
	}
	
	void FindPathTo(Transform destination)
	{
		// Fails if called before UpdateNodes.
		if(nodeList.Count < 1)
		{
			return;
		}
		
		// Find the nodes closest to start and goal
		Node_Wrapper startNode = null;
		Node_Wrapper goalNode = null;
		foreach(Node_Wrapper node in nodeList)
		{
			// Set startnode if this node is closer than the closest so far.
			if(IsTransformVisibleFrom(node.position, transform) || (transform.position - node.position).magnitude < 0.5F)
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
			if(IsTransformVisibleFrom(node.position, destination) || (destination.position - node.position).magnitude < 0.5F)
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
		
		// If you're already as close as you can get to the goal, then don't go anywhere else. Or if you can't find ANY suitable nodes.
		if(startNode == goalNode || startNode == null || goalNode == null)
		{
			currentPath.Clear();
			return;
		}
		
		// A* algorithm
		List<Node_Wrapper> openList = new List<Node_Wrapper>();
		List<Node_Wrapper> closedList = new List<Node_Wrapper>();
		openList.Add(startNode);
		int failSafe = 0;
		while(openList.Count > 0 && failSafe < 1000F)
		{
			// This is a failsafe against getting stuck in an infinite loop...
			++failSafe;
			// This is to find element with lowest score, and can DEFINITELY be improved. May eat CPU.
			Node_Wrapper bestNode = null;
			float bestPathScore = 1000000F;
			foreach(Node_Wrapper node in openList)
			{
				if(node.pathScore <= bestPathScore)
				{
					bestNode = node;
					bestPathScore = node.pathScore;
				}
			}
			// Find all neighbours of that node, that haven't been tried out yet.
			foreach(Node_Wrapper node in bestNode.neighbours)
			{
				if(!closedList.Exists(o => o.position == node.position) 
					&& !openList.Exists(o => o.position == node.position))// Not sure about this one!
				{
					// The most important part here is that the parent variable gets set,
					// Because we use that to find out what path was used when we finally reach goalNode.
					node.parentNode = bestNode;
					node.pathScore = (node.position - destination.position).sqrMagnitude
						+ (node.position - transform.position).sqrMagnitude;
					openList.Add(node);
				}
			}
			// If we found the goalNode, then quit loop and get the path we used!
			if(bestNode.position == goalNode.position)
			{
				currentPath = bestNode.PathHere();// JIPPI!
				break;
			}
			// If not, put current node in closedList and continue looping.
			closedList.Add(bestNode);
			openList.Remove(bestNode);
		}
		
		// Cleans up!
		foreach(Node_Wrapper node in nodeList)
		{
			node.parentNode = null;
			node.pathScore = 0;
		}
	}
	
	void WalkForward()
	{
		// Now with crappy obstacle avoidance
		Debug.DrawLine (transform.position, whereToGo, Color.yellow);
		if(directMovement) rigidbody.velocity = Vector3.zero;
		if(!beingLit)// <--- this should be put in a state machine!
		{
			Vector3 walkDirection = (whereToGo - transform.position).normalized;
			Vector3 left = new Vector3(0 - walkDirection.z, walkDirection.y, walkDirection.x) * 0.1F;
			RaycastHit rayHit = new RaycastHit();
			for(int i = 1; i <= 6; i++)
			{
				for(int j = 1; j <= 6; j++)
				{
					if(Physics.Linecast(transform.position + left * i, (transform.position + left * i) + (transform.forward * (j*0.2F)), out rayHit))
					{
						if(rayHit.transform != transform && rayHit.transform != player.transform)
						{
							Debug.DrawLine(transform.position + left * i, (transform.position + left * i) + (transform.forward * (j*0.2F)));
							walkDirection -= left * (3 - j*0.4F);//could do movespeed +1
						}
					}
					if(Physics.Linecast(transform.position - left * i, (transform.position - left * i) + (transform.forward * (j*0.2F)), out rayHit))
					{
						if(rayHit.transform != transform && rayHit.transform != player.transform)
						{
							Debug.DrawLine(transform.position - left * i, (transform.position - left * i) + (transform.forward * (j*0.2F)));
							walkDirection += left * (3 - j*0.4F);
						}
					}
				}
			}
			if(directMovement) rigidbody.velocity = walkDirection*moveSpeed;
			else rigidbody.AddForce((walkDirection*moveSpeed)/20);
			rigidbody.AddForce(Vector3.up);
			// Face in direction of whereToGo.
			Vector3 facingDirection = new Vector3(walkDirection.x, 0, walkDirection.z);
			transform.forward = Vector3.Lerp(transform.forward, facingDirection, moveSpeed*0.05F);
			if(animation != null)
				animation.CrossFade("walk");
		}else{
			if(animation != null)
				animation.CrossFade("idle");
		}
	}
	
	void OnCollisionStay(Collision collision)
	{
		return;
	}
}

public class Node_Wrapper
{
	// Public
	public Vector3 position;
	public Node_Wrapper thisNode;
	public Node_Wrapper parentNode = null;
	public float pathScore = 0F;
	public List<Node_Wrapper> neighbours = new List<Node_Wrapper>();
	
	public Node_Wrapper(GameObject node)
	{
		thisNode = this;
		position = node.transform.position;
	}
	
	public void LocatePaths(List<Node_Wrapper> nodeList)
	{
		neighbours.Clear();
		foreach(Node_Wrapper node in nodeList)
		{
			if(node == this || node.position == position)
				continue;
			Ray rayLook = new Ray(position, node.position - position);
			if(!Physics.Raycast(rayLook, (node.position - position).magnitude))
			{
				neighbours.Add(node);
			}
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
