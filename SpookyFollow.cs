using UnityEngine;
using System.Collections;

public class SpookyFollow : MonoBehaviour {
	private Transform player;
	private Vector3 viewPlace;
	private float distance;
	public float followSpeed = 0.01F;
	public float levitationHeight = 1.0F;
	public float personalSpace = 1.7F;//Hvor langt unna playeren han stopper. 1,7 meter.
	// Use this for initialization
	void Start () {
		player = GameObject.FindWithTag("Player").transform;
	}
	
	// Update is called once per frame
	void Update () {
		distance = Vector3.Distance(transform.position, player.position);
		viewPlace = player.position+player.forward;
		if(distance > personalSpace)
		{
			if(Vector3.Distance(transform.position,viewPlace) > Vector3.Distance(transform.position,player.position))
			{
				transform.position = Vector3.Lerp(transform.position, player.position, followSpeed);
				Vector3 temp = transform.position;
				temp.y = Terrain.activeTerrain.SampleHeight(transform.position)+levitationHeight;
				transform.position = temp;
				transform.LookAt(new Vector3(player.position.x,1000,player.position.z));
			}
		}
	
	}
}
