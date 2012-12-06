using UnityEngine;
using System.Collections;

public class MurdererFollow : MonoBehaviour{
	/*Rigidbodyen til morderen bør ha mass 0,01. Da flyr han ikke opp fra bakken, og klarer ikke flytte ting med mass 1.
	 *Mass 1 er standard, så da kan han by default ikke flytte på ting.
	 *Hvis noe har lik masse som morderen så greier han å dytte/velte det.
	 *Hvis indirect movement skal benyttes burde rigidbodyen også ha 5 i drag.
	 *Morderen må ha låst alle rotasjoen i rigidbodyen sin!!!*/
	private Transform player;
	private Transform flashlight;
	public bool directMovement = true;
	/*To forskjellige movement modes. Direct betyr at morderen blir flyttet ved å direkte sette farten hans.
	 *Når movement ikke er direct påføres det kraft for å få morderen til å bevege seg. Tror direct generelt er bedre.*/
	public bool beingLit = false;//Sier om morderen blir belyst av flashlight.
	public bool seePlayer = false;
	public bool followPlayer = false;
	public bool walkingAlongWall = false;
	public int nextPositionUpdate = 100;
	public float distance;
	public float moveSpeed = 1;
	public float personalSpace = 1.5f;
	public float extraGravity = -1F;
	Vector3 lastPosition;
	Vector3 wherePlayerAt;//Disse vektorene beholder y-verdien til morderen, for å unngå at han flyr osv.
	Vector3 pointToPlayer;//Vektor som peker fra morderen til playeren.
	Vector3 whereToGo = Vector3.zero;
	Vector3 parallelToWall;
	Vector3 whereWallWas;

	void Start ()
	{
		player = GameObject.FindWithTag("Player").transform;
		flashlight = GameObject.FindWithTag("Light").transform;
	}
	
	void FixedUpdate ()
	{//When more than 25 away and stuck  in corner, does not follow wall. When two corners after each other he just goes back n forth.
		wherePlayerAt = new Vector3(player.position.x, transform.position.y, player.position.z);
		distance = Vector3.Distance(transform.position, wherePlayerAt);
		pointToPlayer = wherePlayerAt - transform.position;
		pointToPlayer.Normalize();
		seePlayer = CheckSeePlayer();
		beingLit = CheckBeingLit();
		
		if(distance > 25 && !walkingAlongWall) whereToGo = Vector3.Lerp(whereToGo, pointToPlayer, 0.1F);//If far away and totally lost
		if(StuckInCorner() && !seePlayer)//If stuck in a corner, and can't see player
		{
			whereToGo = pointToPlayer;
			whereWallWas = Vector3.zero - whereWallWas;
			walkingAlongWall = false;
		}
		if(!walkingAlongWall && whereWallWas != Vector3.zero)//If the wall you were walking along ended
		{
			whereToGo = whereWallWas;
			whereWallWas = Vector3.zero;
			if(distance > 25) whereToGo = pointToPlayer;//Bryter opp veggfølging hvis player er langt unna
		}
		if(seePlayer)
		{
			whereToGo = pointToPlayer;
			walkingAlongWall = false;
		}
		if(walkingAlongWall) whereToGo = parallelToWall;
		//if(rigidbody.velocity == Vector3.zero && !seePlayer) whereToGo = new Vector3(Random.value, transform.position.y, Random.value);
		WalkForward();
		if(distance<=personalSpace && seePlayer)//If you've reached the player
		{
			rigidbody.velocity = Vector3.zero;
		}
		rigidbody.AddForce(0,extraGravity,0);//Gravity var ikke nok for å holde ham nede. Dette holder, hvis massen hans er 0.01.
		transform.forward = Vector3.Lerp(transform.forward, whereToGo, moveSpeed*0.05F);
		if(nextPositionUpdate <= 0)
		{
			lastPosition = transform.position;
			nextPositionUpdate = 100;
		}else{
			nextPositionUpdate -= 1;
		}
	}
	
	bool CheckSeePlayer()
	{
		Ray rayLook = new Ray(transform.position, player.position - transform.position);
		RaycastHit rayHit = new RaycastHit();
		if(Physics.Raycast(rayLook, out rayHit, 20))
		{
			//Debug.DrawLine (rayLook.origin, rayHit.point, Color.red);
			if(rayHit.transform == player) return true;
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
	
	void WalkForward()
	{
		Debug.DrawLine (transform.position, transform.position+whereToGo, Color.red);
		if(directMovement) rigidbody.velocity = Vector3.zero;
		if(!beingLit)
		{
			whereToGo.Normalize();
			if(directMovement) rigidbody.velocity = whereToGo*moveSpeed;
			else rigidbody.AddForce((whereToGo*moveSpeed)/20);
			rigidbody.AddForce(Vector3.up);
		}
	}
	
	bool StuckInCorner()
	{
		//return false;//Sjekker om jeg trenger denne...
		if(nextPositionUpdate <= 1 && !seePlayer)
		{
			if(transform.position.x < lastPosition.x+1 && transform.position.x > lastPosition.x-1)
			{
				if(transform.position.z < lastPosition.z+1 && transform.position.z > lastPosition.z-1) return true;
			}
		}
		return false;
	}
	
	void OnCollisionStay(Collision collision)
	{
		if(StuckInCorner()) return;
		ContactPoint[] walls = new ContactPoint[3];
		int i = 0;
		int wallsHit = 0;
		while(true)
		{
			walls[wallsHit] = collision.contacts[i];
			if(Vector3.Angle(Vector3.up, walls[wallsHit].normal) > 88 && Vector3.Angle(Vector3.up, walls[wallsHit].normal) < 92)
			{
				if(wallsHit == 0) wallsHit++;
				else if(walls[wallsHit].normal != walls[(wallsHit-1)].normal)wallsHit++;
				//wallsHit++;
			}
			if(wallsHit >= walls.Length) break;
			i++;
			if(i >= collision.contacts.Length) break;
		}
		if(wallsHit < 3)//Hvis han traff 2 vegger.
		{
			if(wallsHit < 2)//Hvis han traff 1 vegg.
			{
				if(wallsHit < 1)//Hvis han traff 0 vegger.
				{
					return;
				}
				Vector3 normal = walls[0].normal;
				Vector3 rotatedCW = new Vector3(0-normal.z, normal.y, normal.x);//x given to Z. Z negative given to x
				Vector3 rotatedCCW = new Vector3(normal.z, normal.y, 0-normal.x);//z given to x, x given to z negative
				if(Vector3.Angle(rotatedCW, transform.forward)<Vector3.Angle(rotatedCCW, transform.forward)) parallelToWall = rotatedCW;
				else parallelToWall = rotatedCCW;
				walkingAlongWall = true;
				whereWallWas = Vector3.zero - normal;
				return;
			}
			Vector3 normal0 = walls[0].normal;
			Vector3 normal1 = walls[1].normal;
			if(Vector3.Angle(normal0, transform.forward)<Vector3.Angle(normal1, transform.forward))
			{
				parallelToWall = normal0;
				whereWallWas = Vector3.zero - normal1;
			}else{
				parallelToWall = normal1;
				whereWallWas = Vector3.zero - normal0;
			}
			walkingAlongWall = true;
			return;
		}
		parallelToWall = new Vector3(Random.value, 0, Random.value);
		whereWallWas = Vector3.zero;
		return;
	}
	
	void OnCollisionExit(Collision collision)
	{
		walkingAlongWall = false;
	}
}
