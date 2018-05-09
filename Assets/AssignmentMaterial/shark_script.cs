using UnityEngine;
using System.Collections;

public class shark_script : MonoBehaviour {

	// the global centre of the simulation
	private Vector3 simulation_centre = new Vector3(0f, 0.0f, 0f);
	// the global radius of the simulation
	private float simulation_radius = 20f;
	// the distance where sepration is applied
	private float separation_distance = 8.0f;
	// the distance where cohesion is applied
	private float cohesion_distance = 12.0f;
	// the sepration strength
	private float separation_strength = 25.0f;
	// the strength that cohesion is applied
	private float cohesion_strength = 10.0f;
	
	// the list of Boid neighbours
	private GameObject [] fishes;
	private GameObject [] sharks;

	// the cohesive position
	private Vector3 cohesion_pos;
	// the fish and shark index
	private int fish_index;
	private int sharks_index;
	
	// Currently calculated closest fish.
	private GameObject closestFish;
	private float closestFishDistance;
	
	// The final fish considered closest.
	private GameObject finalClosestFish;
	
	// Shark behaviour vars.
	public float hunger;					// How hungry the shark is.
	private float digestionRate = 0.9f; 	// Less than 1
	private float digestionTime = 2.0f; 	// How often digestion occurs (Time in seconds)
	private float foodCapacity;				// Maximum food capacity.
	private float foodLevel;				// Current level of food the shark has
	public float speed;						// How fast the shark moves taking hunger into account.
	private float base_speed = 2.5f; 		// Base speed of the shark the shark moves.	
	private float hunger_threshold = 0.7f;	// How hungry the shark has to be before taking action.
	
	// Use this for initialization
	void Start () {
		// initialise to null (this script might start while other fish are still being created)
		fishes = null;
		sharks = null;
		
		// set fish index
		fish_index = 0;
		sharks_index = 0;
		
		// create the cohesion vector
		cohesion_pos = new Vector3 (0f, 0f, 0f);
		
		// Shark behaviour values
		speed = 4.0f;
		foodCapacity = 1.0f;
		foodLevel = Random.Range(0.7f, 1.0f);
		hunger = 0.0f;
		
		// Set a place holder value for the closest fish.
		closestFishDistance = 50f;

		// increase hunger / decay every second.
		InvokeRepeating("decayHunger", digestionTime, digestionTime);
	}
	
	// Update is called once per frame
	void Update() {
		// if the shark list is null
		if (sharks == null) {
			// get the other sharks
			sharks = GameObject.FindGameObjectsWithTag ("shark");
		}
		// if the fish list is null
		if (fishes == null) {
			// get the fish
			fishes = GameObject.FindGameObjectsWithTag ("fish");
		} else {
			// deal with the boid escape case - must stay within simulation_radius
			if (checkBoundry()) {return;}

			// Calculate hunger.
			hunger = Mathf.Abs(1 - (foodLevel/foodCapacity));
			speed = base_speed * (hunger + 1);
			
			// Find the closest fish to this shark. Checks 1 fish per frame.
			findClosestFish();
			
			// If hungry, chase fish and eat one if it's close.
			chaseFish();
			
			// Add group cohesion force to this shark.
			groupCohesion();

			// Add separation or alignment forces if applicable.
			separationAndAlignment();
		}
	}
	
	// Checks that the sharks are within bounds, moves them back if they are outside.
	private bool checkBoundry() {
		// deal with the fish escape case - must stay within simulation_radius
		if (Vector3.Distance(simulation_centre, transform.position) > simulation_radius && (hunger < hunger_threshold)) {
			float turnSpeed = 3.0f * Time.deltaTime;
			// set target direction
			Vector3 targetDir = simulation_centre - transform.position;
			Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, turnSpeed, 0.0F);
			transform.rotation = Quaternion.LookRotation(newDir);
			rigidbody.AddForce((-speed*2) * Vector3.Normalize(transform.position - simulation_centre));			
			return true;
		}
		return false;
	}
	
	// Find the closest fish to me.
	private void findClosestFish() {

		GameObject fish = fishes[fish_index];
		fish_script fishScript = fish.GetComponent<fish_script>();		
			
		if (fishScript.isAlive) {
			Vector3 fishPosition = fish.transform.position;
			float fishDistance = Vector3.Distance(transform.position, fishPosition);
			
			if (fishDistance < closestFishDistance) {
				closestFish = fish;
				closestFishDistance = fishDistance;
			}
		}

		fish_index++;	
		if (fish_index >= fishes.Length) {
			fish_index = 0;
			finalClosestFish = closestFish;
			closestFishDistance = 50f;
		}		
	}
	
	// Chase the closest fish.
	private void chaseFish() {
		if (hunger >= hunger_threshold) {
			fish_script closestFishScript = finalClosestFish.GetComponent<fish_script>();
					
			float step = 4.0f * Time.deltaTime;
			Vector3 fishPosition = closestFish.transform.position;
			float fishDistance = Vector3.Distance(transform.position, fishPosition);			
			Vector3 targetDir = fishPosition - transform.position;			
			Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, step, 0.0F);
						
			rigidbody.AddForce(-speed * Vector3.Normalize(transform.position - fishPosition));
			transform.rotation = Quaternion.LookRotation(newDir);

			// If the fish is quite close, eat the fish.
			if (fishDistance < 2.0f) {
				closestFishScript.eatenByShark();
				foodLevel += closestFishScript.foodValue;
			}
		}
	}	
	

	// Add group cohesion force to this shark.
	private void groupCohesion() {
		sharks_index++;
		// if the time count exceed the time delta
		if (sharks_index >= sharks.Length && hunger < hunger_threshold) {
			// computer the scale
			Vector3 cohesive_force = (cohesion_strength/Vector3.Distance(cohesion_pos, transform.position))*(cohesion_pos - transform.position);
			// apply force
			rigidbody.AddForce(cohesive_force);
			// zero the time counter
			sharks_index = 0;
			// zero the cohesion vector
			cohesion_pos.Set(0f, 0f, 0f);
		}
		else if (sharks_index >= sharks.Length) {
			// zero the shark index
			sharks_index = 0;
			// zero the cohesion vector
			cohesion_pos.Set(0f, 0f, 0f);		
		}
	}
	
	// Add separation or alignment forces if applicable.	
	private void separationAndAlignment() {
		// position of shark at index
		Vector3 pos = sharks[sharks_index].transform.position;
		Quaternion rot = sharks[sharks_index].transform.rotation;
		// the distance
		float dist = Vector3.Distance(transform.position, pos);
		
		// if not this boid
		if (dist > 0f) {
			// if within separation
			if (dist <= separation_distance) {
				// compute the scale of separation
				float scale = separation_strength/dist;
				// add a separation force between this boid and its neighbour
				rigidbody.AddForce(scale * Vector3.Normalize(transform.position - pos));
				
			} else if (dist < cohesion_distance && dist > separation_distance && hunger < hunger_threshold) { // if within cohesive distance but not separation
				// compute the cohesive position
				cohesion_pos = cohesion_pos + pos*(1f/(float)sharks.Length);
				// alignment - small rotations are applied based on the alignments of the neighbours
				transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 5f);
				
			} 
			// Wander forwards.
			rigidbody.AddForce(transform.forward * speed);				
		}	
	}

	// Decays the sharks food level causing them to hunger over time.
	public void decayHunger() {
		foodLevel = digestionRate*foodLevel;
		if (foodLevel > foodCapacity) {foodLevel = foodCapacity;}
	}
}
