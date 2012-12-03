using UnityEngine;
using System.Collections;

public class SpookyFollow : MonoBehaviour {
	private Transform player;
	private Transform light;
	private Vector3 viewPlace;
	private Vector3 nextPos;
	private float distance;
	public float moveSpeed = 0.015f;
	public float personalSpace = 1.7f;
	public float levitationHeight = 1.0f;

	void Start () {
		player = GameObject.FindWithTag("Player").transform;
		light = GameObject.FindWithTag ("Light").transform;
	}
	
	void Update () {
		distance = Vector3.Distance(transform.position, player.position);
		viewPlace = player.position+player.forward;
		if(distance>personalSpace)
		{
			if(!renderer.isVisible || light.light.intensity == 0)
			{
				transform.position = Vector3.Lerp(transform.position, player.position, moveSpeed);
				nextPos = new Vector3(transform.position.x, Terrain.activeTerrain.SampleHeight(transform.position)+personalSpace, transform.position.z);
				transform.position = Vector3.Lerp(transform.position, nextPos, levitationHeight);
				transform.LookAt(new Vector3(player.position.x,1000,player.position.z));
			}
		}
	}
}
