using UnityEngine;
using System.Collections;

public class fish_script : MonoBehaviour {

	// the global centre of the simulation
	private Vector3 simulation_centre = new Vector3(0f, 0f, 0f);
	// the global radius of the simulation
	private float simulation_radius = 20f;
	// the fish maximum velocity
	private float base_speed = 3f;
	// the distance where sepration is applied
	private float separation_distance = 1.0f;
	// the distance where cohesion is applied
	private float cohesion_distance = 15.0f;
	// the sepration strength
	private float separation_strength = 50.0f;
	// the strength of cohesion
	private float cohesion_strength = 10f;
	

	// the list of fish and shark neighbours
	private GameObject [] fish;
	private GameObject [] sharks;

	// the cohesive position
	private Vector3 cohesion_pos;

	// the fish and shark index
	private int fish_index;
	private int shark_index;
	
	// the food value this fish has to sharks
	public float foodValue;

	// the respawn time after dying
	private float respawnTime = 8.0f;
	private float deathTime;
	
	// state (is alive or not)
	public bool isAlive = true;

	// Fear value. (how scared it is depending on nearby sharks)
	public float fear = 0.0f;
	
	// The distance away from sharks I'm comfortable.
	public float comfort_distance = 8.0f;
	
	public float speed;
	private float sumFears;
	
	private GameObject shark;
	private float sharkDistance;
	private Vector3 sharkPosition;
	private float sharkSpeed;
	
	private Color colour;

	// Use this for initialization
	void Start () {
		// initialise to null (this script might start while other fish are still being created)
		fish = null;
		sharks = null;
		// set fish index
		fish_index = 0;
		shark_index = 0;
		// create the cohesion vector
		cohesion_pos = new Vector3 (0f, 0f, 0f);

		// set the food value for this fish.
		foodValue = 0.8f;
		
		colour = renderer.material.color;
	}
	
	// Update is called once per frame
	void Update() {
		// if the sharks list is null get all sharks
		if (sharks == null) { sharks = GameObject.FindGameObjectsWithTag ("shark");	}
		
		// if the fish list is null get the other fish
		if (fish == null) {	fish = GameObject.FindGameObjectsWithTag ("fish");	} 
		
		// If this fish is outside of simulation radius, make it move back.
		//if (checkBoundry()) {return;}
		
		// IF this fish is dead and the death time has expired, respawn.
		if (!isAlive && (Time.time - deathTime >= respawnTime)){ 
			respawn(); 
		} 
		
		else if (isAlive) {
			// If this fish is outside of simulation radius, make it move back.
			checkBoundry();
		
			// Get shark and it's position and distance.
			getSharkInfo();
			
			calculateFear();
			
			// If a shark is close run away.
			runFromSharks();

			// Add group cohesion force to this fish.			
			groupCohesion();
			
			// Add separation or alignment forces if applicable.
			separationAndAlignment();
		}
	}

	// Checks that the fish are within bounds, moves them back if they are outside.	
	private bool checkBoundry() {
		// deal with the fish escape case - must stay within simulation_radius
		if (Vector3.Distance(simulation_centre, transform.position) > simulation_radius) {
			float turnSpeed = 4.0f * Time.deltaTime;
			// set target direction
			Vector3 targetDir = simulation_centre - transform.position;
			Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, turnSpeed, 0.0F);
			transform.rotation = Quaternion.LookRotation(newDir);
			rigidbody.AddForce(-speed * Vector3.Normalize(transform.position - simulation_centre));
			return true;
		}
		return false;
	}
	
	// Get information about the shark we're looking it.
	private void getSharkInfo() {
		shark = sharks[shark_index];
		//sharkSpeed = shark.GetComponent<shark_script>().speed;
		sharkPosition = shark.transform.position;
		sharkDistance = Vector3.Distance(transform.position, sharkPosition);	
	}
	
	// Calculate how afraid this dish is based on shark distance, etc.
	private void calculateFear() {
		// Calculate fear of the current predator
		fear = (comfort_distance/sharkDistance);
		if (fear > 1.0f) { fear = 1.0f; }
		
		// Add to the sum of fears.
		sumFears += fear;
		shark_index++;
		
		if (shark_index >= sharks.Length) {
			// Calculate sum of all fears
			sumFears = (sumFears/(float)sharks.Length);
			if (sumFears > 1.0f) { sumFears = 1.0f; }
			
			//Determing max speed
			speed = base_speed * (sumFears + 1.0f);
			
			// Reset Counters
			sumFears = 0;
			shark_index = 0;
		}
	}
	
	// If shark is within comfort zone, move away from it's position	
	private void runFromSharks() {
		if (sharkDistance < comfort_distance) {
			float step = 10.0f * Time.deltaTime;
			// set target direction
			Vector3 targetDir = sharkPosition - transform.position;
			Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, -step, 0.0F);	
			transform.rotation = Quaternion.LookRotation(newDir);
			rigidbody.AddForce(speed * Vector3.Normalize(transform.position - sharkPosition));
		}
	}
	
	// Add group cohesion force to this fish.
	private void groupCohesion() {
		// increment the index into the fish list
		fish_index++;
		// if the time count exceed the time delta
		if (fish_index >= fish.Length) {
			// compute the scale
			Vector3 cohesive_force = (cohesion_strength/Vector3.Distance(cohesion_pos, transform.position))*(cohesion_pos - transform.position);
			// apply force
			rigidbody.AddForce(cohesive_force);
			// zero the time counter
			fish_index = 0;
			// zero the cohesion vector
			cohesion_pos.Set(0f, 0f, 0f);
		}		
	}
	
	// Add separation or alignment forces if applicable.	
	private void separationAndAlignment() {
		// position and distance of fish at index
		Vector3 pos = fish[fish_index].transform.position;
		Quaternion rot = fish[fish_index].transform.rotation;
		float dist = Vector3.Distance(transform.position, pos);
		fish_script fishScript = fish[fish_index].GetComponent<fish_script>();
		
		// if not this fish
		if (dist > 0f && fishScript.isAlive == true) {
			// if within separation
			if (dist <= separation_distance) {
				// compute the scale of separationt
				float scale = separation_strength/dist;
				// add a separation force between this fish and its neighbour
				rigidbody.AddForce(scale * Vector3.Normalize(transform.position - pos));
				
			} else if (dist < cohesion_distance && dist > separation_distance) { // if within cohesive distance but not separation
				// compute the cohesive position
				cohesion_pos = cohesion_pos + pos*(1f/(float)fish.Length);
				// alignment - small rotations are applied based on the alignments of the neighbours
				transform.rotation = Quaternion.RotateTowards(transform.rotation, rot, 10f);
				
			} 
			// Wander forwards.
			rigidbody.AddForce(transform.forward * speed);
		}	
	}
	
	// Get eaten by a shark and change it visually to match.
	public void eatenByShark() {
		deathTime = Time.time;
		isAlive = false;

		// Set fish to be transparent by altering Alpha
		colour.a = 0.3f;
		renderer.material.color = colour;
	}
	
	// Respawn after a period of time and revert to the old visuals.
	private void respawn() {
		isAlive = true;
		
		// Set fish to be opaque by altering Alpha
		colour.a = 1.0f;
		renderer.material.color = colour;
	}
}
