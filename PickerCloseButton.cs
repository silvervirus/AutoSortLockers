using UnityEngine;
using UnityEngine.EventSystems;

namespace AutosortLockers
{
    public class PickerCloseButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        public bool pointerOver;
        public AutosortTarget target;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (target != null && enabled && (eventData.button == PointerEventData.InputButton.Right) || (eventData.button == PointerEventData.InputButton.Left) || (GameInput.GetKeyDown(KeyCode.Joystick1Button1)))
            {
                target.HideConfigureMenu();
            }
        }

        public void Update()
        {
            transform.localScale = new Vector3(pointerOver ? 1.3f : 1, pointerOver ? 1.3f : 1, 1);
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