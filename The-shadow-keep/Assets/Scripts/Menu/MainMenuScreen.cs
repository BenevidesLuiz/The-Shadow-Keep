using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuScreen : MonoBehaviour {

    [Header("Botões Principais")]
    [SerializeField] private Button btnNewGame;
    [SerializeField] private Button btnContinue; // Carrega o último save automático
    [SerializeField] private Button btnLoad;     // Abre a lista de saves
    [SerializeField] private Button btnExit;

    [Header("Objetos de UI")]
    [SerializeField] private GameObject continueButtonObject;
    [SerializeField] private GameObject loadButtonObject; // Esconde se não tiver save

    [Header("Painéis")]
    [SerializeField] private GameObject painelLoadGame; // O painel dos Slots (NOVO)
    [SerializeField] private Button btnFecharPainelLoad; // Botão de fechar o painel (NOVO)

    private void Start() {
        // 1. Descobre se existe algum save e guarda nessa variável
        bool temSave = SaveSystem.HasAnySave();

        // 2. Esconde ou mostra os botões baseados nisso
        if (continueButtonObject != null) continueButtonObject.SetActive(temSave);
        if (loadButtonObject != null) loadButtonObject.SetActive(temSave);

        // 3. Garante que o painel de saves comece escondido
        if (painelLoadGame != null) painelLoadGame.SetActive(false);

        // 4. Liga todos os botões (isso DEVE rodar mesmo se não tiver save!)
        if (btnNewGame != null) btnNewGame.onClick.AddListener(OnNewGameClick);
        if (btnContinue != null) btnContinue.onClick.AddListener(OnContinueClick);
        if (btnLoad != null) btnLoad.onClick.AddListener(AbrirPainelLoad);
        if (btnExit != null) btnExit.onClick.AddListener(OnExitClick);
        if (btnFecharPainelLoad != null) btnFecharPainelLoad.onClick.AddListener(FecharPainelLoad);
    }

    private void OnNewGameClick() {
        if (GameManager.Instance != null) {
            GameManager.Instance.GoToScene("EscolhaPersonagem", loadSave: false);
        }
        else {
            // Se ele não existir por falta de sincronia de milissegundos, nós procuramos ele na marra!
            GameManager gm = Object.FindFirstObjectByType<GameManager>();

            if (gm != null) {
                // Achou! Força ele a virar a Instância e carrega a cena
                gm.GoToScene("EscolhaPersonagem", loadSave: false);
            }
            else {
                // Se mesmo assim não achar, o Unity vai te avisar no console exatamente o que houve
                Debug.LogError("[Menu] Erro Crítico: O objeto do GameManager não está na cena do Menu!");
            }
        }
    }

    // CONTINUAR (Versão Segura)
    private void OnContinueClick() {
        if (!SaveSystem.HasAnySave()) return;

        PlayerData data = SaveSystem.LoadPlayer();
        string cenaSalva = (data != null && !string.IsNullOrEmpty(data.currentScene))
                           ? data.currentScene
                           : "Fase1";

        if (GameManager.Instance != null) {
            GameManager.Instance.GoToScene(cenaSalva, loadSave: true);
        }
        else {
            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm != null) {
                gm.GoToScene(cenaSalva, loadSave: true);
            }
        }
    }

    // CARREGAR JOGO: Abre a interface para escolher o Slot
    private void AbrirPainelLoad() {
        if (painelLoadGame != null) painelLoadGame.SetActive(true);
    }

    private void FecharPainelLoad() {
        if (painelLoadGame != null) painelLoadGame.SetActive(false);
    }

    private void OnExitClick() {
        Application.Quit();
        Debug.Log("[Menu] Saindo do jogo.");
    }
}