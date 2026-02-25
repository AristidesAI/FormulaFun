using UnityEngine;
using TMPro;

public class RaceTimer : MonoBehaviour
{
    [SerializeField] TMP_Text timerText;

    float elapsedTime;
    bool isRunning;

    public float ElapsedTime => elapsedTime;
    public bool IsRunning => isRunning;

    void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChange;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChange;
    }

    void HandleStateChange(GameManager.GameState state)
    {
        switch (state)
        {
            case GameManager.GameState.Racing:
                isRunning = true;
                break;
            case GameManager.GameState.Paused:
            case GameManager.GameState.Finished:
            case GameManager.GameState.MainMenu:
                isRunning = false;
                break;
        }
    }

    void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        int milliseconds = Mathf.FloorToInt((elapsedTime * 1000f) % 1000f);

        timerText.text = string.Format("{0:00}:{1:00}.{2:000}", minutes, seconds, milliseconds);
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateDisplay();
    }
}
