using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;

    [Header("Isometric Settings")]
    [SerializeField] float cameraAngleX = 45f;
    [SerializeField] float cameraAngleY = 45f;
    [SerializeField] float cameraDistance = 25f;

    Vector3 offset;
    Quaternion fixedRotation;

    void Start()
    {
        CalculateIsometricView();
    }

    void CalculateIsometricView()
    {
        // Fixed isometric rotation — never changes
        fixedRotation = Quaternion.Euler(cameraAngleX, cameraAngleY, 0f);
        transform.rotation = fixedRotation;

        // Offset is derived from the rotation looking back at distance
        offset = fixedRotation * new Vector3(0f, 0f, -cameraDistance);
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Only position follows the car; rotation stays fixed
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Rotation never changes — true isometric
        transform.rotation = fixedRotation;
    }

    public void SetTarget(Transform t)
    {
        target = t;
        CalculateIsometricView();
    }
}
