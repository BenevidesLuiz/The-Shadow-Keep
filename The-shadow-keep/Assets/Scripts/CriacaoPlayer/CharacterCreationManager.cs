using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PlayerStats;

/// <summary>
/// CharacterCreationManager CORRIGIDO
/// - Garante que shouldLoad é FALSE antes de criar novo personagem
/// - Sincroniza melhor com GameManager
/// - Validações mais robustas
/// </summary>
public class CharacterCreationManager : MonoBehaviour {

    [System.Serializable]
    public class ClassPreset {
        public string className;
        [TextArea(3, 5)] public string description;
        public PlayerStats.CharacterClass classType;

        [Header("Atributos Iniciais")]
        public int level = 1;
        public int strength;
        public float bladeSharpness = 100f;
        public int vitality;
        public int stamina;
        public int faith;

        public Sprite icon;
        public GameObject prefab;

        public float GetMaxHealthFromVitality() {
            return 100f + (vitality - 10f) * 5f;
        }

        public float GetMaxStaminaFromStamina() {
            return 60f + (stamina - 10f) * 3f;
        }
    }

    [Header("Configuração de Classes")]
    [SerializeField] private ClassPreset[] classes;

    [Header("Nome do Jogador")]
    [SerializeField] private TMP_InputField inputNomeJogador;

    [Header("UI — Display Central")]
    [SerializeField] private TextMeshProUGUI classNameDisplay;
    [SerializeField] private Image classIconDisplay;
    [SerializeField] private TextMeshProUGUI classDescriptionDisplay;

    [Header("UI — Atributos (Textos)")]
    [SerializeField] private TextMeshProUGUI levelValue;
    [SerializeField] private TextMeshProUGUI strengthValue;
    [SerializeField] private TextMeshProUGUI sharpnessValue;
    [SerializeField] private TextMeshProUGUI vitalityValue;
    [SerializeField] private TextMeshProUGUI staminaValue;
    [SerializeField] private TextMeshProUGUI faithValue;

    [Header("UI — Botões")]
    [SerializeField] private Button[] classSelectButtons;
    [SerializeField] private Button acceptButton;

    [Header("Cor de Seleção")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0f, 1f);
    [SerializeField] private Color deselectedColor = Color.white;

    [Header("Painel de Confirmação")]
    [SerializeField] private GameObject painelAviso;
    [SerializeField] private Button botaoSim;
    [SerializeField] private Button botaoCancelar;

    [Header("Avisos na Tela")]
    [SerializeField] private TextMeshProUGUI textoAvisoErro;

    private int selectedIndex = -1;

    private void Start() {
        if (GameManager.Instance != null) {
            GameManager.Instance.shouldLoad = false;
            GameManager.Instance.pendingLoad = null;
            Debug.Log("[CharacterCreationManager] Estado resetado ao entrar na tela de criação");
        }

        // Configura botões de seleção de classe
        for (int i = 0; i < classSelectButtons.Length; i++) {
            int index = i;
            classSelectButtons[i].onClick.AddListener(() => SelectClass(index));
        }

        if (acceptButton != null) acceptButton.onClick.AddListener(AbrirPainelAviso);
        if (botaoSim != null) botaoSim.onClick.AddListener(ConfirmSelection);
        if (botaoCancelar != null) botaoCancelar.onClick.AddListener(FecharPainelAviso);

        if (painelAviso != null) painelAviso.SetActive(false);
    }

    private void AbrirPainelAviso() {
        // Valida seleção de classe
        if (selectedIndex == -1) {
            MostrarErroNaTela("Por favor, selecione uma classe antes de continuar!");
            return;
        }

        // Valida nome do jogador
        if (inputNomeJogador == null) {
            Debug.LogError("[CharacterCreationManager] Campo 'inputNomeJogador' não configurado!");
            MostrarErroNaTela("Erro técnico: Campo de nome não configurado.");
            return;
        }

        if (string.IsNullOrWhiteSpace(inputNomeJogador.text)) {
            MostrarErroNaTela("O seu personagem precisa de um nome válido!");
            return;
        }

        // Se tudo OK, abre painel de confirmação
        if (textoAvisoErro != null) textoAvisoErro.text = "";
        if (painelAviso != null) painelAviso.SetActive(true);
    }

