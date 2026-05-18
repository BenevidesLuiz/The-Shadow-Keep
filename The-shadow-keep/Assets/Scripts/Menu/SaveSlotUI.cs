using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class SaveSlotUI : MonoBehaviour {

    [Header("Qual é este Slot?")]
    [Tooltip("Digite 1, 2 ou 3")]
    public int slotNumber;

    [Header("Textos Visuais")]
    [SerializeField] private TextMeshProUGUI textoNome;
    [SerializeField] private TextMeshProUGUI textoClasse;
    [SerializeField] private TextMeshProUGUI textoLevel;

    [Header("Ícone do Save")]
    [SerializeField] private Image iconeNoSlot;
    [SerializeField] private Sprite fotoGuerreiro;
    [SerializeField] private Sprite fotoPaladino;
    [SerializeField] private Sprite fotoVazio;

    [Header("Botão de Deletar")]
    [SerializeField] private Button btnDeletarSave;

    private Button meubotao;
    private bool temSave;
    private PlayerData dadosDoSave;

    private void Awake() {
        meubotao = GetComponent<Button>();
        meubotao.onClick.AddListener(AoClicarNoSlot);

        // Vincula a função de excluir ao clique do botão de lixeira/X
        if (btnDeletarSave != null) {
            btnDeletarSave.onClick.AddListener(AoClicarEmDeletar);
        }
        
        TentarAutoConectarComponentes();
    }

    private void OnEnable() {
        AtualizarVisualDoSlot();
    }

    public void AtualizarVisualDoSlot() {
       
        dadosDoSave = SaveSystem.LoadPlayer(slotNumber);
        temSave = (dadosDoSave != null);

        if (temSave) {
            // Se o nome no arquivo estiver em branco, usa "Sem Nome"
            if (textoNome != null) {
                textoNome.text = string.IsNullOrEmpty(dadosDoSave.playerName) ? "Sem Nome" : dadosDoSave.playerName;
            }

            string nomeDaClasse = dadosDoSave.characterClass.ToString();
            if (textoClasse != null) textoClasse.text = "Classe: " + nomeDaClasse;
            if (textoLevel != null) textoLevel.text = "Level: " + dadosDoSave.level.ToString();

            if (iconeNoSlot != null) {
                iconeNoSlot.gameObject.SetActive(true);
                if (nomeDaClasse == "Guerreiro" || nomeDaClasse == "Warrior") iconeNoSlot.sprite = fotoGuerreiro;
                else if (nomeDaClasse == "Paladino" || nomeDaClasse == "Paladin") iconeNoSlot.sprite = fotoPaladino;
            }

            // Ativa o botão de excluir apenas se o slot estiver ocupado
            if (btnDeletarSave != null) btnDeletarSave.gameObject.SetActive(true);
        }
        else {
            // Se não houver save, limpa o visual e esconde o botão de exclusão
            if (textoNome != null) {
                textoNome.text = "Slot Vazio";
                textoNome.alignment = TextAlignmentOptions.Center;
            } 
            if (textoClasse != null) textoClasse.text = "";
            if (textoLevel != null) textoLevel.text = "";

            if (iconeNoSlot != null) {
                if (fotoVazio != null) iconeNoSlot.sprite = fotoVazio;
                else iconeNoSlot.gameObject.SetActive(false);
            }

            if (btnDeletarSave != null) btnDeletarSave.gameObject.SetActive(false);
        }
    }

    private void AoClicarNoSlot() {
        if (temSave) {
            Debug.Log($"[SaveSlotUI] Definindo slot ativo e carregando: {slotNumber}");
            SaveSystem.CurrentSlot = slotNumber;

            string cenaSalva = string.IsNullOrEmpty(dadosDoSave.currentScene) ? "Fase1" : dadosDoSave.currentScene;
            if (GameManager.Instance != null) {
                GameManager.Instance.GoToScene(cenaSalva, loadSave: true);
            }
        }
    }

    private void AoClicarEmDeletar() {
        if (temSave) {
            Debug.Log($"[SaveSlotUI] Excluindo dados do Slot {slotNumber}...");

            // Chama a exclusão do SaveSystem
            SaveSystem.DeleteSave(slotNumber);

            // Força a atualização do visual na mesma hora
            AtualizarVisualDoSlot();
        }
    }

    private void TentarAutoConectarComponentes() {
        // Busca os filhos pelo nome exato para evitar cruzar as referências no Inspector
        if (textoNome == null) {
            Transform busca = transform.Find("Nome");
            if (busca != null) textoNome = busca.GetComponent<TextMeshProUGUI>();
        }
        if (textoClasse == null) {
            Transform busca = transform.Find("Classe");
            if (busca != null) textoClasse = busca.GetComponent<TextMeshProUGUI>();
        }
        if (textoLevel == null) {
            Transform busca = transform.Find("Level");
            if (busca != null) textoLevel = busca.GetComponent<TextMeshProUGUI>();
        }
        if (iconeNoSlot == null) {
            Transform busca = transform.Find("IconeSave");
            if (busca != null) iconeNoSlot = busca.GetComponent<Image>();
        }
    }
}