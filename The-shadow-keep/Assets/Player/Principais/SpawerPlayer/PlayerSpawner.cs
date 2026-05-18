using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// PlayerSpawner — Responsável por instanciar o Player correto (Guerreiro ou Paladino)
/// com os assets (sprite, animator) correspondentes à sua classe.
///
/// FLUXO:
/// 1. GameManager carrega a cena "Fase1" com os dados do novo personagem em pendingLoad
/// 2. PlayerSpawner detecta que é um novo jogo e instancia o prefab correto
/// 3. O prefab é configurado com sprite, animator e scripts baseado na classe
///
/// SETUP NO UNITY:
/// 1. Crie um GameObject vazio chamado "PlayerSpawner"
/// 2. Coloque este script nele
/// 3. Configure os prefabs e sprites no Inspector
/// </summary>
public class PlayerSpawner : MonoBehaviour {

    [Header("Prefabs de Personagens")]
    [Tooltip("Prefab base do Guerreiro (tem SoulslikeKnight script)")]
    [SerializeField] private GameObject warriorPrefab;

    [Tooltip("Prefab base do Paladino (tem PaladinKnight script)")]
    [SerializeField] private GameObject paladinPrefab;

    [Header("Sprites por Classe")]
    [SerializeField] private Sprite warriorSprite;
    [SerializeField] private Sprite paladinSprite;

    [Header("Animator Controllers")]
    [SerializeField] private RuntimeAnimatorController warriorAnimator;
    [SerializeField] private RuntimeAnimatorController paladinAnimator;

    [Header("Posição de Spawn")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, 0f);

    private void Start() {
        // Se estamos carregando um novo jogo (não um save)
        if (GameManager.Instance != null &&
            GameManager.Instance.pendingLoad != null &&
            !GameManager.Instance.shouldLoad) {

            SpawnPlayerForNewGame();
        }
        // Se estamos carregando um save, o player já deve estar instanciado
        // Este script só é responsável por novos personagens
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

        // Instancia o prefab correto na cena
        GameObject playerInstance = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        playerInstance.name = "Player"; // Padroniza o nome

        // Configura os componentes corretos
        if (characterClass == PlayerStats.CharacterClass.Warrior) {
            ConfigureWarrior(playerInstance);
        }
        else {
            ConfigurePaladin(playerInstance);
        }

        Debug.Log($"[PlayerSpawner] Player {characterClass} instanciado com sucesso!");
    }

    private void ConfigureWarrior(GameObject playerInstance) {
        // Sprite
        SpriteRenderer spriteRenderer = playerInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && warriorSprite != null) {
            spriteRenderer.sprite = warriorSprite;
        }

        // Animator
        Animator animator = playerInstance.GetComponent<Animator>();
        if (animator != null && warriorAnimator != null) {
            animator.runtimeAnimatorController = warriorAnimator;
        }

        // Garante que tem SoulslikeKnight e remove PaladinKnight se houver
        if (playerInstance.GetComponent<SoulslikeKnight>() == null) {
            playerInstance.AddComponent<SoulslikeKnight>();
        }
        if (playerInstance.GetComponent<PaladinKnight>() != null) {
            DestroyImmediate(playerInstance.GetComponent<PaladinKnight>());
        }
    }

    private void ConfigurePaladin(GameObject playerInstance) {
        // Sprite
        SpriteRenderer spriteRenderer = playerInstance.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && paladinSprite != null) {
            spriteRenderer.sprite = paladinSprite;
        }

        // Animator
        Animator animator = playerInstance.GetComponent<Animator>();
        if (animator != null && paladinAnimator != null) {
            animator.runtimeAnimatorController = paladinAnimator;
        }

        // Garante que tem PaladinKnight e remove SoulslikeKnight se houver
        if (playerInstance.GetComponent<PaladinKnight>() == null) {
            playerInstance.AddComponent<PaladinKnight>();
        }
        if (playerInstance.GetComponent<SoulslikeKnight>() != null) {
            DestroyImmediate(playerInstance.GetComponent<SoulslikeKnight>());
        }
    }
}