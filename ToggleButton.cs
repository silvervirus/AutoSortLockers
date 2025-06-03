using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace AutosortLockers
{
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
            float controllerDPadYValue = Input.GetAxisRaw("Joystick Axis 6");
            bool controllerDPadYPressed = Mathf.Abs(controllerDPadYValue) > 0.5f;

            if (enabled &&
                (eventData.button == PointerEventData.InputButton.Right ||
                 eventData.button == PointerEventData.InputButton.Left ||
                  GameInput.GetKeyDown(KeyCode.Joystick1Button4) || GameInput.GetKeyDown(KeyCode.Joystick1Button1)|| GameInput.GetButtonDown(GameInput.Button.Reload)))
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
}