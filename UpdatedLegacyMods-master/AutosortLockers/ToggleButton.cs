using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ToggleButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public bool pointerOver;
    public RectTransform rectTransform;
    public Action onClick = delegate { };

    // Add a variable to store the key for PlayerPrefs
    public string toggleKey;

    private Toggle toggle;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        toggle = GetComponent<Toggle>();
        // Load the toggle state from PlayerPrefs
        toggle.isOn = PlayerPrefs.GetInt(toggleKey, toggle.isOn ? 1 : 0) == 1;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (enabled && eventData.button == PointerEventData.InputButton.Right)
        {
            // Toggle the toggle state
            toggle.isOn = !toggle.isOn;
            // Save the toggle state to PlayerPrefs
            PlayerPrefs.SetInt(toggleKey, toggle.isOn ? 1 : 0);
            PlayerPrefs.Save(); // Save changes immediately
        }
    }

    public void Update()
    {
        var hover = enabled && pointerOver;
        transform.localScale = new Vector3(hover ? 0.15f : 0.1f, hover ? 0.15f : 0.1f, 0.1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
    }
}