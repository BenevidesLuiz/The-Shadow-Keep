using UnityEngine;

public class PlayerSpawner : MonoBehaviour {

    [Header("Prefabs de Personagens")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject paladinPrefab;

    [Header("Posição de Spawn Padrão (Novo Jogo)")]
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    [Header("Modo de Teste Direto na Cena")]
    [Tooltip("Se ativado, cria um player automaticamente caso você dê Play direto nesta fase.")]
    [SerializeField] private bool modoTeste = true;
    [SerializeField] private PlayerStats.CharacterClass classeParaTeste = PlayerStats.CharacterClass.Warrior;

    private void Start() {

        if (modoTeste && (GameManager.Instance == null || (!GameManager.Instance.shouldLoad && GameManager.Instance.pendingLoad == null))) {
            SpawnPlayerForTesting();
            return;
        }

        if (GameManager.Instance == null) return;

        if (GameManager.Instance.shouldLoad) {
            SpawnPlayerForLoadGame();
        }
        else if (GameManager.Instance.pendingLoad != null) {
            SpawnPlayerForNewGame();
        }
    }

    private void SpawnPlayerForNewGame() {
        PlayerStats.CharacterClass characterClass = GameManager.Instance.pendingLoad.characterClass;

        GameObject playerInstance = InstanciarPrefab(characterClass, spawnPosition);

        if (playerInstance != null) {
            PlayerBase baseScript = playerInstance.GetComponent<PlayerBase>();
            PlayerStats statsScript = playerInstance.GetComponent<PlayerStats>();

            if (baseScript != null) {
                baseScript.playerName = GameManager.Instance.pendingLoad.playerName;
                baseScript.SetCoreStatsFromMenu(GameManager.Instance.pendingLoad.currentHealth, GameManager.Instance.pendingLoad.currentStamina);
            }

            if (statsScript != null) {
                statsScript.level = GameManager.Instance.pendingLoad.level;
                statsScript.strength = GameManager.Instance.pendingLoad.strength;
                statsScript.bladeSharpness = GameManager.Instance.pendingLoad.bladeSharpness;
                statsScript.faith = GameManager.Instance.pendingLoad.faith;
                statsScript.currentClass = characterClass;
                statsScript.ApplyStatsToKnight();
            }

            if (baseScript != null) baseScript.SavePlayer();
        }

        Debug.Log($"[PlayerSpawner] NOVO JOGO: Player {characterClass} criado e configurado com sucesso!");
    }

    private void SpawnPlayerForTesting() {
        Debug.LogWarning($"[PlayerSpawner] MODO DE TESTE ATIVADO: Criando um {classeParaTeste} para testes locais!");

        GameObject playerInstance = InstanciarPrefab(classeParaTeste, transform.position);

        if (playerInstance != null) {
            PlayerBase baseScript = playerInstance.GetComponent<PlayerBase>();
            PlayerStats statsScript = playerInstance.GetComponent<PlayerStats>();

            if (baseScript != null) {
                baseScript.playerName = "Testador Local";
                baseScript.SetCoreStatsFromMenu(100f, 100f);
            }

            if (statsScript != null) {
                statsScript.level = 99;
                statsScript.currentClass = classeParaTeste;
                statsScript.ApplyStatsToKnight();
            }
        }
    }

    private void SpawnPlayerForLoadGame() {
        int slotAtual = SaveSystem.CurrentSlot;
        PlayerData data = SaveSystem.LoadPlayer(slotAtual);

        if (data != null) {
            GameObject playerInstance = InstanciarPrefab(data.characterClass, spawnPosition);

            if (playerInstance != null) {
                PlayerBase baseScript = playerInstance.GetComponent<PlayerBase>();
                if (baseScript != null) {
                    baseScript.LoadPlayer();
                }
            }
            Debug.Log($"[PlayerSpawner] LOAD GAME: Player {data.characterClass} carregado do Slot {slotAtual}!");
        }
        else {
            Debug.LogError("[PlayerSpawner] Erro: Arquivo de save não encontrado no slot ativo!");
        }
    }

    private GameObject InstanciarPrefab(PlayerStats.CharacterClass characterClass, Vector3 pos) {
        GameObject playerPrefab = (characterClass == PlayerStats.CharacterClass.Warrior)
            ? warriorPrefab
            : paladinPrefab;

        if (playerPrefab != null) {
            GameObject playerInstance = Instantiate(playerPrefab, pos, Quaternion.identity);
            playerInstance.name = "Player";
            return playerInstance;
        }
        else {
            Debug.LogError($"[PlayerSpawner] Prefab não encontrado para a classe {characterClass}!");
            return null;
        }
    }
}