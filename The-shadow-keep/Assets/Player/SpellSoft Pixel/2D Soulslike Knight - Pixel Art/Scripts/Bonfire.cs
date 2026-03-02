using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bonfire — Fogueira estilo Dark Souls.
///
/// SETUP NO UNITY:
///   1. Crie um GameObject "Fogueira"
///   2. Adicione este script
///   3. Adicione um Collider2D com Is Trigger = ON (area de interacao)
///   4. Adicione um Animator (opcional) com clips "Idle" e "Active"
///   5. Marque a Tag do Player como "Player"
///   6. Arraste todos os inimigos da fase no Inspector (lista enemyPrefabs)
///      OU use o modo de auto-registro (inimigos se registram sozinhos)
///
/// TECLA PADRAO: F para descansar
/// </summary>
public class Bonfire : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Configuracao
    // ------------------------------------------------------------------ //
    [Header("Interacao")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float interactRange = 2f;

    [Header("Animacao (opcional)")]
    [SerializeField] private Animator bonfireAnimator;
    private const string ANIM_IDLE = "Idle";
    private const string ANIM_ACTIVE = "Active";

    [Header("Inimigos da Fase")]
    [Tooltip("Arraste os PREFABS dos inimigos aqui. Eles serao reinstanciados ao descansar.")]
    [SerializeField] private List<EnemySpawnData> enemySpawns = new List<EnemySpawnData>();

    [Header("Feedback Visual (opcional)")]
    [SerializeField] private GameObject interactPrompt; // ex: texto "Pressione F"
    [SerializeField] private float restDuration = 1.5f; // tempo de animacao de descanso

    // ------------------------------------------------------------------ //
    //  Estado
    // ------------------------------------------------------------------ //
    private bool playerInRange = false;
    private bool isResting = false;
    private bool isLit = false; // fogueira acesa pela primeira vez

    // Inimigos ativos no momento (registrados automaticamente)
    private static List<EnemySpawnData> registeredSpawns = new List<EnemySpawnData>();

    // Referencia ao player
    private SoulslikeKnight player;
    private Transform playerTransform;

    // Checkpoint: posicao de respawn do player
    private static Vector3 lastCheckpoint;
    private static Bonfire activeCheckpoint;

    // ==================================================================== //
    //  UNITY CALLBACKS
    // ==================================================================== //

    private void Start()
    {
        // Esconde o prompt de interacao inicialmente
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        // Anima fogueira apagada
        if (bonfireAnimator != null)
            bonfireAnimator.Play(ANIM_IDLE);
    }

    private void Update()
    {
        if (isResting) return;

        if (playerInRange && player != null)
        {
            // Mostra prompt apenas se o player esta vivo
            if (interactPrompt != null)
                interactPrompt.SetActive(true);

            // Detecta input de interacao
            if (Input.GetKeyDown(interactKey))
                StartCoroutine(RestAtBonfire());
        }
        else
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(false);
        }
    }

    // ==================================================================== //
    //  COLISAO — detecta player na area
    // ==================================================================== //

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        playerTransform = other.transform;
        player = other.GetComponent<SoulslikeKnight>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;

        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    // ==================================================================== //
    //  DESCANSO
    // ==================================================================== //

    private IEnumerator RestAtBonfire()
    {
        isResting = true;

        // Acende a fogueira na primeira vez
        if (!isLit)
        {
            isLit = true;
            if (bonfireAnimator != null)
                bonfireAnimator.Play(ANIM_ACTIVE);
        }

        // Define este como checkpoint ativo
        SetCheckpoint();

        // Esconde o prompt durante o descanso
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        // Aguarda animacao de descanso
        yield return new WaitForSeconds(restDuration);

        // ── 1. Cura o player completamente ──
        if (player != null)
        {
            player.RestoreHealth(99999f); // restaura tudo
            player.RestoreStamina(99999f);
        }

        // ── 2. Respawna todos os inimigos ──
        RespawnAllEnemies();

        isResting = false;
    }

    // ==================================================================== //
    //  CHECKPOINT
    // ==================================================================== //

    private void SetCheckpoint()
    {
        activeCheckpoint = this;

        if (playerTransform != null)
            lastCheckpoint = playerTransform.position;
        else
            lastCheckpoint = transform.position;

        Debug.Log($"[Bonfire] Checkpoint salvo em {lastCheckpoint}");
    }

    /// <summary>
    /// Chame isso ao morrer para retornar ao ultimo checkpoint.
    /// </summary>
    public static void RespawnPlayerAtCheckpoint(SoulslikeKnight knight)
    {
        if (knight == null) return;

        // Teleporta para o checkpoint
        knight.transform.position = lastCheckpoint != Vector3.zero
            ? lastCheckpoint
            : Vector3.zero;

        // Cura completamente
        knight.RestoreHealth(99999f);
        knight.RestoreStamina(99999f);

        // Respawna inimigos
        if (activeCheckpoint != null)
            activeCheckpoint.RespawnAllEnemies();
    }

    // ==================================================================== //
    //  RESPAWN DE INIMIGOS
    // ==================================================================== //

    private static List<EnemySpawner> registeredSpawners = new List<EnemySpawner>();

    // ── Método de registro (chamado pelo EnemySpawner.Start) ──
    public static void RegisterSpawner(EnemySpawner spawner)
    {
        if (!registeredSpawners.Contains(spawner))
            registeredSpawners.Add(spawner);
    }

    // ── Substitui RespawnAllEnemies() ──
    private void RespawnAllEnemies()
    {
        // Remove spawners destruídos da lista
        registeredSpawners.RemoveAll(s => s == null);

        foreach (EnemySpawner spawner in registeredSpawners)
            spawner.ResetSpawner();

        Debug.Log($"[Bonfire] {registeredSpawners.Count} spawner(s) resetados.");
    }


    // ==================================================================== //
    //  REGISTRO AUTOMATICO DE INIMIGOS
    //  Chame Enemy.RegisterSpawn() no Start() do Enemy para auto-registrar
    // ==================================================================== //

    public static void RegisterEnemySpawn(GameObject prefab, Vector3 position)
    {
        registeredSpawns.Add(new EnemySpawnData { prefab = prefab, spawnPosition = position });
    }

    // ==================================================================== //
    //  GIZMOS
    // ==================================================================== //

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}

// ------------------------------------------------------------------ //
//  Dados de spawn de cada inimigo
// ------------------------------------------------------------------ //
[System.Serializable]
public class EnemySpawnData
{
    [Tooltip("O PREFAB do inimigo (nao o objeto da cena)")]
    public GameObject prefab;

    [Tooltip("Posicao onde ele vai renascer")]
    public Vector3 spawnPosition;
}