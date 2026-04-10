using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuScreen : MonoBehaviour {

    [Header("Botões")]
    [SerializeField] private Button btnPlay;
    [SerializeField] private Button btnLoad;
    [SerializeField] private Button btnExit;

    [Header("Continue (só aparece se tiver save)")]
    [SerializeField] private GameObject continueButton;

    private void Start() {
        // Mostra o botão Continue só se tiver save
        if (continueButton != null)
            continueButton.SetActive(SaveSystem.HasSave());

        btnPlay.onClick.AddListener(OnPlayClick);
        btnLoad.onClick.AddListener(OnLoadClick);
        btnExit.onClick.AddListener(OnExitClick);
    }

    // Novo jogo — ignora save
    private void OnPlayClick() {
        GameManager.Instance.GoToScene("Fase1", loadSave: false);
    }

    // Continua do save
    private void OnLoadClick() {
        if (!SaveSystem.HasSave()) {
            Debug.Log("[Menu] Nenhum save encontrado.");
            return;
        }
        GameManager.Instance.GoToScene("Fase1", loadSave: true);
    }

    private void OnExitClick() {
        Application.Quit();
        Debug.Log("[Menu] Saindo do jogo.");
    }
}