using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomSplah : MonoBehaviour
{
    public GameObject logo;
    private RawImage logoImage;

    public float fadeIncrement = 0.04f;
    private float alpha = 0;
    private bool fadeIn = true;
    private bool fadeOut = false;

    void Start()
    {
        logoImage = logo.GetComponent<RawImage>();
    }

    void HideSplash(){
        gameObject.SetActive(false);
    }

    void FadeOut(){
        fadeOut = true;
    }

    void FixedUpdate()
    {
        if (alpha >= 1f && fadeIn){
            fadeIn = false;
            Invoke("FadeOut", 0.8f);
        }

        if (fadeIn)
        {
            alpha += fadeIncrement;
            logoImage.color = new Color(255, 255, 255, alpha);
        }

         if (alpha <= 1f && fadeOut){
            Invoke("HideSplash", 1);
        }

        if (fadeOut){
            alpha -= fadeIncrement;
            logoImage.color = new Color(255, 255, 255, alpha);
        }
    }
}
