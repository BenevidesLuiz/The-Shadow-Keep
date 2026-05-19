using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// InputManager CORRIGIDO v2
/// - Reseta estado quando volta ao menu
/// - Previne que input fique bloqueado eternamente
/// - Funciona com scene loading
/// </summary>
public class InputManager : MonoBehaviour {
    public static InputManager Instance { get; private set; }

    private bool inputEnabled = true;
    private int disabledCount = 0;

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() {
        // ✅ NOVO: Reseta quando menu carrega
        if (SceneManager.GetActiveScene().name.Contains("Menu") ||
            SceneManager.GetActiveScene().name == "MenuPrincipal") {
            ResetInputState();
        }
    }

    public void DisableInput(string reason = "Unknown") {
        disabledCount++;
        inputEnabled = false;
        Debug.Log($"[InputManager] Input desabilitado. Razão: {reason} (Total: {disabledCount})");
    }

    public void EnableInput(string reason = "Unknown") {
        if (disabledCount > 0) {
            disabledCount--;
        }
        inputEnabled = (disabledCount == 0);
        Debug.Log($"[InputManager] Input habilitado. Razão: {reason} (Total: {disabledCount})");
    }

    public bool IsInputEnabled() {
        return inputEnabled;
    }

    public void ResetInputState() {
        disabledCount = 0;
        inputEnabled = true;
        Debug.Log("[InputManager] Estado de input resetado para HABILITADO");
    }
}