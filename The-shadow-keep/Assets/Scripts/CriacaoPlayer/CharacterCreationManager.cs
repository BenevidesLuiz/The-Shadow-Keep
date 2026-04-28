using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        // 1. Checa a classe
        if (selectedIndex == -1) {
            MostrarErroNaTela("Por favor, selecione uma classe antes de continuar!");
            return;
        }

        // 2. Checa o nome
        if (inputNomeJogador != null && string.IsNullOrWhiteSpace(inputNomeJogador.text)) {
            MostrarErroNaTela("O seu personagem precisa de um nome!");
            return;
        }

        // Se estiver tudo certo, garante que não tem mensagem de erro e abre o painel
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
    //  CONFIRMAÇÃO E CRIAÇÃO DO PLAYER
    // ====================================================================

    private void ConfirmSelection() {
        ClassPreset selected = classes[selectedIndex];

        if (painelAviso != null) painelAviso.SetActive(false);

        Debug.Log($"[CharacterCreationManager] Confirmando: {selected.className}");

        GameObject playerObj = Instantiate(selected.prefab);
        DontDestroyOnLoad(playerObj);

        PlayerStats stats = playerObj.GetComponent<PlayerStats>();
        if (stats == null) {
            Debug.LogError("[CharacterCreationManager] PlayerStats não encontrado no prefab!");
            return;
        }

        stats.SetStatsFromMenuOrSave(
            selected.classType,
            selected.level,
            selected.strength,
            selected.bladeSharpness,
            selected.GetMaxHealthFromVitality(),
            selected.GetMaxStaminaFromStamina(),
            selected.faith
        );

        Debug.Log($"[CharacterCreationManager] Stats passados: Level {selected.level}, Força {selected.strength}, Fé {selected.faith}");

        if (GameManager.Instance != null) {
            GameManager.Instance.GoToScene("Fase1", false);
        }
        else {
            Debug.LogError("[CharacterCreationManager] GameManager não encontrado!");
        }
    }
}