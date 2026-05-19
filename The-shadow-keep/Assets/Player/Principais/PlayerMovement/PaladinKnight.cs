using UnityEngine;

/// <summary>
/// PaladinKnight — Subclasse do Paladino com habilidades sagradas.
///
/// CARACTERÍSITCAS:
///   - Ataque Leve: Golpe Sagrado com life steal de 20% do dano
///   - Ataque Pesado: Golpe Divino com explosão em área e stun
///   - Habilidade R: Bênção Divina — restaura stamina e aplica proteção
///
/// INPUT:
///   P = Ataque Leve (Attack1)
///   U = Ataque Pesado / Golpe Divino (Attack2)
///   I = Ataque Especial 2 (Attack3)
///   R = Bênção Divina (restaura stamina, aplica bônus de defesa)
///
/// SETUP NO UNITY:
///   1. Crie um GameObject "Player_Paladin" com este script
///   2. Adicione Rigidbody2D, Animator, Collider2D(s) e tag "Player"
///   3. Crie dois filhos com Box Collider 2D (Is Trigger = ON):
///      - "HitboxSagrado" → adicione PaladinHitbox (isHeavy = false)
///      - "HitboxDivino" → adicione PaladinHitbox (isHeavy = true)
///   4. Crie um filho "GroundCheck" e arraste em groundCheckTransform
/// </summary>
public class PaladinKnight : PlayerBase {

    [Header("Bênção Divina (Habilidade R)")]
    [SerializeField] private float blessingCooldown = 8f;
    [SerializeField] private float blessingDefenseBonus = 0.6f;  
    [SerializeField] private float blessingDuration = 4f;

    private float blessingCooldownTimer = 0f;
    private bool isBlessingActive = false;
    private float blessingEndTime = 0f;

    // Evento para a UI monitorar o cooldown da Bênção
    public event System.Action<float, float> OnBlessingCooldownChanged;
    public event System.Action<bool> OnBlessingActiveChanged;

    // ================================================================== //
    //  PROPRIEDADES PÚBLICAS
    // ================================================================== //

    public bool IsBlessingActive => isBlessingActive;
    public float BlessingCooldownRemaining => Mathf.Max(0f, blessingCooldownTimer);

    // ================================================================== //
    //  UNITY CALLBACKS
    // ================================================================== //

    protected override void Awake() {
        base.Awake();
        // Inicializa sem bênção
        isBlessingActive = false;
        blessingCooldownTimer = 0f;
    }

    protected override void Start() {
        base.Start();
    }

    protected override void Update() {
        base.Update();
        UpdateBlessingCooldown();
    }

    // ================================================================== //
    //  INPUT DE COMBATE
    // ================================================================== //

    protected override void ReadCombatInput() {
        bool fatigued = playerStats != null && playerStats.isFatigued;
        if (fatigued) return;

        // Ataque Leve — Golpe Sagrado (dano normal + life steal)
        if (Input.GetKeyDown(KeyCode.P)) {
            TryAttackLight();
        }

        // Ataque Pesado — Golpe Divino (dano alto + explosão + stun)
        if (Input.GetKeyDown(KeyCode.U)) {
            TryAttackSpecial1();
        }

        // Ataque Especial 2
        if (Input.GetKeyDown(KeyCode.I)) {
            TryAttackSpecial2();
        }

        // Habilidade R — Bênção Divina (restaura stamina + proteção temporária)
        if (Input.GetKeyDown(KeyCode.R)) {
            TryActivateBlessing();
        }
    }

    // ================================================================== //
    //  BÊNÇÃO DIVINA (Habilidade R)
    // ================================================================== //

    private void TryActivateBlessing() {
        // Verifica se está em cooldown
        if (blessingCooldownTimer > 0f) {
            Debug.Log($"[PaladinKnight] Bênção em cooldown! Tempo restante: {blessingCooldownTimer:F1}s");
            return;
        }

        // Se já está ativa, não deixa ativar novamente
        if (isBlessingActive) {
            Debug.Log("[PaladinKnight] Bênção já está ativa!");
            return;
        }

        // Ativa a bênção
        ActivateBlessing();
    }

    private void ActivateBlessing() {
        isBlessingActive = true;
        blessingEndTime = Time.time + blessingDuration;
        blessingCooldownTimer = blessingCooldown + blessingDuration;

        // Restaura stamina completa
        RestoreStamina(MaxStamina);

        Debug.Log($"[PaladinKnight] Bênção Divina ativada! Stamina restaurada. Duração: {blessingDuration}s");

        OnBlessingActiveChanged?.Invoke(true);
        OnBlessingCooldownChanged?.Invoke(blessingCooldownTimer, blessingCooldown + blessingDuration);
    }

    private void UpdateBlessingCooldown() {
        // Se a bênção está ativa, verifica se já passou a duração
        if (isBlessingActive && Time.time >= blessingEndTime) {
            isBlessingActive = false;
            Debug.Log("[PaladinKnight] Bênção Divina expirou.");
            OnBlessingActiveChanged?.Invoke(false);
        }

        // Decrementa o cooldown
        if (blessingCooldownTimer > 0f) {
            blessingCooldownTimer -= Time.deltaTime;
            OnBlessingCooldownChanged?.Invoke(blessingCooldownTimer, blessingCooldown + blessingDuration);
        }
    }

    // ================================================================== //
    //  MODIFICADOR DE DANO (Proteção da Bênção)
    // ================================================================== //

    public override void TakeDamage(float amount) {
        // Se a bênção está ativa, reduz o dano recebido
        if (isBlessingActive) {
            float reducedDamage = amount * (1f - blessingDefenseBonus);
            Debug.Log($"[PaladinKnight] Bênção Divina ativa! Dano reduzido de {amount} para {reducedDamage}");
            base.TakeDamage(reducedDamage);
        }
        else {
            base.TakeDamage(amount);
        }
    }

    // ================================================================== //
    //  CALLBACK DE ROUBO DE VIDA (Ataque Leve)
    // ================================================================== //

    /// <summary>
    /// Chamado por PaladinHitbox quando um ataque sagrado acerta um inimigo.
    /// Implementa o roubo de vida de 20% do dano causado.
    /// </summary>
    public void OnHolyHit(float damageDealt) {
        float heal = damageDealt * 0.20f;
        if (heal > 0f) {
            RestoreHealth(heal);
            Debug.Log($"[PaladinKnight] Roubo de Vida Sagrado: +{heal:F1} de HP");
        }
    }

    // ================================================================== //
    //  GETTERS DE DANO
    // ================================================================== //

    /// <summary>
    /// Retorna o dano leve do Paladino.
    /// Pode ser modificado pelo PlayerStats se necessário.
    /// </summary>
    public new float GetLightDamage() {
        return lightDamage;
    }

    /// <summary>
    /// Retorna o dano pesado do Paladino.
    /// Pode ser modificado pelo PlayerStats se necessário.
    /// </summary>
    public new float GetHeavyDamage() {
        return heavyDamage;
    }
}