using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// BonfireUI CORRIGIDO v2
/// - Desabilita SCRIPTS do player (não só input)
/// - Carrega dados do player corretamente
/// - Sincroniza com InputManager
/// - Habilita tudo ao fechar
/// </summary>
public class BonfireUI : MonoBehaviour {

    [Header("Telas da Fogueira")]
    [SerializeField] private GameObject telaOpcoes;
    [SerializeField] private GameObject telaLevelUp;

    [Header("Painel de Confirmação (Sim/Não)")]
    [SerializeField] private GameObject painelConfirmacao;
    [SerializeField] private TextMeshProUGUI txtConfirmarOk;
    [SerializeField] private TextMeshProUGUI txtConfirmarCancelar;

    [Header("Referências do Jogador")]
    private PlayerStats playerStats;
    private PlayerBase playerBase;
    private GameObject playerGameObject;
    private MonoBehaviour[] playerScripts; // Para desabilitar todos os scripts

    [Header("Foto da Classe")]
    [SerializeField] private Image iconePlayer;
    [SerializeField] private Sprite fotoGuerreiro;
    [SerializeField] private Sprite fotoPaladino;

    [Header("Valores ATUAIS e ECONOMIA")]
    [SerializeField] private TextMeshProUGUI txtLevelAtual;
    [SerializeField] private TextMeshProUGUI txtSuasAlmas;
    [SerializeField] private TextMeshProUGUI txtAlmasNecessarias;
    [SerializeField] private TextMeshProUGUI txtForcaAtual;
    [SerializeField] private TextMeshProUGUI txtAfiacaoAtual;
    [SerializeField] private TextMeshProUGUI txtVitalidadeAtual;
    [SerializeField] private TextMeshProUGUI txtEstaminaAtual;
    [SerializeField] private TextMeshProUGUI txtFeAtual;

    [Header("Novos Valores (Coluna Direita)")]
    [SerializeField] private TextMeshProUGUI txtLevelNovo;
    [SerializeField] private TextMeshProUGUI txtForcaNova;
    [SerializeField] private TextMeshProUGUI txtAfiacaoNova;
    [SerializeField] private TextMeshProUGUI txtVitalidadeNova;
    [SerializeField] private TextMeshProUGUI txtEstaminaNova;
    [SerializeField] private TextMeshProUGUI txtFeNova;

    [Header("Navegação (Cursor)")]
    [Tooltip("Ordem: 0=Força, 1=Afiação, 2=Vitalidade, 3=Estamina, 4=Fé")]
    [SerializeField] private TextMeshProUGUI[] nomesAtributos;
    [SerializeField] private TextMeshProUGUI txtBotaoAceitar;
    [SerializeField] private TextMeshProUGUI txtBotaoVoltar;

    private int tempLevel, tempForca, tempVit, tempEst, tempFe;
    private float tempAfiacao;
    private int almasGastasTemporariamente;

    private int cursorIndex = 0;
    private bool isConfirmacaoAberta = false;
    private int confirmacaoIndex = 0;

    private void OnEnable() {
        Debug.Log("[BonfireUI] OnEnable chamado");

        // ✅ PASSO 1: Encontra o player
        playerGameObject = GameObject.FindGameObjectWithTag("Player");
        if (playerGameObject == null) {
            Debug.LogError("[BonfireUI] ERRO CRÍTICO: Player não encontrado com tag 'Player'!");
            return;
        }

        // ✅ PASSO 2: Carrega os componentes
        playerStats = playerGameObject.GetComponent<PlayerStats>();
        playerBase = playerGameObject.GetComponent<PlayerBase>();

        if (playerStats == null) {
            Debug.LogError("[BonfireUI] ERRO: PlayerStats não encontrado no Player!");
        }
        if (playerBase == null) {
            Debug.LogError("[BonfireUI] ERRO: PlayerBase não encontrado no Player!");
        }

        if (playerStats != null && playerBase != null) {
            Debug.Log("[BonfireUI] Player carregado com sucesso!");
            ConfigurarFotoDaClasse();
        }

        // ✅ PASSO 3: DESABILITA TODOS OS SCRIPTS DO PLAYER
        // Isso impede que ele se mova, ataque, etc
        playerScripts = playerGameObject.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in playerScripts) {
            // NÃO desabilita o PlayerStats (precisa dos dados)
            if (!(script is PlayerStats)) {
                script.enabled = false;
                Debug.Log($"[BonfireUI] Desabilitado: {script.GetType().Name}");
            }
        }

