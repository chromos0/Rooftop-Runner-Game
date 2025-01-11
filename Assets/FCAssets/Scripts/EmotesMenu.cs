using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

using UnityEngine;

public class EmotesMenu : MonoBehaviour
{
    bool emotesMenuOpened = false;
    public GameObject EmotesScreen;
    public GameObject Player;
    public GameObject PlayerModel;
    public GameObject MPCanvas;
    public GameObject Chat;

    public Transform DefaultPoint;
    public Transform PlayerTransform;
    public Rigidbody CameraBody;

    private Animator PlayerAnimator;
    private Rigidbody PlayerRigidbody;

    bool CameraCoroutine;

    void Start()
    {
        EmotesScreen.SetActive(false);
        PlayerAnimator = PlayerModel.GetComponent<Animator>();
        PlayerRigidbody = Player.GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != "Main Menu"){
            GameObject Win = GameObject.Find("WinMenu");

            if (GameObject.Find("Options") == null && Win == null && !Chat.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.B))
                {
                    if (!emotesMenuOpened)
                    {
                        OpenMenu();
                    }
                }
                if (Input.GetKeyUp(KeyCode.B))
                {
                    if(emotesMenuOpened)
                    {
                        CloseMenu();
                    }
                }
            } else
            {
                CloseMenu();
            }
        }
    }

    void OpenMenu()
    {
        EmotesScreen.SetActive(true);
        emotesMenuOpened = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (!MPCanvas.activeSelf)
        {
            Time.timeScale = 0f;
        }
    }

    void CloseMenu()
    {
        EmotesScreen.SetActive(false);
        emotesMenuOpened = false;
        if (GameObject.Find("Options") == null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Time.timeScale = 1f;
        }
    }
    
    
    Vector3 RoundVector(Vector3 vector)
    {
        float roundedX = Mathf.Round(vector.x);
        float roundedY = Mathf.Round(vector.y);
        float roundedZ = Mathf.Round(vector.z);

        return new Vector3(roundedX, roundedY, roundedZ);
    }

    IEnumerator slowDownCamera(){
        if (RoundVector(CameraBody.position) == RoundVector(DefaultPoint.position) && !CameraCoroutine){
            PlayerRigidbody.isKinematic = true;
            Debug.Log("moving camera for emotes");
            CameraCoroutine = true;
            CameraBody.isKinematic = false; 
            CameraBody.AddForce(-PlayerTransform.forward*15f + 2*Vector3.up, ForceMode.Impulse);
            yield return new WaitForSeconds(0.32f);
            CameraBody.isKinematic = true;
            PlayerRigidbody.isKinematic = false;
            CameraCoroutine = false;
        }
    }

    public void Emote0()
    {
        StartCoroutine(slowDownCamera());
        PlayerAnimator.SetTrigger("Emote1");
        CloseMenu();
    }

    public void Emote1()
    {
        StartCoroutine(slowDownCamera());
        PlayerAnimator.SetTrigger("Emote2");
        CloseMenu();
    }

    public void Emote2()
    {
        StartCoroutine(slowDownCamera());
        PlayerAnimator.SetTrigger("Emote3");
        CloseMenu();
    }

    public void Emote3()
    {
        StartCoroutine(slowDownCamera());
        PlayerAnimator.SetTrigger("Emote4");
        CloseMenu();
    }
}
