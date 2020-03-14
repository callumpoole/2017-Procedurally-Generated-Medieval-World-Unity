using UnityEngine;
using System.Collections;

public class BoatBob : MonoBehaviour {

    float startHeight = 0;
    float height = 0;
    const float DELTA_HEIGHT_SPEED = 0.05f;
    const float DELTA_HEIGHT = 5;
    const float DELTA_YAW = 0.05f;

    void Start() {
        startHeight = transform.position.y;
    }
	
	void FixedUpdate() {
        height += DELTA_HEIGHT_SPEED;
        transform.position = new Vector3(transform.position.x, startHeight + Mathf.Sin(height) * DELTA_HEIGHT, transform.position.z);
        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y + DELTA_YAW, 0);
	}
}
