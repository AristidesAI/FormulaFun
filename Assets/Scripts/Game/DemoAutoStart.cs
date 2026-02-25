using UnityEngine;

public class DemoAutoStart : MonoBehaviour
{
    void Start()
    {
        // Auto-start the race in demo scene
        if (GameManager.Instance != null)
            GameManager.Instance.StartRace();
    }
}
