using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { MainMenu, Racing, Paused, Finished }

    [SerializeField] GameState currentState = GameState.MainMenu;

    public GameState CurrentState => currentState;

    public event System.Action<GameState> OnStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void StartRace()
    {
        SetState(GameState.Racing);
        Time.timeScale = 1f;
    }

    public void PauseGame()
    {
        if (currentState != GameState.Racing) return;
        SetState(GameState.Paused);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Paused) return;
        SetState(GameState.Racing);
        Time.timeScale = 1f;
    }

    public void RestartRace()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SetState(GameState.MainMenu);
        // Load main menu scene if separate, or toggle UI panels
        SceneManager.LoadScene("MainMenu");
    }

    public void FinishRace()
    {
        SetState(GameState.Finished);
    }

    void SetState(GameState newState)
    {
        currentState = newState;
        OnStateChanged?.Invoke(newState);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