        // ✅ PASSO 4: Desabilita input do InputManager também
        if (InputManager.Instance != null) {
            InputManager.Instance.DisableInput("BonfireUI aberto");
        }

        // ✅ PASSO 5: Garante que as telas estão no estado correto
        if (telaOpcoes != null) telaOpcoes.SetActive(true);
        if (telaLevelUp != null) telaLevelUp.SetActive(false);
        if (painelConfirmacao != null) painelConfirmacao.SetActive(false);

        isConfirmacaoAberta = false;
        cursorIndex = 0;
    }

    private void OnDisable() {
        Debug.Log("[BonfireUI] OnDisable chamado - Reabilitando player");

        // ✅ REABILITA TODOS OS SCRIPTS DO PLAYER
        if (playerGameObject != null && playerScripts != null) {
            foreach (MonoBehaviour script in playerScripts) {
                if (script != null && !(script is PlayerStats)) {
                    script.enabled = true;
                    Debug.Log($"[BonfireUI] Reabilitado: {script.GetType().Name}");
                }
            }
        }

        // ✅ Habilita input do InputManager
        if (InputManager.Instance != null) {
            InputManager.Instance.EnableInput("BonfireUI fechado");
        }
    }

    private void Update() {
        if (playerStats == null) return;
        if (telaLevelUp == null || !telaLevelUp.activeSelf) return;

        if (isConfirmacaoAberta) {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ||
                Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {

                confirmacaoIndex = (confirmacaoIndex == 0) ? 1 : 0;
                AtualizarCoresConfirmacao();
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F)) {
                if (confirmacaoIndex == 0) ClicouEmOk();
                else ClicouEmCancelarConfirmacao();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) {
            cursorIndex--;
            if (cursorIndex < 0) cursorIndex = 6;
            AtualizarCoresDoCursor();
        }
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) {
            cursorIndex++;
            if (cursorIndex > 6) cursorIndex = 0;
            AtualizarCoresDoCursor();
        }

        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) {
            if (cursorIndex >= 0 && cursorIndex <= 4) TentarSubirAtributo(cursorIndex);
        }
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) {
            if (cursorIndex >= 0 && cursorIndex <= 4) TentarDiminuirAtributo(cursorIndex);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F)) {
            if (cursorIndex == 5) ClicouEmAceitarLevelUp();
            else if (cursorIndex == 6) ClicouEmVoltarLevelUp();
        }
    }

    public void ClicouEmAceitarLevelUp() {
        if (almasGastasTemporariamente > 0) {
            isConfirmacaoAberta = true;
            painelConfirmacao.SetActive(true);
            confirmacaoIndex = 0;
            AtualizarCoresConfirmacao();
        }
    }

    public void ClicouEmVoltarLevelUp() {
        telaLevelUp.SetActive(false);
        telaOpcoes.SetActive(true);
    }

    public void ClicouEmOk() {
        if (playerStats == null) {
            Debug.LogError("[BonfireUI] PlayerStats é null em ClicouEmOk!");
            return;
        }

        if (SoulManager.Instance != null) {
            SoulManager.Instance.SpendSouls(almasGastasTemporariamente);
        }

        playerStats.level = tempLevel;
        playerStats.strength = tempForca;
        playerStats.bladeSharpness = tempAfiacao;
        playerStats.vitality = tempVit;
        playerStats.stamina = tempEst;
        playerStats.faith = tempFe;

        playerStats.ApplyStatsToKnight();
        playerStats.UpdateMaxStats();

        if (playerBase != null) {
            SaveSystem.SavePlayer(playerBase, playerStats);
            Debug.Log("[BonfireUI] Player salvo com sucesso!");
        }

        ClicouEmCancelarConfirmacao();
        ResetarValoresTemporarios();
    }

    public void ClicouEmCancelarConfirmacao() {
        isConfirmacaoAberta = false;
        painelConfirmacao.SetActive(false);
        AtualizarCoresDoCursor();
    }

    private void AtualizarCoresConfirmacao() {
        Color corSelecionado;
        ColorUtility.TryParseHtmlString("#6B3A2A", out corSelecionado);

        if (txtConfirmarOk != null) txtConfirmarOk.color = Color.white;
        if (txtConfirmarCancelar != null) txtConfirmarCancelar.color = Color.white;

        if (confirmacaoIndex == 0 && txtConfirmarOk != null) txtConfirmarOk.color = corSelecionado;
        else if (confirmacaoIndex == 1 && txtConfirmarCancelar != null) txtConfirmarCancelar.color = corSelecionado;
    }

    private void AtualizarCoresDoCursor() {
        Color corSelecionado;
        ColorUtility.TryParseHtmlString("#6B3A2A", out corSelecionado);

        for (int i = 0; i < nomesAtributos.Length; i++) {
            if (nomesAtributos[i] != null) nomesAtributos[i].color = Color.white;
        }
        if (txtBotaoAceitar != null) txtBotaoAceitar.color = Color.white;
        if (txtBotaoVoltar != null) txtBotaoVoltar.color = Color.white;

        if (cursorIndex >= 0 && cursorIndex <= 4) {
            if (nomesAtributos[cursorIndex] != null) nomesAtributos[cursorIndex].color = corSelecionado;
        }
        else if (cursorIndex == 5 && txtBotaoAceitar != null) {
            txtBotaoAceitar.color = corSelecionado;
        }
        else if (cursorIndex == 6 && txtBotaoVoltar != null) {
            txtBotaoVoltar.color = corSelecionado;
        }
    }

    private void TentarSubirAtributo(int index) {
        if (playerStats == null) return;

        int custoDesteLevel = CalcularCustoDoLevel(tempLevel);
        int almasDisponiveis = (SoulManager.Instance != null) ? SoulManager.Instance.CurrentSouls : 0;

        if (almasDisponiveis - almasGastasTemporariamente >= custoDesteLevel) {
            almasGastasTemporariamente += custoDesteLevel;
            tempLevel++;

            if (index == 0) tempForca++;
            else if (index == 1) tempAfiacao++;
            else if (index == 2) tempVit++;
            else if (index == 3) tempEst++;
            else if (index == 4) tempFe++;

            AtualizarInterface();
        }
    }

    private void TentarDiminuirAtributo(int index) {
        if (playerStats == null) return;

        bool temPontoPraDevolver = false;

        if (index == 0 && tempForca > playerStats.strength) { tempForca--; temPontoPraDevolver = true; }
        else if (index == 1 && tempAfiacao > playerStats.bladeSharpness) { tempAfiacao--; temPontoPraDevolver = true; }
        else if (index == 2 && tempVit > playerStats.vitality) { tempVit--; temPontoPraDevolver = true; }
        else if (index == 3 && tempEst > playerStats.stamina) { tempEst--; temPontoPraDevolver = true; }
        else if (index == 4 && tempFe > playerStats.faith) { tempFe--; temPontoPraDevolver = true; }

        if (temPontoPraDevolver) {
            tempLevel--;
            almasGastasTemporariamente -= CalcularCustoDoLevel(tempLevel);
            AtualizarInterface();
        }
    }

    private void ResetarValoresTemporarios() {
        if (playerStats == null) return;

        tempLevel = playerStats.level;
        tempForca = playerStats.strength;
        tempAfiacao = playerStats.bladeSharpness;
        tempVit = playerStats.vitality;
        tempEst = playerStats.stamina;
        tempFe = playerStats.faith;
        almasGastasTemporariamente = 0;

        if (txtLevelAtual) txtLevelAtual.text = playerStats.level.ToString();
        if (txtForcaAtual) txtForcaAtual.text = playerStats.strength.ToString();
        if (txtAfiacaoAtual) txtAfiacaoAtual.text = playerStats.bladeSharpness.ToString("F0") + "%";
        if (txtVitalidadeAtual) txtVitalidadeAtual.text = playerStats.vitality.ToString();
        if (txtEstaminaAtual) txtEstaminaAtual.text = playerStats.stamina.ToString();
        if (txtFeAtual) txtFeAtual.text = playerStats.faith.ToString();

        AtualizarInterface();
    }

    private int CalcularCustoDoLevel(int nivelAtual) {
        float custoNivel1 = 300f;
        float aumentoPorLevel = 1.10f;
        return Mathf.RoundToInt(custoNivel1 * Mathf.Pow(aumentoPorLevel, nivelAtual - 1));
    }

    private void AtualizarInterface() {
        if (playerStats == null) return;

        int almasAtuais = (SoulManager.Instance != null) ? SoulManager.Instance.CurrentSouls : 0;
        int almasRestantes = almasAtuais - almasGastasTemporariamente;
        int custoDoProximoUP = CalcularCustoDoLevel(tempLevel);

        if (txtSuasAlmas) txtSuasAlmas.text = almasRestantes.ToString();

        if (txtAlmasNecessarias) {
            txtAlmasNecessarias.text = custoDoProximoUP.ToString();
            txtAlmasNecessarias.color = (almasRestantes < custoDoProximoUP) ? new Color(0.9f, 0.2f, 0.2f) : Color.white;
        }

        if (txtLevelNovo) txtLevelNovo.text = tempLevel.ToString();
        if (txtForcaNova) txtForcaNova.text = tempForca.ToString();
        if (txtAfiacaoNova) txtAfiacaoNova.text = tempAfiacao.ToString("F0") + "%";

        float futuroHP = playerStats.CalcularHPDarkSouls(tempVit);
        if (txtVitalidadeNova) txtVitalidadeNova.text = tempVit.ToString() + $" (HP: {futuroHP})";

        if (txtEstaminaNova) txtEstaminaNova.text = tempEst.ToString();
        if (txtFeNova) txtFeNova.text = tempFe.ToString();

        DestacarSeMaior(txtLevelNovo, tempLevel, playerStats.level);
        DestacarSeMaior(txtForcaNova, tempForca, playerStats.strength);
        DestacarSeMaiorFloat(txtAfiacaoNova, tempAfiacao, playerStats.bladeSharpness);
        DestacarSeMaior(txtVitalidadeNova, tempVit, playerStats.vitality);
        DestacarSeMaior(txtEstaminaNova, tempEst, playerStats.stamina);
        DestacarSeMaior(txtFeNova, tempFe, playerStats.faith);
    }

    private void DestacarSeMaior(TextMeshProUGUI texto, int tempValor, int baseValor) {
        if (texto == null) return;
        texto.color = (tempValor > baseValor) ? new Color(0.2f, 0.8f, 1f) : Color.white;
    }

    private void DestacarSeMaiorFloat(TextMeshProUGUI texto, float tempValor, float baseValor) {
        if (texto == null) return;
        texto.color = (tempValor > baseValor) ? new Color(0.2f, 0.8f, 1f) : Color.white;
    }

    public void IrParaLevelUp() {
        telaOpcoes.SetActive(false);
        telaLevelUp.SetActive(true);
        cursorIndex = 0;
        ResetarValoresTemporarios();
        AtualizarCoresDoCursor();
    }

    public void VoltarParaOpcoes() {
        telaLevelUp.SetActive(false);
        telaOpcoes.SetActive(true);
    }

    public void FecharFogueira() {
        Debug.Log("[BonfireUI] Fechando fogueira");
        // OnDisable será chamado automaticamente e reabilitará o player
        gameObject.SetActive(false);
    }

    public void VoltarParaMenuPrincipal() {
        Debug.Log("[BonfireUI] Voltando para menu principal");

        if (playerBase != null && playerStats != null) {
            SaveSystem.SavePlayer(playerBase, playerStats);
            Debug.Log("[BonfireUI] Player salvo antes de voltar ao menu");
        }

        Time.timeScale = 1f;

        // ✅ Reseta input antes de ir para o menu
        if (InputManager.Instance != null) {
            InputManager.Instance.ResetInputState();
        }

        if (GameManager.Instance != null) {
            GameManager.Instance.GoToScene("Menu");
        }
        else {
            SceneManager.LoadScene("Menu");
        }
    }

    private void ConfigurarFotoDaClasse() {
        if (iconePlayer != null && playerStats != null) {
            iconePlayer.sprite = (playerStats.currentClass == PlayerStats.CharacterClass.Warrior) ? fotoGuerreiro : fotoPaladino;
        }
    }
}