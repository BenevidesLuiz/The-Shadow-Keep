using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class SaveSlotUI : MonoBehaviour {

    [Header("Qual é este Slot?")]
    [Tooltip("Digite 1, 2 ou 3")]
    public int slotNumber;

    [Header("Textos Visuais")]
    [SerializeField] private TextMeshProUGUI textoNome;   // 👈 Agora puxamos o Nome!
    [SerializeField] private TextMeshProUGUI textoClasse;
    [SerializeField] private TextMeshProUGUI textoLevel;

    [Header("Ícone do Save")]
    [SerializeField] private Image iconeNoSlot;
    [SerializeField] private Sprite fotoGuerreiro;
    [SerializeField] private Sprite fotoPaladino;
    [SerializeField] private Sprite fotoVazio;

    private Button meubotao;
    private bool temSave;
    private PlayerData dadosDoSave;

    private void Awake() {
        meubotao = GetComponent<Button>();
        meubotao.onClick.AddListener(AoClicarNoSlot);
    }

    private void OnEnable() {
        AtualizarVisualDoSlot();
    }

    private void AtualizarVisualDoSlot() {
        dadosDoSave = SaveSystem.LoadPlayer(slotNumber);
        temSave = (dadosDoSave != null);

        if (temSave) {
            if (textoNome) textoNome.text = string.IsNullOrEmpty(dadosDoSave.playerName) ? "Sem Nome" : dadosDoSave.playerName;

            if (textoClasse) textoClasse.text = "Classe: " + dadosDoSave.characterClass.ToString();
            if (textoLevel) textoLevel.text = "Level: " + dadosDoSave.level.ToString();

            if (iconeNoSlot != null) {
                iconeNoSlot.gameObject.SetActive(true);
                string nomeDaClasse = dadosDoSave.characterClass.ToString();

                if (nomeDaClasse == "Guerreiro") iconeNoSlot.sprite = fotoGuerreiro;
                else if (nomeDaClasse == "Paladino") iconeNoSlot.sprite = fotoPaladino;
            }
        }
        else {
            if (textoNome) textoNome.text = "Slot Vazio"; 
            if (textoClasse) textoClasse.text = "";
            if (textoLevel) textoLevel.text = "";

            if (iconeNoSlot != null) {
                if (fotoVazio != null) iconeNoSlot.sprite = fotoVazio;
                else iconeNoSlot.gameObject.SetActive(false);
            }
        }
    }

    private void AoClicarNoSlot() {
        if (temSave) {
            Debug.Log($"[SaveSlotUI] Carregando o jogo do Slot {slotNumber}...");
            SaveSystem.CurrentSlot = slotNumber;
            string cenaSalva = string.IsNullOrEmpty(dadosDoSave.currentScene) ? "Fase1" : dadosDoSave.currentScene;
            GameManager.Instance.GoToScene(cenaSalva, loadSave: true);
        }
    }
}