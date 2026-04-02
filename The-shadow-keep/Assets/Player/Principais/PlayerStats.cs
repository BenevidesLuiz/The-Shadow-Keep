using UnityEngine;
using System.Collections;

/// <summary>
/// PlayerStats v3 — Integrada com PlayerBase (funciona com SoulslikeKnight E PaladinKnight).
///
/// MUDANÇA v3:
///   - [RequireComponent] trocado de SoulslikeKnight → PlayerBase
///   - Resto inalterado
/// </summary>
[RequireComponent(typeof(PlayerBase))]
public class PlayerStats : MonoBehaviour {
    public enum CharacterClass { Warrior, Paladin }

    [Header("Identidade")]
    public CharacterClass currentClass = CharacterClass.Warrior;

    [Header("Atributos Base")]
    public int strength = 10;

    [Header("Mecânica: Lâmina Cega")]
    [Tooltip("100 = Afiada, 0 = Completamente Cega")]
    [Range(0f, 100f)]
    public float bladeSharpness = 100f;

    [SerializeField] private float sharpnessCostPerAttack = 5f;

    [Header("Fadiga")]
    [Tooltip("Se verdadeiro, o knight fica temporariamente sem stamina")]
    public bool isFatigued = false;
    [SerializeField] private float fatigueDuration = 3f;

    // ── Refs ─────────────────────────────────────────────────────────────
    private PlayerBase player;   // ← era SoulslikeKnight

    // ── Valores base (antes de modificadores) ────────────────────────────
    private float baseLightDamage;
    private float baseHeavyDamage;

    // ── Eventos ──────────────────────────────────────────────────────────
    public event System.Action<float> OnSharpnessChanged;
    public event System.Action<bool> OnFatigueChanged;
    public event System.Action<int> OnStrengthChanged;

    // ── Propriedades ─────────────────────────────────────────────────────
    public float DamageMultiplier => CalculateDamageMultiplier();
    public float BladeSharpness => bladeSharpness;

    // ====================================================================
    private void Awake() {
        player = GetComponent<PlayerBase>();   // ← era SoulslikeKnight
    }

    private void Start() {
        baseLightDamage = player.GetLightDamage();
        baseHeavyDamage = player.GetHeavyDamage();
        ConfigurarClasseInicial();
    }

    // ====================================================================
    //  CONFIGURAÇÃO DE CLASSE
    // ====================================================================

    private void ConfigurarClasseInicial() {
        switch (currentClass) {
            case CharacterClass.Warrior:
                strength = 10;
                break;
            case CharacterClass.Paladin:
                strength = 8;
                player.RestoreHealth(20f);
                break;
        }
        ApplyStatsToKnight();
        Debug.Log($"[PlayerStats] Classe: {currentClass} | Força: {strength} | Dano base: {baseLightDamage}/{baseHeavyDamage}");
    }

    // ====================================================================
    //  APLICAR STATS
    // ====================================================================

    public void ApplyStatsToKnight() {
        float mult = CalculateDamageMultiplier();
        player.SetLightDamage(baseLightDamage * mult);
        player.SetHeavyDamage(baseHeavyDamage * mult);
    }

    private float CalculateDamageMultiplier() {
        float strengthMult = Mathf.Max(0.5f, 1f + (strength - 10) * 0.05f);
        float sharpnessMult = bladeSharpness < 30f ? 0.5f : 1f;
        float classMult = currentClass == CharacterClass.Warrior ? 1.1f : 1f;
        return strengthMult * sharpnessMult * classMult;
    }

    // ====================================================================
    //  DESGASTE DA LÂMINA
    // ====================================================================

    public void OnAttackPerformed() => WearBlade(sharpnessCostPerAttack);

    private void WearBlade(float amount) {
        bladeSharpness = Mathf.Max(0f, bladeSharpness - amount);
        OnSharpnessChanged?.Invoke(bladeSharpness);
        ApplyStatsToKnight();
        if (bladeSharpness <= 0f)
            Debug.Log("[PlayerStats] LÂMINA COMPLETAMENTE CEGA! Afie em um altar.");
    }

    // ====================================================================
    //  FADIGA
    // ====================================================================

    public void TriggerFatigue() {
        if (!isFatigued) StartCoroutine(FatigueRoutine());
    }

    private IEnumerator FatigueRoutine() {
        isFatigued = true;
        OnFatigueChanged?.Invoke(true);
        Debug.Log("[PlayerStats] EXAUSTO!");
        yield return new WaitForSeconds(fatigueDuration);
        isFatigued = false;
        OnFatigueChanged?.Invoke(false);
        Debug.Log("[PlayerStats] Recuperado da fadiga.");
    }

    // ====================================================================
    //  SACRIFÍCIO DE VIDA
    // ====================================================================

    public void SacrificeLife() {
        float knightMaxHP = player.MaxHealth;
        int soulsGained = 0;

        if (knightMaxHP > 40f) {
            player.ReduceMaxHealth(20f);
            soulsGained = 5000 + (currentClass == CharacterClass.Warrior ? 500 : 0);
        }
        else if (knightMaxHP > 25f) {
            player.ReduceMaxHealth(12f);
            soulsGained = 1500 + (currentClass == CharacterClass.Warrior ? 15 : 0);
        }
        else if (knightMaxHP > 15f) {
            player.ReduceMaxHealth(10f);
            soulsGained = 500 + (currentClass == CharacterClass.Warrior ? 15 : 0);
        }
        else {
            Debug.Log("[PlayerStats] Sacrifício não pode ser realizado — vida muito baixa.");
            return;
        }

        if (SoulManager.Instance != null)
            SoulManager.Instance.AddSouls(soulsGained);

        Debug.Log($"[PlayerStats] Sacrifício → +{soulsGained} almas.");
    }

    // ====================================================================
    //  AFIAR LÂMINA
    // ====================================================================

    public void SharpenBlade() {
        bladeSharpness = 100f;
        OnSharpnessChanged?.Invoke(bladeSharpness);
        ApplyStatsToKnight();
        Debug.Log("[PlayerStats] Espada afiada!");
    }

    // ====================================================================
    //  UPGRADES COM ALMAS
    // ====================================================================

    [Header("Custo de Upgrades")]
    [SerializeField] private int strengthUpgradeCost = 1000;

    public bool UpgradeStrength() {
        if (SoulManager.Instance == null) return false;
        if (!SoulManager.Instance.SpendSouls(strengthUpgradeCost)) return false;

        strength++;
        OnStrengthChanged?.Invoke(strength);
        ApplyStatsToKnight();
        Debug.Log($"[PlayerStats] Força → {strength}! Custo: {strengthUpgradeCost} almas.");
        return true;
    }
}