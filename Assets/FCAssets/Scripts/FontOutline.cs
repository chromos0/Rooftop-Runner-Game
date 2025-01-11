using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FontOutline : MonoBehaviour
{
    void Awake()
    {
        TextMeshPro textmeshPro = GetComponent<TextMeshPro>();
        textmeshPro.outlineWidth = 0.3f;
        textmeshPro.outlineColor = new Color32(0, 0, 0, 255);
    }
}
