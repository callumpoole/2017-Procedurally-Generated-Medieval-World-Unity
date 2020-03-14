using UnityEngine;
using System.Collections;

public class CameraMove : MonoBehaviour {

    private const float MOVE_SPEED = 500f;
    private const float QE_ROTATION_SPEED = 150f;
    private const float SCROLL_SPEED = 1200f;
    private const float MOUSE_SENSITIVITY_X = 60f;
    private const float MOUSE_SENSITIVITY_Y = 60f;

    void Start () {
        Cursor.lockState = CursorLockMode.Locked;
    }
	
	void Update () {
        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();
        Vector3 left = Vector3.Cross(forward, new Vector3(0, 1, 0));

        float speedMultiplied = Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftShift))
            speedMultiplied *= 2.5f;

        if (Input.GetKey(KeyCode.W))
            transform.position += MOVE_SPEED * forward * speedMultiplied;
        if (Input.GetKey(KeyCode.S))
            transform.position += MOVE_SPEED * -forward * speedMultiplied;
        if (Input.GetKey(KeyCode.A))
            transform.position += MOVE_SPEED * left * speedMultiplied;
        if (Input.GetKey(KeyCode.D))
            transform.position += MOVE_SPEED * -left * speedMultiplied;
        if (Input.GetKey(KeyCode.Q))
            transform.eulerAngles += QE_ROTATION_SPEED * new Vector3(0, -1, 0) * speedMultiplied;
        if (Input.GetKey(KeyCode.E))                            
            transform.eulerAngles += QE_ROTATION_SPEED * new Vector3(0, 1, 0) * speedMultiplied;

        float d = Input.GetAxis("Mouse ScrollWheel");
        if (d > 0f) // scroll up
            transform.localPosition += SCROLL_SPEED * transform.forward * speedMultiplied;
        else if (d < 0f)  // scroll down
            transform.localPosition += SCROLL_SPEED * -transform.forward * speedMultiplied;

        float mouseY = Input.GetAxis("Mouse Y");
        float mouseX = Input.GetAxis("Mouse X");
        transform.eulerAngles += Time.deltaTime * MOUSE_SENSITIVITY_X * new Vector3(0, mouseX, 0);
        transform.eulerAngles += Time.deltaTime * MOUSE_SENSITIVITY_Y * new Vector3(-mouseY, 0, 0); 
    }
}