    private void MostrarErroNaTela(string mensagem) {
        if (textoAvisoErro != null) {
            textoAvisoErro.text = mensagem;
            StopAllCoroutines();
            StartCoroutine(ApagarAvisoDepoisDeTempo());
        }
    }

    private System.Collections.IEnumerator ApagarAvisoDepoisDeTempo() {
        yield return new WaitForSeconds(3f);
        if (textoAvisoErro != null) {
            textoAvisoErro.text = "";
        }
    }

    private void FecharPainelAviso() {
        if (painelAviso != null) painelAviso.SetActive(false);
    }

    private void SelectClass(int index) {
        if (index < 0 || index >= classes.Length) return;

        selectedIndex = index;
        ClassPreset c = classes[index];

        Debug.Log($"[CharacterCreationManager] Selecionado: {c.className}");

        // Atualiza UI
        if (classNameDisplay) classNameDisplay.text = c.className;
        if (classIconDisplay && c.icon) classIconDisplay.sprite = c.icon;
        if (classDescriptionDisplay) classDescriptionDisplay.text = c.description;

        if (levelValue) levelValue.text = c.level.ToString();
        if (strengthValue) strengthValue.text = c.strength.ToString();
        if (sharpnessValue) sharpnessValue.text = c.bladeSharpness.ToString("F0");
        if (vitalityValue) vitalityValue.text = c.vitality.ToString();
        if (staminaValue) staminaValue.text = c.stamina.ToString();
        if (faithValue) faithValue.text = c.faith.ToString();

        // Atualiza cor dos botões
        for (int i = 0; i < classSelectButtons.Length; i++) {
            Image buttonImage = classSelectButtons[i].GetComponent<Image>();
            if (buttonImage != null) {
                buttonImage.color = (i == index) ? selectedColor : deselectedColor;
            }
        }
    }

    private void ConfirmSelection() {
        ClassPreset selected = classes[selectedIndex];

        if (painelAviso != null) painelAviso.SetActive(false);

        // Encontra um slot disponível
        int slotDisponivel = -1;
        for (int i = 1; i <= 3; i++) {
            if (!SaveSystem.HasSave(i)) {
                slotDisponivel = i;
                break;
            }
        }

        if (slotDisponivel == -1) {
            MostrarErroNaTela("Todos os slots estão cheios! Exclua um save antes de começar.");
            return;
        }

        SaveSystem.CurrentSlot = slotDisponivel;
        Debug.Log($"[CharacterCreationManager] Novo personagem salvo no Slot: {slotDisponivel}");

        GameManager gm = GameManager.Instance;
        if (gm == null) {
            gm = Object.FindFirstObjectByType<GameManager>();
        }

        if (gm != null) {
            if (gm.pendingLoad == null) {
                gm.pendingLoad = new PlayerData();
            }

            // Preenche dados do novo personagem
            gm.pendingLoad.playerName = inputNomeJogador != null ? inputNomeJogador.text : "Recruta";
            gm.pendingLoad.characterClass = selected.classType;
            gm.pendingLoad.level = selected.level;
            gm.pendingLoad.strength = selected.strength;
            gm.pendingLoad.bladeSharpness = selected.bladeSharpness;
            gm.pendingLoad.currentHealth = selected.GetMaxHealthFromVitality();
            gm.pendingLoad.currentStamina = selected.GetMaxStaminaFromStamina();
            gm.pendingLoad.faith = selected.faith;

            gm.shouldLoad = false;

            Debug.Log($"[CharacterCreationManager] Iniciando novo jogo com {selected.className}");
            gm.GoToScene("Fase1", loadSave: false);
        }
        else {
            Debug.LogError("[CharacterCreationManager] GameManager não encontrado na cena!");
            MostrarErroNaTela("Erro: GameManager não disponível.");
        }
    }
}