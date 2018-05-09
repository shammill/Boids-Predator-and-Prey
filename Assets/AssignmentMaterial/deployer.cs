using UnityEngine;
using System.Collections;

public class deployer : MonoBehaviour {

	// the Boid prefab transform
	public Transform fish;
	public Transform shark;

	// initialise the boids
	void Start () {
		for (int i = 1; i < 15; i++) {
			// Instantiate (clone) the fish object.
			Instantiate(fish, new Vector3(Random.Range(-7.0F, 7.0F), Random.Range(-7.0F, 7.0F), Random.Range(0, 10.0F)), Quaternion.identity);
		}
		for (int i = 1; i < 5; i++) {
			// Instantiate (clone) the shark object.
			Instantiate(shark, new Vector3(Random.Range(-18.0F, -18.0F), Random.Range(-10.0F, 10.0F), Random.Range(-14.0F, 0F)), Quaternion.identity);
		}
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
