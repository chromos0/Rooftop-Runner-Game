using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraMovement: MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform DefaultPoint;

    public Transform orientation;

    float xRotation;
    float yRotation;

    bool MenuOpened = false;

    public GameObject EmotesScreen;
    public GameObject ChatMessage;
    public GameObject Options;
    public GameObject ModeVotingMenu;
    public GameObject VotingMenu;
    public GameObject WinMenu;
    public GameObject Chat;


    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        xRotation = orientation.rotation.x;
        yRotation = orientation.rotation.y;
    }

    Vector3 RoundVector(Vector3 vector)
    {
        float roundedX = Mathf.Round(vector.x);
        float roundedY = Mathf.Round(vector.y);
        float roundedZ = Mathf.Round(vector.z);

        return new Vector3(roundedX, roundedY, roundedZ);
    }

    void Update()
    {
        if (EmotesScreen.activeSelf|| Options.activeSelf || VotingMenu.activeSelf || WinMenu.activeSelf || ModeVotingMenu.activeSelf || Chat.activeSelf)
        {
            MenuOpened = true;
        } else if (EventSystem.current.currentSelectedGameObject == ChatMessage){
            MenuOpened = true;
        }
        else
        {
            MenuOpened = false;
        }

        //Debug.Log("Delusion: " + MenuOpened);

        if (!MenuOpened)
        {
            if (RoundVector(transform.position) == RoundVector(DefaultPoint.position)){
                float mouseX = Input.GetAxisRaw("Mouse X") * Time.fixedDeltaTime * sensX;
                float mouseY = Input.GetAxisRaw("Mouse Y") * Time.fixedDeltaTime * sensY;
                yRotation += mouseX;
                xRotation -= mouseY;
                xRotation = Mathf.Clamp(xRotation, -90f, 90f);
                transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
                orientation.rotation = Quaternion.Euler(0, yRotation, 0);
            }
            else{
                  transform.rotation = DefaultPoint.rotation;
            }
        }
    }
}
