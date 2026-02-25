using System.Collections.Generic;
using UnityEngine;

public class TrainingManager : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] TrackGenerator trackGenerator;
    [SerializeField] GameObject carAgentPrefab;
    [SerializeField] int agentCount = 4;
    [SerializeField] bool randomizeTrackPerEpisode = true;

    List<CarAgent> agents = new();

    void Start()
    {
        BuildTrainingEnvironment();
    }

    void BuildTrainingEnvironment()
    {
        // Generate the track
        trackGenerator.GenerateTrack();

        // Spawn agents at start positions
        var spawns = trackGenerator.SpawnPoints;
        var cpManager = trackGenerator.CheckpointMgr;

        for (int i = 0; i < Mathf.Min(agentCount, spawns.Count); i++)
        {
            GameObject agentObj = Instantiate(carAgentPrefab, spawns[i].position, spawns[i].rotation, transform);
            agentObj.name = $"Agent_{i}";

            var agent = agentObj.GetComponent<CarAgent>();
            if (agent != null)
            {
                // Wire up checkpoint manager via serialized field reflection workaround:
                // The agent needs its checkpoint manager and spawn point set.
                // We do this via a setup method since we can't set SerializeField at runtime.
                agent.Setup(cpManager, spawns[i]);
                agents.Add(agent);
            }
        }
    }

    public void ResetEnvironment()
    {
        // Destroy existing agents
        foreach (var agent in agents)
        {
            if (agent != null)
                Destroy(agent.gameObject);
        }
        agents.Clear();

        // Optionally regenerate track
        if (randomizeTrackPerEpisode)
        {
            trackGenerator.ClearTrack();
            trackGenerator.GenerateTrack();
        }

        // Respawn agents
        var spawns = trackGenerator.SpawnPoints;
        var cpManager = trackGenerator.CheckpointMgr;

        for (int i = 0; i < Mathf.Min(agentCount, spawns.Count); i++)
        {
            GameObject agentObj = Instantiate(carAgentPrefab, spawns[i].position, spawns[i].rotation, transform);
            agentObj.name = $"Agent_{i}";

            var agent = agentObj.GetComponent<CarAgent>();
            if (agent != null)
            {
                agent.Setup(cpManager, spawns[i]);
                agents.Add(agent);
            }
        }
    }
}
