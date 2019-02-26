using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseRot : MonoBehaviour
{
	[SerializeField] float _sensitivity = 0.01f;

	Vector3 prevMouse;
	Vector3 rot;

    // Start is called before the first frame update
    void Start()
    {
        prevMouse = Input.mousePosition;
    }

    // Update is called once per frame
	
    void Update()
    {
		Vector3 delta = Input.mousePosition - prevMouse;

		if (Input.GetMouseButton(1)) {
			rot += new Vector3(-delta.y*_sensitivity, delta.x*_sensitivity, 0);
			transform.eulerAngles = rot;
		}

        prevMouse = Input.mousePosition;
    }
}
