using UnityEngine;
using System.Collections;

public class SpookyFollow : MonoBehaviour {
	private Transform player;
	private Vector3 viewPlace;
	private Vector3 nextPos;
	private float distance;
	// Use this for initialization
	void Start () {
		player = GameObject.FindWithTag("Player").transform;
	
	}
	
	// Update is called once per frame
	void Update () {
		distance = Vector3.Distance(transform.position, player.position);
		viewPlace = player.position+player.forward;
		if(distance>1.7)
		{
			if(Vector3.Distance(transform.position,viewPlace)>Vector3.Distance(transform.position,player.position))
			{
				transform.position = Vector3.Lerp(transform.position, player.position, 0.015F);
				nextPos = new Vector3(transform.position.x, Terrain.activeTerrain.SampleHeight(transform.position)+1.7F, transform.position.z);
				transform.position = Vector3.Lerp(transform.position, nextPos, 1F);
				transform.LookAt(new Vector3(player.position.x,1000,player.position.z));
			}
		}
	
	}
}
