using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] float maxSpeed = 30f;
    [SerializeField] float acceleration = 15f;
    [SerializeField] float brakeForce = 25f;
    [SerializeField] float drag = 2f;
    [SerializeField] float lateralGrip = 8f;

    [Header("Steering")]
    [SerializeField] float turnSpeed = 120f;
    [SerializeField] float minSpeedForTurn = 1f;
    [SerializeField, Range(0f, 1f)] float turnSpeedFalloff = 0.5f;

    [Header("Wheels")]
    [SerializeField] Transform wheelFrontLeft;
    [SerializeField] Transform wheelFrontRight;
    [SerializeField] Transform wheelBackLeft;
    [SerializeField] Transform wheelBackRight;
    [SerializeField] float wheelRadius = 0.15f;
    [SerializeField] float maxSteerAngle = 25f;

    [Header("Ground Offset")]
    [SerializeField] float groundOffset = 0.0001f;

    [Header("Physics (Runtime Read-Only)")]
    [SerializeField, Tooltip("Current forward speed m/s")] float debugSpeed;
    [SerializeField, Tooltip("Current gear throttle 0-1")] float debugThrottle;
    [SerializeField, Tooltip("Current steer input -1 to 1")] float debugSteer;

    Rigidbody rb;
    float currentSpeed;
    float steerInput;
    float throttle;
    float wheelSpinAngle;

    public float CurrentSpeed => currentSpeed;
    public float CurrentSpeedKmh => currentSpeed * 3.6f;
    public float NormalizedSpeed => maxSpeed > 0f ? Mathf.Clamp01(currentSpeed / maxSpeed) : 0f;
    public float ThrottleNormalized => throttle;
    public float MaxSpeed => maxSpeed;
    public float SteerInput => steerInput;
    public Vector3 Velocity => rb ? rb.linearVelocity : Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearDamping = drag;
        rb.angularDamping = 4f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        AutoFindWheels();
    }

    void AutoFindWheels()
    {
        // Auto-assign wheels from the CarModel child if not set in inspector
        Transform model = transform.Find("CarModel");
        if (model == null) return;

        if (wheelFrontLeft == null)
            wheelFrontLeft = FindChildRecursive(model, "wheelFrontLeft");
        if (wheelFrontRight == null)
            wheelFrontRight = FindChildRecursive(model, "wheelFrontRight");
        if (wheelBackLeft == null)
            wheelBackLeft = FindChildRecursive(model, "wheelBackLeft");
        if (wheelBackRight == null)
            wheelBackRight = FindChildRecursive(model, "wheelBackRight");
    }

    static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    public void SetSteerInput(float value) => steerInput = Mathf.Clamp(value, -1f, 1f);
    public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);

    void FixedUpdate()
    {
        float targetSpeed = maxSpeed * throttle;
        currentSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);

        float speedDiff = targetSpeed - currentSpeed;
        if (Mathf.Abs(speedDiff) > 0.1f)
        {
            float forceAmount = speedDiff > 0f ? acceleration : -brakeForce;
            rb.AddForce(transform.forward * forceAmount, ForceMode.Acceleration);
        }

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        if (Mathf.Abs(currentSpeed) > minSpeedForTurn)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / (maxSpeed * turnSpeedFalloff));
            float turn = steerInput * turnSpeed * speedFactor * Time.fixedDeltaTime;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turn, 0f));
        }

        Vector3 lateralVelocity = Vector3.Dot(rb.linearVelocity, transform.right) * transform.right;
        rb.AddForce(-lateralVelocity * lateralGrip, ForceMode.Acceleration);
    }

    void Update()
    {
        SpinWheels();
        SteerFrontWheels();

        // Debug inspector readouts
        debugSpeed = currentSpeed;
        debugThrottle = throttle;
        debugSteer = steerInput;
    }

    void SpinWheels()
    {
        if (wheelRadius <= 0f) return;

        // Degrees per second = (linear speed / wheel circumference) * 360
        float degreesPerSecond = (currentSpeed / (2f * Mathf.PI * wheelRadius)) * 360f;
        wheelSpinAngle += degreesPerSecond * Time.deltaTime;
        wheelSpinAngle %= 360f;

        // The FBX model child is rotated 180° on Y, so the wheel's local spin axis
        // is inverted: spin around local X, but negative to roll forward visually
        Quaternion spinRotation = Quaternion.Euler(-wheelSpinAngle, 0f, 0f);

        if (wheelBackLeft != null)
            wheelBackLeft.localRotation = spinRotation;
        if (wheelBackRight != null)
            wheelBackRight.localRotation = spinRotation;

        // Front wheels: spin + steer angle combined
        float steerY = steerInput * maxSteerAngle;
        Quaternion frontRotation = Quaternion.Euler(-wheelSpinAngle, steerY, 0f);

        if (wheelFrontLeft != null)
            wheelFrontLeft.localRotation = frontRotation;
        if (wheelFrontRight != null)
            wheelFrontRight.localRotation = frontRotation;
    }

    void SteerFrontWheels()
    {
        // Visual steer is already handled in SpinWheels via the combined rotation
    }

    public void ResetCar(Vector3 position, Quaternion rotation)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(position, rotation);
        steerInput = 0f;
        throttle = 0f;
        currentSpeed = 0f;
        wheelSpinAngle = 0f;
    }
}
