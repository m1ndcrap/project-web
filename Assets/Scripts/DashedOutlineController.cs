using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class DashedOutlineController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private SpriteRenderer myRenderer;
    private Material dashedOutline;
    [SerializeField] private Color outlineColor = Color.white;

    void Start()
    {
        dashedOutline = myRenderer.material;
    }

    void LateUpdate()
    {
        myRenderer.sprite = mainRenderer.sprite;
        myRenderer.flipX = mainRenderer.flipX;
        dashedOutline.color = outlineColor;
    }
}
