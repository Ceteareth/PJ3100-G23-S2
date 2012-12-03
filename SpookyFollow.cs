using UnityEngine;
using System.Collections;

public class SpookyFollow : MonoBehaviour {
	private Transform player;
	private Transform light;
	private Vector3 viewPlace;
	private Vector3 nextPos;
	private float distance;
	public float moveSpeed = 0.015f;

	void Start () {
		player = GameObject.FindWithTag("Player").transform;
		light = GameObject.FindWithTag ("Light").transform;
	}
	
	void Update () {
		distance = Vector3.Distance(transform.position, player.position);
		viewPlace = player.position+player.forward;
		if(distance>1.7)
		{
			if(!renderer.isVisible || light.light.intensity == 0)
			{
				transform.position = Vector3.Lerp(transform.position, player.position, moveSpeed);
				nextPos = new Vector3(transform.position.x, Terrain.activeTerrain.SampleHeight(transform.position)+1.7F, transform.position.z);
				transform.position = Vector3.Lerp(transform.position, nextPos, 1F);
				transform.LookAt(new Vector3(player.position.x,1000,player.position.z));
			}
		}
	}
}
