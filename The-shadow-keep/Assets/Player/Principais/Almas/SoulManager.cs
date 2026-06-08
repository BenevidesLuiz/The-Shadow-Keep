using UnityEngine;

/// <summary>
/// SoulManager — Gerenciador central de Almas (moeda do jogo, estilo Dark Souls).
///
///
/// EVENTOS DISPONÍVEIS:
///   OnSoulsChanged(int atual)  — sempre que o valor muda
///   OnSoulsLost(int perdidos, Vector3 pos) — ao morrer, para spawnar orbe
/// </summary>
public class SoulManager : MonoBehaviour {

    private static SoulManager _instance;
    public static SoulManager Instance {
        get {
   
            if (_instance == null) {
                _instance = Object.FindFirstObjectByType<SoulManager>();

                if (_instance == null) {
                    GameObject go = new GameObject("SoulManager_AutoTeste");
                    _instance = go.AddComponent<SoulManager>();
                    DontDestroyOnLoad(go);
                    Debug.LogWarning("⚠️ [SoulManager] Gerenciador de Almas criado automaticamente para o Modo de Teste!");
                }
            }
            return _instance;
        }
    }

    // ── Estado ────────────────────────────────────────────────────────────
    [Header("Almas")]
    [SerializeField] private int startingSouls = 0;
    private int currentSouls;

    private int pendingRecoverySouls = 0;
    private Vector3 deathPosition;
    private bool hasPendingRecovery = false;

    public event System.Action<int> OnSoulsChanged;
    public event System.Action<int, Vector3> OnSoulsLost;
    public event System.Action<int> OnSoulsRecovered;

    public int CurrentSouls => currentSouls;
    public bool HasPendingRecovery => hasPendingRecovery;
    public int PendingRecoverySouls => pendingRecoverySouls;

    private void Awake() {
        if (_instance != null && _instance != this) {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        currentSouls = startingSouls;
    }

    /// <summary>
    /// Adiciona almas ao jogador (chamado quando inimigo morre).
    /// </summary>
    public void AddSouls(int amount) {
        if (amount <= 0) return;
        currentSouls += amount;
        OnSoulsChanged?.Invoke(currentSouls);
        Debug.Log($"[SoulManager] +{amount} almas. Total: {currentSouls}");
    }

    // ====================================================================
    //  GASTAR ALMAS
    // ====================================================================

    /// <summary>
    /// Gasta almas. Retorna true se tinha saldo suficiente.
    /// </summary>
    public bool SpendSouls(int amount) {
        if (amount > currentSouls) {
            Debug.Log($"[SoulManager] Almas insuficientes: {currentSouls} / {amount}");
            return false;
        }

        currentSouls -= amount;
        OnSoulsChanged?.Invoke(currentSouls);
        Debug.Log($"[SoulManager] -{amount} almas. Total: {currentSouls}");
        return true;
    }

    public bool CanAfford(int amount) => currentSouls >= amount;

    // ====================================================================
    //  MORTE — perde almas, deixa orbe no chão
    // ====================================================================

    /// <summary>
    /// Chamado pelo SoulslikeKnight ao morrer.
    /// Guarda as almas para possível recuperação.
    /// </summary>
    public void OnPlayerDied(Vector3 position) {
        if (currentSouls <= 0) return;

        pendingRecoverySouls = currentSouls;
        deathPosition = position;
        hasPendingRecovery = true;

        currentSouls = 0;
        OnSoulsChanged?.Invoke(currentSouls);
        OnSoulsLost?.Invoke(pendingRecoverySouls, deathPosition);

        Debug.Log($"[SoulManager] Player morreu. {pendingRecoverySouls} almas deixadas em {deathPosition}");
    }

    /// <summary>
    /// Chamado pelo orbe de recuperação ao ser coletado.
    /// </summary>
    public void RecoverSouls() {
        if (!hasPendingRecovery) return;

        int recovered = pendingRecoverySouls;
        currentSouls += recovered;
        pendingRecoverySouls = 0;
        hasPendingRecovery = false;

        OnSoulsChanged?.Invoke(currentSouls);
        OnSoulsRecovered?.Invoke(recovered);

        Debug.Log($"[SoulManager] +{recovered} almas recuperadas! Total: {currentSouls}");
    }

    /// <summary>
    /// Se morrer de novo antes de recuperar, as almas pendentes são perdidas.
    /// </summary>
    public void DiscardPendingSouls() {
        pendingRecoverySouls = 0;
        hasPendingRecovery = false;
        Debug.Log("[SoulManager] Almas pendentes perdidas para sempre.");
    }
}