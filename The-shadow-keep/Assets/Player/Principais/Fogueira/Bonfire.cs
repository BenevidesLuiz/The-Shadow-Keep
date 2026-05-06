using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bonfire v2 — Fogueira estilo Dark Souls.
/// Agora integrada com a Tela de Level Up (PainelBonfire).
/// Versão limpa: Sem animações.
/// </summary>
public class Bonfire : MonoBehaviour {
    [Header("UI do Level Up")]
    [SerializeField] private GameObject painelBonfire; 

    [Header("Interacao")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float interactRange = 2f;

    [Header("Inimigos da Fase")]
    [SerializeField] private List<EnemySpawnData> enemySpawns = new List<EnemySpawnData>();

    [Header("Feedback Visual (opcional)")]
    [SerializeField] private GameObject interactPrompt;
    [SerializeField] private float restDuration = 1.5f;

    // ── Estado ────────────────────────────────────────────────────────
    private bool playerInRange = false;
    private bool isResting = false;
    private bool isLit = false;

    private static List<EnemySpawnData> registeredSpawns = new List<EnemySpawnData>();

    private PlayerBase player;
    private Transform playerTransform;

    private static Vector3 lastCheckpoint;
    private static Bonfire activeCheckpoint;

    // ==================================================================
    private void Start() {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update() {
        // Se o painel de UI já estiver aberto, não faz nada
        if (painelBonfire != null && painelBonfire.activeSelf) {
            if (interactPrompt != null) interactPrompt.SetActive(false);
            return;
        }

        if (isResting) return;

        if (playerInRange && player != null) {
            if (interactPrompt != null) interactPrompt.SetActive(true);

            if (Input.GetKeyDown(interactKey)) {
                StartCoroutine(RestAtBonfire());
            }
        }
        else {
            if (interactPrompt != null) interactPrompt.SetActive(false);
        }
    }

    // ── Detecção de player ────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        playerTransform = other.transform;
        player = other.GetComponent<PlayerBase>();
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;

        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (painelBonfire != null) painelBonfire.SetActive(false); 
    }

    private IEnumerator RestAtBonfire() {
        isResting = true;

        if (!isLit) {
            isLit = true;
            // A fogueira foi acesa pela primeira vez!
        }

        SetCheckpoint();

        if (interactPrompt != null) interactPrompt.SetActive(false);

        // Espera o tempo de "descanso" (tela escura, fade, ou só uma pausa rápida)
        yield return new WaitForSeconds(restDuration);

        // 1. Cura total e recarrega os frascos (Estus)
        if (player != null) {
            player.RestoreHealth(99999f);
            player.RestoreStamina(99999f);
            player.RefillEstus();

            // 2. Salva o jogo automaticamente no slot atual!
            player.SavePlayer();
        }

        // 3. Renasce os inimigos
        RespawnAllEnemies();

        // 4. Abre a tela de Level Up
        if (painelBonfire != null) {
            painelBonfire.SetActive(true);
        }

        isResting = false;
    }

    private void SetCheckpoint() {
        activeCheckpoint = this;
        lastCheckpoint = playerTransform != null ? playerTransform.position : transform.position;
        Debug.Log($"[Bonfire] Checkpoint salvo em {lastCheckpoint}");
    }

    public static void RespawnPlayerAtCheckpoint(PlayerBase player) {
        if (player == null) return;

        player.transform.position = lastCheckpoint != Vector3.zero ? lastCheckpoint : Vector3.zero;
        player.RestoreHealth(99999f);
        player.RestoreStamina(99999f);
        player.RefillEstus();

        if (activeCheckpoint != null) activeCheckpoint.RespawnAllEnemies();
    }

    // ── Respawn de inimigos ───────────────────────────────────────────

    private static List<EnemySpawner> registeredSpawners = new List<EnemySpawner>();

    public static void RegisterSpawner(EnemySpawner spawner) {
        if (!registeredSpawners.Contains(spawner)) registeredSpawners.Add(spawner);
    }

    private void RespawnAllEnemies() {
        registeredSpawners.RemoveAll(s => s == null);
        foreach (EnemySpawner spawner in registeredSpawners) spawner.ResetSpawner();
        Debug.Log($"[Bonfire] {registeredSpawners.Count} spawner(s) resetados.");
    }

    public static void RegisterEnemySpawn(GameObject prefab, Vector3 position) {
        registeredSpawns.Add(new EnemySpawnData { prefab = prefab, spawnPosition = position });
    }

    // ── Gizmos ────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected() {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

[System.Serializable]
public class EnemySpawnData {
    [Tooltip("O PREFAB do inimigo (nao o objeto da cena)")]
    public GameObject prefab;

    [Tooltip("Posicao onde ele vai renascer")]
    public Vector3 spawnPosition;
}