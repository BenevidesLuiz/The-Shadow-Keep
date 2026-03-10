using UnityEngine;

/// <summary>
/// SoulManager — Gerenciador central de Almas (moeda do jogo, estilo Dark Souls).
///
/// COMO USAR:
///   - Adicione este script num GameObject vazio "GameManager" na cena
///   - Acesse via SoulManager.Instance de qualquer lugar
///   - Inimigos chamam SoulManager.Instance.AddSouls(amount) ao morrer
///   - Player perde almas ao morrer (ficam no chão para recuperar)
///
/// EVENTOS DISPONÍVEIS:
///   OnSoulsChanged(int atual)  — sempre que o valor muda
///   OnSoulsLost(int perdidos, Vector3 pos) — ao morrer, para spawnar orbe
/// </summary>
public class SoulManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────
    public static SoulManager Instance { get; private set; }

    // ── Estado ────────────────────────────────────────────────────────────
    [Header("Almas")]
    [SerializeField] private int startingSouls = 0;
    private int currentSouls;

    // Almas perdidas na última morte (para o orbe de recuperação)
    private int pendingRecoverySouls = 0;
    private Vector3 deathPosition;
    private bool hasPendingRecovery = false;

    // ── Eventos ──────────────────────────────────────────────────────────
    public event System.Action<int> OnSoulsChanged;
    public event System.Action<int, Vector3> OnSoulsLost; // (quantidade, posição de morte)
    public event System.Action<int> OnSoulsRecovered;

    // ── Propriedades públicas ─────────────────────────────────────────────
    public int CurrentSouls => currentSouls;
    public bool HasPendingRecovery => hasPendingRecovery;
    public int PendingRecoverySouls => pendingRecoverySouls;

    // ====================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentSouls = startingSouls;
    }

    // ====================================================================
    //  GANHAR ALMAS
    // ====================================================================

    /// <summary>
    /// Adiciona almas ao jogador (chamado quando inimigo morre).
    /// </summary>
    public void AddSouls(int amount)
    {
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
    public bool SpendSouls(int amount)
    {
        if (amount > currentSouls)
        {
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
    public void OnPlayerDied(Vector3 position)
    {
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
    public void RecoverSouls()
    {
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
    public void DiscardPendingSouls()
    {
        pendingRecoverySouls = 0;
        hasPendingRecovery = false;
        Debug.Log("[SoulManager] Almas pendentes perdidas para sempre.");
    }
}