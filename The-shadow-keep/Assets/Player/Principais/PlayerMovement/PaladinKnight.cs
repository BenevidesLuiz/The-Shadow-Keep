using System.Collections;
using UnityEngine;

/// <summary>
/// PaladinKnight — Paladino, herda de PlayerBase.
///
/// HABILIDADES EXCLUSIVAS:
///   ┌─────────────────────────────────────────────────────────────────┐
///   │  Tecla P  →  Ataque Sagrado (leve)                             │
///   │              Dano normal + cura o paladino em 20% do dano      │
///   │                                                                 │
///   │  Tecla U  →  Golpe Divino (pesado, trigger "special")          │
///   │              Dano alto, consome mais stamina                    │
///   │                                                                 │
///   │  Tecla R  →  Bênção Divina (cooldown blessingCooldown s)       │
///   │              Regena toda a stamina instantaneamente            │
///   │              + bônus de regen de stamina por blessingDuration s │
///   └─────────────────────────────────────────────────────────────────┘
///
/// STATS BASE diferentes do Knight:
///   - maxHealth  +20  (tanque)
///   - lightDamage -2  (menos dano bruto, mas se cura)
///   - heavyDamage +4  (golpe divino mais forte)
///   - staminaCostLight +5 (ataques sagrados custam mais stamina)
///
/// SETUP NO UNITY:
///   1. Crie um novo prefab Player_Paladin
///   2. Adicione ESTE script (PaladinKnight) — NÃO o SoulslikeKnight
///   3. Adicione PlayerStats, PlayerHitbox(es), Rigidbody2D, Animator
///   4. No PlayerStats: mude currentClass para Paladin
///   5. Configure o Animator Controller com o spritesheet do Paladino
///      (mesmos trigger names: "attack", "special" — assim o PlayerCombat
///       e os Animation Events funcionam sem mudança)
/// </summary>
public class PaladinKnight : PlayerBase {

    // ------------------------------------------------------------------ //
    //  Inspector — Ataque Sagrado (leve)
    // ------------------------------------------------------------------ //
    [Header("Ataque Sagrado (leve — tecla P)")]
    [Tooltip("Percentual do dano causado que é devolvido como vida (0–1)")]
    [SerializeField, Range(0f, 1f)] private float lifeStealRatio = 0.20f;

    // ------------------------------------------------------------------ //
    //  Inspector — Bênção Divina (tecla R)
    // ------------------------------------------------------------------ //
    [Header("Bênção Divina (tecla R)")]
    [SerializeField] private float blessingCooldown = 12f;  // segundos entre usos
    [SerializeField] private float blessingDuration = 5f;   // tempo com regen turbinado
    [SerializeField] private float blessingRegenBonus = 40f;  // regen extra durante a bênção
    [Tooltip("Efeito visual/sonoro opcional ao ativar a Bênção")]
    [SerializeField] private GameObject blessingVFX;

    // ── Estado da bênção ──────────────────────────────────────────────
    private float blessingCooldownTimer = 0f;
    private bool blessingActive = false;

    // Evento para a UI saber do cooldown (opcional)
    public event System.Action<float, float> OnBlessingCooldown; // (atual, max)


    protected override void Start() {
        maxHealth += 20f;   
        lightDamage -= 2f;  
        heavyDamage += 4f;   
        staminaCostLight += 5f;
        base.Start();              
    }

    protected override void Update() {
        base.Update();             // movimento, base input, regen

        // Conta regressiva do cooldown da bênção
        if (blessingCooldownTimer > 0f) {
            blessingCooldownTimer -= Time.deltaTime;
            OnBlessingCooldown?.Invoke(blessingCooldownTimer, blessingCooldown);
        }
    }

    // ================================================================== //
    //  INPUT DE COMBATE  (implementa o abstrato de PlayerBase)
    // ================================================================== //

    protected override void ReadCombatInput() {
        bool fatigued = playerStats != null && playerStats.isFatigued;

        if (!fatigued) {
            if (Input.GetKeyDown(KeyCode.P)) TryAttackLight();    // life steal aplicado no OnHolyHit
            if (Input.GetKeyDown(KeyCode.U)) TryAttackSpecial1();
            if (Input.GetKeyDown(KeyCode.I)) TryAttackSpecial2();
        }

        // Bênção Divina — sem bloqueio por fadiga
        if (Input.GetKeyDown(KeyCode.R) && blessingCooldownTimer <= 0f && !blessingActive)
            StartCoroutine(BlessingRoutine());
    }
    // ================================================================== //
    //  BÊNÇÃO DIVINA
    // ================================================================== //

    private IEnumerator BlessingRoutine() {
        blessingActive = true;
        blessingCooldownTimer = blessingCooldown;

        // Ativa VFX
        if (blessingVFX != null) blessingVFX.SetActive(true);

        // Restaura stamina instantaneamente
        RestoreStamina(maxStamina);

        // Guarda regen original e aplica bônus
        float originalRegen = staminaRegenRate;
        staminaRegenRate += blessingRegenBonus;

        Debug.Log($"[PaladinKnight] Bênção Divina ativa por {blessingDuration}s!");

        yield return new WaitForSeconds(blessingDuration);

        // Reverte regen
        staminaRegenRate = originalRegen;
        blessingActive = false;

        if (blessingVFX != null) blessingVFX.SetActive(false);

        Debug.Log("[PaladinKnight] Bênção Divina encerrada.");
    }

    // ================================================================== //
    //  CALLBACK DE DANO CAUSADO — chamado pelo PlayerHitbox
    //
    //  O PlayerHitbox pode chamar este método via:
    //      hit.GetComponentInParent<PaladinKnight>()?.OnHolyHit(damage);
    //  OU adicione em PlayerHitbox uma chamada genérica ao PlayerBase.
    // ================================================================== //

    /// <summary>
    /// Chamado após o Paladino causar dano num inimigo.
    /// Devolve uma fração do dano como vida (life steal).
    /// </summary>
    public void OnHolyHit(float damageDealt) {
        float heal = damageDealt * lifeStealRatio;
        if (heal > 0f) {
            RestoreHealth(heal);
            Debug.Log($"[PaladinKnight] Life steal: +{heal:F1} HP");
        }
    }

    // ================================================================== //
    //  GIZMOS
    // ================================================================== //

    protected override void OnDrawGizmosSelected() {
        base.OnDrawGizmosSelected();

        // Raio visual do cooldown da bênção
        Gizmos.color = blessingCooldownTimer <= 0f ? Color.cyan : new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
