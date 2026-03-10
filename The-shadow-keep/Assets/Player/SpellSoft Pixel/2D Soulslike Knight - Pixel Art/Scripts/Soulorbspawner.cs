using UnityEngine;

/// <summary>
/// SoulOrbSpawner — Escuta o SoulManager e spawna o orbe de recuperação.
///
/// SETUP:
///   1. Crie um GameObject "SoulOrbSpawner" na cena
///   2. Adicione este script
///   3. Arraste o Prefab do SoulOrb no Inspector
/// </summary>
public class SoulOrbSpawner : MonoBehaviour
{
    [SerializeField] private GameObject soulOrbPrefab;

    private GameObject currentOrb;

    private void OnEnable()
    {
        if (SoulManager.Instance != null)
            SoulManager.Instance.OnSoulsLost += SpawnOrb;
    }

    private void OnDisable()
    {
        if (SoulManager.Instance != null)
            SoulManager.Instance.OnSoulsLost -= SpawnOrb;
    }

    private void Start()
    {
        // Conecta após o SoulManager inicializar (caso seja DontDestroyOnLoad)
        if (SoulManager.Instance != null)
            SoulManager.Instance.OnSoulsLost += SpawnOrb;
    }

    private void SpawnOrb(int soulsAmount, Vector3 position)
    {
        if (soulOrbPrefab == null) return;

        // Destrói orbe anterior se existir
        if (currentOrb != null)
            Destroy(currentOrb);

        currentOrb = Instantiate(soulOrbPrefab, position, Quaternion.identity);
        Debug.Log($"[SoulOrbSpawner] Orbe com {soulsAmount} almas spawnado em {position}");
    }
}