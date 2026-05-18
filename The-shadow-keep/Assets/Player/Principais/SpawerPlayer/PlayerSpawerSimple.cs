using UnityEngine;

public class PlayerSpawerSimple : MonoBehaviour{

    [Header("Prefabs de Personagens")]
    [SerializeField] private GameObject warriorPrefab;
    [SerializeField] private GameObject paladinPrefab;

    [Header("Posição de Spawn")]
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    private void Start() {
        if (GameManager.Instance != null &&
            GameManager.Instance.pendingLoad != null &&
            !GameManager.Instance.shouldLoad) {

            SpawnPlayerForNewGame();
        }
    }

    private void SpawnPlayerForNewGame() {
        PlayerStats.CharacterClass characterClass = GameManager.Instance.pendingLoad.characterClass;

        GameObject playerPrefab = (characterClass == PlayerStats.CharacterClass.Warrior)
            ? warriorPrefab
            : paladinPrefab;

        if (playerPrefab == null) {
            Debug.LogError($"[PlayerSpawner] Prefab não encontrado para classe {characterClass}!");
            return;
        }

        // Instancia o prefab correto
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerInstance.name = "Player";

        // Apenas garante que tem o script correto
        if (characterClass == PlayerStats.CharacterClass.Warrior) {
            if (playerInstance.GetComponent<SoulslikeKnight>() == null) {
                playerInstance.AddComponent<SoulslikeKnight>();
            }
            DestroyImmediate(playerInstance.GetComponent<PaladinKnight>());
        }
        else {
            if (playerInstance.GetComponent<PaladinKnight>() == null) {
                playerInstance.AddComponent<PaladinKnight>();
            }
            DestroyImmediate(playerInstance.GetComponent<SoulslikeKnight>());
        }

        Debug.Log($"[PlayerSpawner] Player {characterClass} instanciado com sucesso!");
    }
}

