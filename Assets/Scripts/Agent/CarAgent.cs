using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

[RequireComponent(typeof(CarController))]
public class CarAgent : Agent
{
    [Header("References")]
    [SerializeField] CheckpointManager checkpointManager;
    [SerializeField] Transform spawnPoint;

    [Header("Raycasts")]
    [SerializeField] float rayLength = 20f;
    [SerializeField] float[] rayAngles = { -60f, -30f, -15f, 0f, 15f, 30f, 60f };
    [SerializeField] LayerMask wallMask = ~0;
    [SerializeField] float rayOriginHeight = 0.5f;

    [Header("Rewards")]
    [SerializeField] float checkpointReward = 1.0f;
    [SerializeField] float lapCompleteReward = 5.0f;
    [SerializeField] float wallHitPenalty = -0.5f;
    [SerializeField] float timePenaltyPerStep = -0.001f;
    [SerializeField] float speedRewardScale = 0.0005f;
    [SerializeField] float facingRewardScale = 0.001f;

    CarController car;
    int nextCheckpointIndex;
    int lapsCompleted;
    int totalCheckpointsReached;

    public int NextCheckpointIndex => nextCheckpointIndex;
    public int LapsCompleted => lapsCompleted;

    public override void Initialize()
    {
        car = GetComponent<CarController>();
    }

    /// <summary>
    /// Called by TrainingManager to wire up references at runtime.
    /// </summary>
    public void Setup(CheckpointManager cpManager, Transform spawn)
    {
        checkpointManager = cpManager;
        spawnPoint = spawn;
    }

    public override void OnEpisodeBegin()
    {
        // Reset car to spawn position
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rot = spawnPoint != null ? spawnPoint.rotation : transform.rotation;
        car.ResetCar(pos, rot);

        nextCheckpointIndex = 0;
        lapsCompleted = 0;
        totalCheckpointsReached = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. Raycast distances (7 values) - normalized 0..1
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        foreach (float angle in rayAngles)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, rayLength, wallMask))
            {
                sensor.AddObservation(hit.distance / rayLength);
            }
            else
            {
                sensor.AddObservation(1f);
            }
        }

        // 2. Car speed (normalized) - 1 value
        sensor.AddObservation(car.NormalizedSpeed);

        // 3. Current steering input - 1 value
        sensor.AddObservation(car.SteerInput);

        // 4. Direction to next checkpoint (local space) - 2 values
        if (checkpointManager != null && checkpointManager.TotalCheckpoints > 0)
        {
            Vector3 cpPos = checkpointManager.GetCheckpointPosition(nextCheckpointIndex);
            Vector3 localDir = transform.InverseTransformPoint(cpPos).normalized;
            sensor.AddObservation(localDir.x); // left/right
            sensor.AddObservation(localDir.z); // forward/back
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
        }

        // 5. Dot product of car forward vs checkpoint direction (alignment) - 1 value
        if (checkpointManager != null && checkpointManager.TotalCheckpoints > 0)
        {
            Vector3 toCheckpoint = (checkpointManager.GetCheckpointPosition(nextCheckpointIndex) - transform.position).normalized;
            float dot = Vector3.Dot(transform.forward, toCheckpoint);
            sensor.AddObservation(dot);
        }
        else
        {
            sensor.AddObservation(1f);
        }

        // Total observations: 7 + 1 + 1 + 2 + 1 = 12
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Continuous actions: [0] = steering (-1..1), [1] = throttle (0..1)
        float steer = actions.ContinuousActions[0];
        float throttle = Mathf.Clamp01((actions.ContinuousActions[1] + 1f) * 0.5f); // map -1..1 to 0..1

        car.SetSteerInput(steer);
        car.SetThrottle(throttle);

        // Small time penalty to encourage finishing quickly
        AddReward(timePenaltyPerStep);

        // Reward for speed toward next checkpoint
        if (checkpointManager != null && checkpointManager.TotalCheckpoints > 0)
        {
            Vector3 toCheckpoint = (checkpointManager.GetCheckpointPosition(nextCheckpointIndex) - transform.position).normalized;
            float speedTowardCP = Vector3.Dot(car.Velocity, toCheckpoint);
            AddReward(speedTowardCP * speedRewardScale);

            // Reward for facing the next checkpoint
            float facing = Vector3.Dot(transform.forward, toCheckpoint);
            AddReward(facing * facingRewardScale);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;

        // Keyboard fallback for testing
        float steer = 0f;
        float throttle = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) steer = -1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) steer = 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) throttle = 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) throttle = -1f;

        continuous[0] = steer;
        continuous[1] = throttle;
    }

    public void OnCheckpointReached(int checkpointIndex)
    {
        if (checkpointIndex != nextCheckpointIndex) return;

        AddReward(checkpointReward);
        totalCheckpointsReached++;

        nextCheckpointIndex = (nextCheckpointIndex + 1) % checkpointManager.TotalCheckpoints;

        // Completed a lap
        if (nextCheckpointIndex == 0)
        {
            lapsCompleted++;
            AddReward(lapCompleteReward);
            EndEpisode();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        // Penalize wall hits
        if (((1 << collision.gameObject.layer) & wallMask) != 0)
        {
            AddReward(wallHitPenalty);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize raycasts in editor
        Vector3 origin = transform.position + Vector3.up * rayOriginHeight;
        foreach (float angle in rayAngles)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(origin, dir * rayLength);
        }

        // Draw line to next checkpoint
        if (checkpointManager != null && checkpointManager.TotalCheckpoints > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, checkpointManager.GetCheckpointPosition(nextCheckpointIndex));
        }
    }
}
