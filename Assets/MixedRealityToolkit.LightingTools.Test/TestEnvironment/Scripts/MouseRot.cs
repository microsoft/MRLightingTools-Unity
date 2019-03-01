using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseRot : MonoBehaviour
{
	[SerializeField] float _sensitivity = 0.01f;
    [SerializeField] float _moveSensitivity = .5f;

    Vector3 prevMouse;
	Vector3 rot;

    // Start is called before the first frame update
    void Start()
    {
        prevMouse = Input.mousePosition;
        rot = transform.eulerAngles;
    }

    // Update is called once per frame
	
    void Update()
    {
		Vector3 delta = Input.mousePosition - prevMouse;

		if (Input.GetMouseButton(1)) {
			rot += new Vector3(-delta.y*_sensitivity, delta.x*_sensitivity, 0);
			transform.eulerAngles = rot;
		}
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.A)) movement += -transform.right;
        if (Input.GetKey(KeyCode.D)) movement += transform.right;
        if (Input.GetKey(KeyCode.W)) movement += transform.forward;
        if (Input.GetKey(KeyCode.S)) movement += -transform.forward;
        if (Input.GetKey(KeyCode.Q)) movement += -transform.up;
        if (Input.GetKey(KeyCode.E)) movement += transform.up;

        transform.position += movement * Time.deltaTime * _moveSensitivity;

        prevMouse = Input.mousePosition;
    }
}
