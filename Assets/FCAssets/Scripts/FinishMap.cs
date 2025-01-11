using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FinishMap : MonoBehaviour
{

    public GameObject congratsScreen;
    public Transform Player;
    public Rigidbody PlayerBody;
    float spawnX;
    float spawnY;
    float spawnZ;

    // Start is called before the first frame update
    void Start()
    {
        congratsScreen.SetActive(false);
        spawnX = Player.position.x;
        spawnY = Player.position.y;
        spawnZ = Player.position.z;
    }

    public void BackToMenu()
    {
        Destroy(GameObject.Find("Main Menu"));
        SceneManager.LoadScene("Main Menu");
    }

    public void RestartMap()
    {
        Vector3 spawn = new Vector3(spawnX, spawnY + 0.2f, spawnZ);
        PlayerBody.velocity = new Vector3(0f, 0f, 0f);
        Player.position = spawn;
        congratsScreen.SetActive(false);
        Debug.Log("A");
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
