using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RaceHUD : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] TMP_Text speedText;
    [SerializeField] Button pauseButton;

    [Header("Pause Panel")]
    [SerializeField] GameObject pausePanel;
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button mainMenuButton;

    [Header("References")]
    [SerializeField] CarController carController;

    void Start()
    {
        if (pauseButton != null)
            pauseButton.onClick.AddListener(OnPauseClicked);
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChange;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChange;
    }

    void HandleStateChange(GameManager.GameState state)
    {
        if (pausePanel != null)
            pausePanel.SetActive(state == GameManager.GameState.Paused);
    }

    void Update()
    {
        if (carController != null && speedText != null)
        {
            speedText.text = Mathf.RoundToInt(carController.CurrentSpeedKmh) + " km/h";
        }
    }

    void OnPauseClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();
    }

    void OnResumeClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }

    void OnRestartClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.RestartRace();
    }

    void OnMainMenuClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.GoToMainMenu();
    }

    public void SetCarController(CarController cc) => carController = cc;
}
