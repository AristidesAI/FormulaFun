using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SpeedKnobUI : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] CarController carController;
    [SerializeField] RectTransform knobHandle;
    [SerializeField] RectTransform knobTrack;
    [SerializeField] TMP_Text gearLabel;

    [Header("Gear Settings")]
    [SerializeField] int gearCount = 5;
    [SerializeField] float snapStrength = 10f;

    int currentGear;
    float knobNormalized; // 0 = bottom (idle), 1 = top (max gear)
    bool isDragging;

    readonly string[] gearNames = { "N", "1", "2", "3", "4", "5" };

    // Compute track height live — anchor-stretched RectTransforms return 0 in Start()
    float TrackHeight => knobTrack != null ? knobTrack.rect.height : 200f;

    void Start()
    {
        SetGear(0);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        UpdateKnobFromPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        UpdateKnobFromPointer(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        int nearestGear = Mathf.RoundToInt(knobNormalized * gearCount);
        SetGear(nearestGear);
    }

    void UpdateKnobFromPointer(PointerEventData eventData)
    {
        if (knobTrack == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            knobTrack, eventData.position, eventData.pressEventCamera, out Vector2 localPoint);

        float h = TrackHeight;
        if (h < 1f) h = 200f; // fallback if layout not ready
        float halfHeight = h * 0.5f;
        float normalized = Mathf.Clamp01((localPoint.y + halfHeight) / h);
        knobNormalized = normalized;

        UpdateKnobVisual();
        UpdateThrottle();
    }

    void SetGear(int gear)
    {
        currentGear = Mathf.Clamp(gear, 0, gearCount);
        knobNormalized = (float)currentGear / gearCount;
        UpdateKnobVisual();
        UpdateThrottle();
    }

    void UpdateKnobVisual()
    {
        if (knobHandle == null) return;
        float h = TrackHeight;
        if (h < 1f) return; // layout not ready yet
        float yPos = Mathf.Lerp(-h * 0.5f, h * 0.5f, knobNormalized);
        knobHandle.anchoredPosition = new Vector2(0f, yPos);

        int displayGear = Mathf.RoundToInt(knobNormalized * gearCount);
        if (gearLabel != null && displayGear < gearNames.Length)
            gearLabel.text = gearNames[displayGear];
    }

    void UpdateThrottle()
    {
        if (carController != null)
            carController.SetThrottle(knobNormalized);
    }

    void Update()
    {
        if (!isDragging)
        {
            float targetNorm = (float)currentGear / gearCount;
            knobNormalized = Mathf.Lerp(knobNormalized, targetNorm, snapStrength * Time.unscaledDeltaTime);
            UpdateKnobVisual();
            UpdateThrottle();
        }

        // Keyboard support for editor testing
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.wKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Keyboard.current.upArrowKey.wasPressedThisFrame)
                SetGear(currentGear + 1);
            if (UnityEngine.InputSystem.Keyboard.current.sKey.wasPressedThisFrame ||
                UnityEngine.InputSystem.Keyboard.current.downArrowKey.wasPressedThisFrame)
                SetGear(currentGear - 1);
        }
    }

    public void SetCarController(CarController cc) => carController = cc;
}
