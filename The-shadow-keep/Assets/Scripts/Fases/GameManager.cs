using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager CORRIGIDO para funcionar com LoadingScreen existente
/// 
/// Mudanças principais:
/// 1. Reseta shouldLoad quando volta ao menu
/// 2. Remove a lógica de LoadTargetScene() (seu LoadingScreen já faz isso)
/// 3. Sincroniza dados de save corretamente
/// </summary>
public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public PlayerData pendingLoad;
    public bool shouldLoad = false;
    public string targetScene;
    public string currentScene;

    private void Awake() {
        if (Instance != null) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Método principal para transição de cenas
    /// Usa a cena Loading como intermediária
    /// </summary>
    public void GoToScene(string sceneName, bool loadSave = false) {
        if (sceneName.Contains("Menu") || sceneName == "MenuPrincipal") {
            shouldLoad = false;
            pendingLoad = null;
            Debug.Log("[GameManager] Voltando ao menu. Estado resetado.");
        }
        else {
            if (!loadSave) {
                shouldLoad = false;
                Debug.Log("[GameManager] Novo jogo iniciado. shouldLoad = false");
            }
            else if (loadSave && SaveSystem.HasSave(SaveSystem.CurrentSlot)) {
                pendingLoad = SaveSystem.LoadPlayer();
                shouldLoad = true;
                Debug.Log("[GameManager] Carregando save do slot " + SaveSystem.CurrentSlot);
            }
        }

        targetScene = sceneName;

        SceneManager.LoadScene("Loading");
    }

    /// <summary>
    /// Pula a tela de loading (vai direto para a cena)
    /// Use apenas para menu e telas de UI
    /// </summary>
    public void GoToSceneInstant(string sceneName) {
        // Reseta o estado se for menu
        if (sceneName.Contains("Menu") || sceneName == "MenuPrincipal") {
            shouldLoad = false;
            pendingLoad = null;
            Debug.Log("[GameManager] Voltando ao menu instantaneamente. Estado resetado.");
        }
        SceneManager.LoadScene(sceneName);
    }
}