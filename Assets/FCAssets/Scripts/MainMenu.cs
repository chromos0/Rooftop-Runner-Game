using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{   
    public GameObject Canvas;
    public GameObject MainMenu;
    public GameObject Options;
    public GameObject ChooseMapScreen;
    public GameObject MultiplayerScreen;
    private GameObject WinScreen;
    public GameObject Chat;
    private GameObject GameCanvas;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Options.SetActive(false);
        ChooseMapScreen.SetActive(false);
        MultiplayerScreen.SetActive(false);
        MainMenu.SetActive(true);
        GameCanvas = GameObject.Find("Game Canvas");
        WinScreen = GameObject.Find("WinMenu");
    }

    public void PlayMap(int index)
    {
        Canvas.SetActive(false);
        Chat.SetActive(false);
        GameCanvas.SetActive(true);
        SceneManager.LoadScene(index);
    }

    public void RetryMapScene(Scene scene, LoadSceneMode mode)
    {
        WinScreen.SetActive(false);
        SceneManager.sceneLoaded -= RetryMapScene;
    }

    public void RetryMap()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        SceneManager.sceneLoaded += RetryMapScene;
    }

    public void QuitButton()
    {
        Application.Quit();
    }

    public void OptionsButton()
    {
        Options.SetActive(true);
        MainMenu.SetActive(false);
    }

    public void MultiplayerButton()
    {
        MultiplayerScreen.SetActive(true);
        MainMenu.SetActive(false);
    }

    public void ChooseMapButton()
    {
        ChooseMapScreen.SetActive(true);
        MainMenu.SetActive(false);
    }

    public void BackButton()
    {
        Options.SetActive(false);
        ChooseMapScreen.SetActive(false);
        MultiplayerScreen.SetActive(false);
        MainMenu.SetActive(true);
    }
}
