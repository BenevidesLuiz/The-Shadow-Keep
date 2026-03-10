using UnityEngine;
using System.Collections;

/// <summary>
/// PlayerStats v2 — Integrada com SoulslikeKnight e SoulManager.
///
/// MUDANÇAS:
///   - Classe (Warrior/Paladin) afeta dano de ataque diretamente no Knight
///   - BladeSharpness reduz dano conforme a lâmina enfraquece
///   - Sacrifício de vida usa sistema de almas para recompensa
///   - Força (strength) escala o dano do Knight automaticamente
///   - Fadiga bloqueia ações do Knight via evento
///
/// SETUP:
///   - Coloque este script no mesmo GameObject que o SoulslikeKnight
///   - Ele lê e modifica os valores de dano do Knight em runtime
/// </summary>
[RequireComponent(typeof(SoulslikeKnight))]
public class PlayerStats : MonoBehaviour
{
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
    private SoulslikeKnight knight;

    // ── Valores base (antes de modificadores) ────────────────────────────
    private float baseLightDamage;
    private float baseHeavyDamage;

    // ── Eventos ──────────────────────────────────────────────────────────
    public event System.Action<float> OnSharpnessChanged;    // (0–100)
    public event System.Action<bool> OnFatigueChanged;      // (isFatigued)
    public event System.Action<int> OnStrengthChanged;

    // ── Propriedades ─────────────────────────────────────────────────────
    public float DamageMultiplier => CalculateDamageMultiplier();
    public float BladeSharpness => bladeSharpness;

    // ====================================================================
    private void Awake()
    {
        knight = GetComponent<SoulslikeKnight>();
    }

    private void Start()
    {
        // Salva os danos base configurados no Knight pelo designer
        baseLightDamage = knight.GetLightDamage();
        baseHeavyDamage = knight.GetHeavyDamage();

        ConfigurarClasseInicial();
    }

    // ====================================================================
    //  CONFIGURAÇÃO DE CLASSE
    // ====================================================================

    private void ConfigurarClasseInicial()
    {
        switch (currentClass)
        {
            case CharacterClass.Warrior:
                strength = 10;
                break;
            case CharacterClass.Paladin:
                strength = 8;
                // Paladino começa com mais vida (comunica ao Knight via RestoreHealth)
                knight.RestoreHealth(20f);
                break;
        }

        ApplyStatsToKnight();
        Debug.Log($"[PlayerStats] Classe: {currentClass} | Força: {strength} | Dano base: {baseLightDamage}/{baseHeavyDamage}");
    }

    // ====================================================================
    //  APLICAR STATS NO KNIGHT
    // ====================================================================

    /// <summary>
    /// Recalcula e aplica modificadores de dano no Knight.
    /// Chamado sempre que force, sharpness ou classe mudam.
    /// </summary>
    public void ApplyStatsToKnight()
    {
        float mult = CalculateDamageMultiplier();
        knight.SetLightDamage(baseLightDamage * mult);
        knight.SetHeavyDamage(baseHeavyDamage * mult);
    }

    private float CalculateDamageMultiplier()
    {
        // Multiplicador de força: cada ponto acima de 10 dá +5% de dano
        float strengthMult = 1f + (strength - 10) * 0.05f;
        strengthMult = Mathf.Max(0.5f, strengthMult);

        // Lâmina cega: abaixo de 30% reduz dano em 50%
        float sharpnessMult = bladeSharpness < 30f ? 0.5f : 1f;

        // Bônus de classe
        float classMult = currentClass == CharacterClass.Warrior ? 1.1f : 1f;

        return strengthMult * sharpnessMult * classMult;
    }

    // ====================================================================
    //  GASTAR STAMINA (com lâmina e fadiga)
    // ====================================================================

    /// <summary>
    /// Chamado pelo Knight ao realizar ataques pesados/leves.
    /// Desgasta a lâmina e verifica fadiga.
    /// </summary>
    public void OnAttackPerformed()
    {
        WearBlade(sharpnessCostPerAttack);
    }

    private void WearBlade(float amount)
    {
        bladeSharpness = Mathf.Max(0f, bladeSharpness - amount);
        OnSharpnessChanged?.Invoke(bladeSharpness);
        ApplyStatsToKnight(); // recalcula dano imediatamente

        if (bladeSharpness <= 0f)
            Debug.Log("[PlayerStats] LÂMINA COMPLETAMENTE CEGA! Afie em um altar.");
    }

    // ====================================================================
    //  FADIGA
    // ====================================================================

    public void TriggerFatigue()
    {
        if (!isFatigued) StartCoroutine(FatigueRoutine());
    }

    private IEnumerator FatigueRoutine()
    {
        isFatigued = true;
        OnFatigueChanged?.Invoke(true);
        Debug.Log("[PlayerStats] EXAUSTO! Aguarde a recuperação.");

        yield return new WaitForSeconds(fatigueDuration);

        isFatigued = false;
        OnFatigueChanged?.Invoke(false);
        Debug.Log("[PlayerStats] Recuperado da fadiga.");
    }

    // ====================================================================
    //  SACRIFÍCIO DE VIDA
    // ====================================================================

    /// <summary>
    /// Sacrifica vida em troca de Almas.
    /// Usa SoulManager para adicionar as almas ganhas.
    /// </summary>
    public void SacrificeLife()
    {
        // Pega vida máxima atual do Knight
        float knightMaxHP = knight.MaxHealth;

        int soulsGained = 0;

        if (knightMaxHP > 40f)
        {
            knight.ReduceMaxHealth(20f);
            soulsGained = 5000 + (currentClass == CharacterClass.Warrior ? 500 : 0);
        }
        else if (knightMaxHP > 25f)
        {
            knight.ReduceMaxHealth(12f);
            soulsGained = 1500 + (currentClass == CharacterClass.Warrior ? 15 : 0);
        }
        else if (knightMaxHP > 15f)
        {
            knight.ReduceMaxHealth(10f);
            soulsGained = 500 + (currentClass == CharacterClass.Warrior ? 15 : 0);
        }
        else
        {
            Debug.Log("[PlayerStats] Sacrifício não pode ser realizado — vida muito baixa.");
            return;
        }

        if (SoulManager.Instance != null)
            SoulManager.Instance.AddSouls(soulsGained);

        Debug.Log($"[PlayerStats] Sacrifício realizado! -{(knightMaxHP - knight.MaxHealth):F0} HP máx → +{soulsGained} almas.");
    }

    // ====================================================================
    //  AFIAR LÂMINA
    // ====================================================================

    /// <summary>
    /// Restaura a lâmina. Chamar em altares ou ao usar item.
    /// </summary>
    public void SharpenBlade()
    {
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

    public bool UpgradeStrength()
    {
        if (SoulManager.Instance == null) return false;
        if (!SoulManager.Instance.SpendSouls(strengthUpgradeCost)) return false;

        strength++;
        OnStrengthChanged?.Invoke(strength);
        ApplyStatsToKnight();

        Debug.Log($"[PlayerStats] Força aumentada para {strength}! Custo: {strengthUpgradeCost} almas.");
        return true;
    }
}