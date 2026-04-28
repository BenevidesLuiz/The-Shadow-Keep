using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public PlayerData pendingLoad;
    public bool shouldLoad = false;
    public string targetScene;
    public string currentScene;  

    private void Awake() {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void GoToScene(string sceneName, bool loadSave = false) {
        targetScene = sceneName;
        shouldLoad = loadSave;

        if (loadSave && SaveSystem.HasSave(SaveSystem.CurrentSlot))
            pendingLoad = SaveSystem.LoadPlayer();

        SceneManager.LoadScene("Loading");
    }

    public void GoToSceneInstant(string sceneName) {
        SceneManager.LoadScene(sceneName);
    }
}