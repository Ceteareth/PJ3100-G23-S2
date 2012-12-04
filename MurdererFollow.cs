using UnityEngine;
using System.Collections;

public class MurdererFollow : MonoBehaviour{
	/*Rigidbodyen til morderen bør ha mass 0,01. Da flyr han ikke opp fra bakken, og klarer ikke flytte ting med mass 1.
	 *Mass 1 er standard, så da kan han by default ikke flytte på ting.
	 *Hvis noe har lik masse som morderen så greier han å dytte/velte det.
	 *Hvis indirect movement skal benyttes burde rigidbodyen også ha 5 i drag.*/
	private Transform player;
	private Transform flashlight;
	public bool directMovement = true;
	/*To forskjellige movement modes. Direct betyr at morderen blir flyttet ved å direkte sette farten hans.
	 *Når movement ikke er direct påføres det kraft for å få morderen til å bevege seg. Tror direct generelt er bedre.*/
	public bool beingLit = false;//Sier om morderen blir belyst av flashlight.
	public float moveSpeed = 2;
	public float personalSpace = 1.5f;
	Vector3 pointToPlayer;//Vektor som peker fra morderen til playeren.

	void Start ()
	{
		player = GameObject.FindWithTag("Player").transform;
		flashlight = GameObject.FindWithTag("Light").transform;
	}
	
	void FixedUpdate ()
	{
		beingLit = false;
		if(flashlight.light.intensity > 0 && checkForFlashlight()) beingLit = true;
		float distance = Vector3.Distance(transform.position, player.position);
		if(directMovement) rigidbody.velocity = new Vector3(0,0,0);//Ødelegger indirect movement, så kjøre bare med direct movement.
		if(distance>personalSpace && !beingLit)
		{
			pointToPlayer = player.position - transform.position;
			pointToPlayer.Normalize();
			if(directMovement) rigidbody.velocity = pointToPlayer*moveSpeed;
			else rigidbody.AddForce((pointToPlayer*moveSpeed)/20);
		}
		if(distance<personalSpace)
		{
			rigidbody.velocity = new Vector3(0,0,0);//Hvis directMovement er av hindrer dette morderen fra å være helt gal.
		}
		rigidbody.AddForce(0,-2F,0);//Gravity var ikke nok for å holde ham nede. Dette holder, hvis massen hans er 0.01.
		transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));//Y holder seg oppover.
	}
	
	bool checkForFlashlight()
	{
		Ray rayLook;
		RaycastHit rayHit = new RaycastHit();
		for(float i = 0; i < 0.07F; i += 0.01F)//Alle disse tallene er for å dekke riktig område på skjermen.
		{
			for(float j = 0; j < 0.17F; j += 0.02F)//Ditto
			{
				rayLook = Camera.main.ViewportPointToRay(new Vector3(0.465F+i,0.42F+j,0F));//Her og.
				if(Physics.Raycast(rayLook, out rayHit, 20))//20 er hvor mange meter unna flashlighten stopper morderen.
				{
					if(rayHit.transform == transform) return true;
					Debug.DrawLine (rayLook.origin, rayHit.point, Color.red);
				}
			}
		}
		return false;
	}
}
