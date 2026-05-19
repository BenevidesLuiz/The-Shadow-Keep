using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bonfire v3 CORRIGIDO
/// - Reseta estado quando sai da cena
/// - Desabilita input do player quando painel abre
/// - Sincroniza com InputManager
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

    private bool playerInRange = false;
    private bool isResting = false;
    private bool isLit = false;

    private static List<EnemySpawnData> registeredSpawns = new List<EnemySpawnData>();
    private static List<EnemySpawner> registeredSpawners = new List<EnemySpawner>();

    private PlayerBase player;
    private Transform playerTransform;

    private static Vector3 lastCheckpoint;
    private static Bonfire activeCheckpoint;

    private void OnEnable() {
        // Se estamos voltando ao menu (shouldLoad = false e estamos na cena de menu)
        if (GameManager.Instance != null && !GameManager.Instance.shouldLoad) {
            ResetBonfireState();
        }
    }

    private void Start() {
        if (interactPrompt != null) interactPrompt.SetActive(false);
    }

    private void Update() {
        if (painelBonfire != null && painelBonfire.activeSelf) {
            if (interactPrompt != null) interactPrompt.SetActive(false);

            if (InputManager.Instance != null) {
                InputManager.Instance.DisableInput("Painel Bonfire aberto");
            }

            return;
        }

        if (InputManager.Instance != null && !InputManager.Instance.IsInputEnabled()) {
            InputManager.Instance.EnableInput("Painel Bonfire fechado");
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

        if (painelBonfire != null && painelBonfire.activeSelf) {
            painelBonfire.SetActive(false);
            if (InputManager.Instance != null) {
                InputManager.Instance.EnableInput("Player saiu do trigger da fogueira");
            }
        }
    }

    private IEnumerator RestAtBonfire() {
        isResting = true;

        if (!isLit) {
            isLit = true;
        }

        SetCheckpoint();

        if (interactPrompt != null) interactPrompt.SetActive(false);

        if (InputManager.Instance != null) {
            InputManager.Instance.DisableInput("Descansando na fogueira");
        }

        yield return new WaitForSeconds(restDuration);

        // 1. Cura total e recarrega os frascos
        if (player != null) {
            player.RestoreHealth(99999f);
            player.RestoreStamina(99999f);
            player.RefillEstus();
            player.SavePlayer(); 
        }


        RespawnAllEnemies();

        if (painelBonfire != null) {
            painelBonfire.SetActive(true);
            Debug.Log("[Bonfire] Painel de Level Up aberto. Input desabilitado.");
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

    
    public static void RegisterSpawner(EnemySpawner spawner) {
        if (!registeredSpawners.Contains(spawner)) {
            registeredSpawners.Add(spawner);
        }
    }

    private void RespawnAllEnemies() {
        registeredSpawners.RemoveAll(s => s == null);
        foreach (EnemySpawner spawner in registeredSpawners) {
            spawner.ResetSpawner();
        }
        Debug.Log($"[Bonfire] {registeredSpawners.Count} spawner(s) resetados.");
    }

    public static void RegisterEnemySpawn(GameObject prefab, Vector3 position) {
        registeredSpawns.Add(new EnemySpawnData { prefab = prefab, spawnPosition = position });
    }

    private void OnDestroy() {
        ResetBonfireState();
    }

    private static void ResetBonfireState() {
        lastCheckpoint = Vector3.zero;
        activeCheckpoint = null;
        registeredSpawns.Clear();
        registeredSpawners.Clear();
        Debug.Log("[Bonfire] Estado resetado ao sair da cena");
    }

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