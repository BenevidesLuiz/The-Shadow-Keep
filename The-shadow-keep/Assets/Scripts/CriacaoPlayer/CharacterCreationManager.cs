using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static PlayerStats;
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
    [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0f, 1f); // Dourado
    [SerializeField] private Color deselectedColor = Color.white;

    [Header("Painel de Confirmação")]
    [SerializeField] private GameObject painelAviso;
    [SerializeField] private Button botaoSim;
    [SerializeField] private Button botaoCancelar;

    [Header("Avisos na Tela")]
    [SerializeField] private TextMeshProUGUI textoAvisoErro;

    private int selectedIndex = -1;

    private void Start() {
        // Configura botões de seleção de classe
        for (int i = 0; i < classSelectButtons.Length; i++) {
            int index = i;
            classSelectButtons[i].onClick.AddListener(() => SelectClass(index));
        }

        if (acceptButton != null) acceptButton.onClick.AddListener(AbrirPainelAviso);

        if (botaoSim != null) botaoSim.onClick.AddListener(ConfirmSelection); // O Sim  criação do player

        if (botaoCancelar != null) botaoCancelar.onClick.AddListener(FecharPainelAviso);

        if (painelAviso != null) painelAviso.SetActive(false);
    }



    private void AbrirPainelAviso() {
        // 1. Checa se o jogador selecionou uma classe
        if (selectedIndex == -1) {
            MostrarErroNaTela("Por favor, selecione uma classe antes de continuar!");
            return;
        }

        // 2. Checa de forma rigorosa se o componente existe E se o texto digitado é válido
        if (inputNomeJogador == null) {
            Debug.LogError("[CharacterCreationManager] ERRO: O campo 'inputNomeJogador' não foi arrastado no Inspector!");
            MostrarErroNaTela("Erro técnico: Campo de nome não configurado no painel.");
            return;
        }

        // Valida se o jogador digitou apenas espaços ou deixou vazio
        if (string.IsNullOrWhiteSpace(inputNomeJogador.text)) {
            MostrarErroNaTela("O seu personagem precisa de um nome válido!");
            return;
        }

        // Se passar em todas as validações, limpa mensagens de erro e abre o painel de confirmação
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
        yield return new WaitForSeconds(3f); // Espera 3 segundos
        if (textoAvisoErro != null) {
            textoAvisoErro.text = ""; // Apaga o texto
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

        // Atualiza nome e ícone
        if (classNameDisplay) classNameDisplay.text = c.className;
        if (classIconDisplay && c.icon) classIconDisplay.sprite = c.icon;

        if (classDescriptionDisplay) classDescriptionDisplay.text = c.description;

        // Atualiza todos os valores na UI (puxando do ClassPreset)
        if (levelValue) levelValue.text = c.level.ToString();
        if (strengthValue) strengthValue.text = c.strength.ToString();
        if (sharpnessValue) sharpnessValue.text = c.bladeSharpness.ToString("F0");
        if (vitalityValue) vitalityValue.text = c.vitality.ToString();
        if (staminaValue) staminaValue.text = c.stamina.ToString();
        if (faithValue) faithValue.text = c.faith.ToString();

        for (int i = 0; i < classSelectButtons.Length; i++) {
            Image buttonImage = classSelectButtons[i].GetComponent<Image>();
            if (buttonImage != null) {
                if (i == index) {
                    buttonImage.color = selectedColor;
                }
                else {
                    buttonImage.color = deselectedColor;
                }
            }
        }
    }

    // ====================================================================
    //  CONFIRMAÇÃO E CRIAÇÃO DO PLAYER (Versão sem Prefabs)
    // ====================================================================

    private void ConfirmSelection() {
        ClassPreset selected = classes[selectedIndex];

        if (painelAviso != null) painelAviso.SetActive(false);

        int slotDisponivel = -1;

        for (int i = 1; i <= 3; i++) {
            if (!SaveSystem.HasSave(i)) {
                slotDisponivel = i; // Encontrou o primeiro slot livre (1, 2 ou 3)
                break;
            }
        }

        // Se todos os 3 slots estiverem cheios, avisa o jogador e não deixa prosseguir
        if (slotDisponivel == -1) {
            MostrarErroNaTela("Todos os slots estão cheios! Exclua um save no menu anterior antes de começar.");
            return;
        }

        // Define o slot descoberto como o atual onde o jogo será gravado
        SaveSystem.CurrentSlot = slotDisponivel;
        Debug.Log($"[CharacterCreationManager] Slot livre encontrado! Salvando automaticamente no Slot: {slotDisponivel}");

        if (GameManager.Instance != null) {
            if (GameManager.Instance.pendingLoad == null) {
                GameManager.Instance.pendingLoad = new PlayerData();
            }

            // Atribui os dados do novo personagem
            GameManager.Instance.pendingLoad.playerName = inputNomeJogador != null ? inputNomeJogador.text : "Recruta";

            if (selected.classType == PlayerStats.CharacterClass.Warrior) {
                GameManager.Instance.pendingLoad.characterClass = CharacterClass.Warrior;
            }
            else {
                GameManager.Instance.pendingLoad.characterClass = CharacterClass.Paladin;
            }

            GameManager.Instance.pendingLoad.level = selected.level;
            GameManager.Instance.pendingLoad.strength = selected.strength;
            GameManager.Instance.pendingLoad.bladeSharpness = selected.bladeSharpness;
            GameManager.Instance.pendingLoad.currentHealth = selected.GetMaxHealthFromVitality();
            GameManager.Instance.pendingLoad.currentStamina = selected.GetMaxStaminaFromStamina();
            GameManager.Instance.pendingLoad.faith = selected.faith;

            // Inicia o jogo na Fase 1
            GameManager.Instance.GoToScene("Fase1", loadSave: false);
        }
    }

}