using System.Collections.Generic;
using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    [SerializeField] List<Checkpoint> checkpoints = new();

    public int TotalCheckpoints => checkpoints.Count;
    public IReadOnlyList<Checkpoint> Checkpoints => checkpoints;

    void Awake()
    {
        // Auto-discover checkpoints if not assigned
        if (checkpoints.Count == 0)
        {
            GetComponentsInChildren(checkpoints);
        }

        // Assign indices and manager reference
        for (int i = 0; i < checkpoints.Count; i++)
        {
            checkpoints[i].Index = i;
            checkpoints[i].Manager = this;
        }
    }

    public void CarReachedCheckpoint(CarAgent agent, Checkpoint cp)
    {
        agent.OnCheckpointReached(cp.Index);
    }

    public Vector3 GetCheckpointPosition(int index)
    {
        if (index < 0 || index >= checkpoints.Count) return Vector3.zero;
        return checkpoints[index].transform.position;
    }

    public Vector3 GetCheckpointForward(int index)
    {
        if (index < 0 || index >= checkpoints.Count) return Vector3.forward;
        return checkpoints[index].transform.forward;
    }

    public Checkpoint GetCheckpoint(int index)
    {
        if (index < 0 || index >= checkpoints.Count) return null;
        return checkpoints[index];
    }
}
