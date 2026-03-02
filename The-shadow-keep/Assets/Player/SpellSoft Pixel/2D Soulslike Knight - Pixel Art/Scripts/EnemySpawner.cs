using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Setup")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public float timeBetweenSpawns = 0.5f;
    public int numberOfEnemies = 5;

    [Header("Dark Souls Behavior")]
    [Tooltip("Se falso, inimigos NÃO respawnam até o player descansar na fogueira")]
    public bool respawnOnBonfireRest = true;

    private bool hasTriggered = false;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private int aliveCount = 0;
    public bool AllEnemiesDead => aliveCount <= 0 && hasTriggered;

    private void Start()
    {
        // Auto-registra este spawner na Bonfire
        Bonfire.RegisterSpawner(this);
    }

    // A ÚNICA MUDANÇA FOI NESTA LINHA: OnTriggerEnter2D e Collider2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(SpawnWave());
        }
    }

    // ==================================================================== //
    //  SPAWN
    // ==================================================================== //

    IEnumerator SpawnWave()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            if (spawnPoints.Length == 0) break;

            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject newEnemy = Instantiate(enemyPrefab, sp.position, sp.rotation);

            spawnedEnemies.Add(newEnemy);
            aliveCount++;

            // Notifica este spawner quando o inimigo morrer
            EnemyDeathNotifier notifier = newEnemy.AddComponent<EnemyDeathNotifier>();
            notifier.spawner = this;

            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    // ==================================================================== //
    //  CHAMADO PELA BONFIRE ao descansar ou ao morrer
    // ==================================================================== //

    public void ResetSpawner()
    {
        if (!respawnOnBonfireRest) return;

        StopAllCoroutines();

        // Destroi inimigos vivos
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        spawnedEnemies.Clear();
        aliveCount = 0;

        hasTriggered = false;

        Debug.Log($"[EnemySpawner] {gameObject.name} resetado pela Bonfire.");
    }


    public void OnEnemyDied(GameObject enemy)
    {
        spawnedEnemies.Remove(enemy);
        aliveCount = Mathf.Max(0, aliveCount - 1);

        Debug.Log($"[EnemySpawner] Inimigo morreu. Restam: {aliveCount}");
    }
}

// ====================================================================== //
// notifica o spawner quando o inimigo morre
// ====================================================================== //
public class EnemyDeathNotifier : MonoBehaviour
{
    [HideInInspector] public EnemySpawner spawner;

    private void OnDestroy()
    {
        if (spawner != null)
            spawner.OnEnemyDied(gameObject);
    }
}