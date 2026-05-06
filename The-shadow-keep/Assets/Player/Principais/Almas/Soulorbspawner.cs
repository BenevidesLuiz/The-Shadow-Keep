using UnityEngine;

public class SoulOrbSpawner : MonoBehaviour {
    [SerializeField] private GameObject soulOrbPrefab;
    private GameObject currentOrb;

    // SoulManager usa DontDestroyOnLoad, então Start() garante que ele já existe
    private void Start() {
        if (SoulManager.Instance != null)
            SoulManager.Instance.OnSoulsLost += SpawnOrb;
    }

    private void OnDestroy()  // OnDisable → OnDestroy, mais seguro para objetos de cena
    {
        if (SoulManager.Instance != null)
            SoulManager.Instance.OnSoulsLost -= SpawnOrb;
    }

    private void SpawnOrb(int soulsAmount, Vector3 position) {
        if (soulOrbPrefab == null) return;

        if (currentOrb != null)
            Destroy(currentOrb);

        currentOrb = Instantiate(soulOrbPrefab, position, Quaternion.identity);
    }
}