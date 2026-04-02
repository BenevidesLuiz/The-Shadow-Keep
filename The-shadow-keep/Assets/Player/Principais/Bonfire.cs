using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bonfire v2 — Fogueira estilo Dark Souls.
/// MUDANÇA v2: usa PlayerBase em vez de SoulslikeKnight.
/// Compatível com SoulslikeKnight e PaladinKnight sem alteração.
/// </summary>
public class Bonfire : MonoBehaviour {
    [Header("Interacao")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float interactRange = 2f;

    [Header("Animacao (opcional)")]
    [SerializeField] private Animator bonfireAnimator;
    private const string ANIM_IDLE = "Idle";
    private const string ANIM_ACTIVE = "Active";

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

    private PlayerBase player;          // ← era SoulslikeKnight
    private Transform playerTransform;

    private static Vector3 lastCheckpoint;
    private static Bonfire activeCheckpoint;

    // ==================================================================
    private void Start() {
        if (interactPrompt != null) interactPrompt.SetActive(false);
        if (bonfireAnimator != null) bonfireAnimator.Play(ANIM_IDLE);
    }

    private void Update() {
        if (isResting) return;

        if (playerInRange && player != null) {
            if (interactPrompt != null) interactPrompt.SetActive(true);
            if (Input.GetKeyDown(interactKey)) StartCoroutine(RestAtBonfire());
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
        player = other.GetComponent<PlayerBase>();   // ← era SoulslikeKnight
    }

    private void OnTriggerExit2D(Collider2D other) {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    // ── Descanso ──────────────────────────────────────────────────────

    private IEnumerator RestAtBonfire() {
        isResting = true;

        if (!isLit) {
            isLit = true;
            if (bonfireAnimator != null) bonfireAnimator.Play(ANIM_ACTIVE);
        }

        SetCheckpoint();

        if (interactPrompt != null) interactPrompt.SetActive(false);
        yield return new WaitForSeconds(restDuration);

        if (player != null) {
            player.RestoreHealth(99999f);
            player.RestoreStamina(99999f);
        }

        RespawnAllEnemies();
        isResting = false;
    }

    // ── Checkpoint ────────────────────────────────────────────────────

    private void SetCheckpoint() {
        activeCheckpoint = this;
        lastCheckpoint = playerTransform != null ? playerTransform.position : transform.position;
        Debug.Log($"[Bonfire] Checkpoint salvo em {lastCheckpoint}");
    }

    /// <summary>
    /// Teleporta o player para o último checkpoint e cura completamente.
    /// Aceita qualquer subclasse de PlayerBase (Knight, Paladin, etc.).
    /// </summary>
    public static void RespawnPlayerAtCheckpoint(PlayerBase player) {   // ← era SoulslikeKnight
        if (player == null) return;

        player.transform.position = lastCheckpoint != Vector3.zero ? lastCheckpoint : Vector3.zero;
        player.RestoreHealth(99999f);
        player.RestoreStamina(99999f);

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

// ------------------------------------------------------------------ //
//  Dados de spawn de cada inimigo
// ------------------------------------------------------------------ //
[System.Serializable]
public class EnemySpawnData {
    [Tooltip("O PREFAB do inimigo (nao o objeto da cena)")]
    public GameObject prefab;

    [Tooltip("Posicao onde ele vai renascer")]
    public Vector3 spawnPosition;
}