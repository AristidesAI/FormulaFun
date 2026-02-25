using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int Index { get; set; }
    public CheckpointManager Manager { get; set; }

    void Awake()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponentInParent<CarAgent>();
        if (agent != null)
        {
            Manager.CarReachedCheckpoint(agent, this);
        }
    }
}
