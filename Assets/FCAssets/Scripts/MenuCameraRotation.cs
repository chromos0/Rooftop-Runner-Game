using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuCameraRotation : MonoBehaviour
{
    public GameObject target;

    void Start()
    {
        Debug.Log("Menu Camera Rotation Started");
    }

    void Update()
    {
        transform.RotateAround(target.transform.position, Vector3.up, 0.35f * Time.fixedDeltaTime);
        transform.LookAt(target.transform);
    }
}
