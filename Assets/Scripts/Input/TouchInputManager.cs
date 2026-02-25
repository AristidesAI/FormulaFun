using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class TouchInputManager : MonoBehaviour
{
    [SerializeField] CarController carController;

    [Header("UI References")]
    [SerializeField] RectTransform leftButton;
    [SerializeField] RectTransform rightButton;
    [SerializeField] SpeedKnobUI speedKnob;

    float steerValue;
    bool leftPressed;
    bool rightPressed;

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        if (carController == null) return;

        leftPressed = false;
        rightPressed = false;

        // Process all active touches
        foreach (var touch in Touch.activeTouches)
        {
            Vector2 screenPos = touch.screenPosition;

            if (RectTransformContainsScreenPoint(leftButton, screenPos))
                leftPressed = true;

            if (RectTransformContainsScreenPoint(rightButton, screenPos))
                rightPressed = true;
        }

        // Also support keyboard for editor testing
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                leftPressed = true;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                rightPressed = true;
        }

        // Combine steering
        steerValue = 0f;
        if (leftPressed) steerValue -= 1f;
        if (rightPressed) steerValue += 1f;

        carController.SetSteerInput(steerValue);

        // Speed knob handles its own touch input and calls carController.SetThrottle
    }

    bool RectTransformContainsScreenPoint(RectTransform rect, Vector2 screenPoint)
    {
        if (rect == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, null);
    }

    public void SetCarController(CarController cc) => carController = cc;
}
