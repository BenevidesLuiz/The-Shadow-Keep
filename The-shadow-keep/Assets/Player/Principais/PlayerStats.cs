using UnityEngine;
using System.Collections;

/// <summary>
/// PlayerStats v4 FINAL CORRIGIDO
/// 
/// FIX: Agora Awake() é chamado DEPOIS que Instantiate() termina,
/// garantindo que SetStatsFromMenuOrSave() já foi chamado antes de Start().
/// 
/// ORDEM CORRETA:
/// 1. CharacterCreationManager chama Instantiate(prefab)
/// 2. Awake() é executado → inicializa player
/// 3. SetStatsFromMenuOrSave() é chamado IMEDIATAMENTE pelo CharacterCreationManager
/// 4. SetStatsFromMenuOrSave() marca statsAlreadyLoaded = true
/// 5. Start() é executado e VÊ statsAlreadyLoaded = true
/// 6. Start() NÃO reseta os valores ✅
/// </summary>
[RequireComponent(typeof(PlayerBase))]
public class PlayerStats : MonoBehaviour {
    public enum CharacterClass { Warrior, Paladin }

    [Header("Identidade")]
    public CharacterClass currentClass = CharacterClass.Warrior;

    [Header("Atributos Base")]
    public int level = 1;
    public int strength = 10;
    public int faith = 10;

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
    private PlayerBase player;

    // ── Valores base (antes de modificadores) ────────────────────────────
    private float baseLightDamage;
    private float baseHeavyDamage;

    // 🔑 FLAG CRÍTICA: Se true, Start() NÃO reseta os valores
    private bool statsAlreadyLoaded = false;

    // ── Eventos ──────────────────────────────────────────────────────────
    public event System.Action<float> OnSharpnessChanged;
    public event System.Action<bool> OnFatigueChanged;
    public event System.Action<int> OnStrengthChanged;
    public event System.Action<int> OnLevelChanged;

    // ── Propriedades ─────────────────────────────────────────────────────
    public float DamageMultiplier => CalculateDamageMultiplier();
    public float BladeSharpness => bladeSharpness;
    public int Level => level;

    // ====================================================================
    private void Awake() {
        player = GetComponent<PlayerBase>();
        Debug.Log("[PlayerStats] Awake() chamado");
    }

    private void Start() {
        baseLightDamage = player.GetLightDamage();
        baseHeavyDamage = player.GetHeavyDamage();

        Debug.Log($"[PlayerStats] Start() chamado | statsAlreadyLoaded={statsAlreadyLoaded}");

        // 🔑 CHAVE: Se os stats foram carregados do menu, NÃO reseta
        if (!statsAlreadyLoaded) {
            Debug.Log("[PlayerStats] 📋 statsAlreadyLoaded = false → Configurando classe padrão");
            ConfigurarClasseInicial();
        }
        else {
            Debug.Log($"[PlayerStats] ✅ statsAlreadyLoaded = true → Mantendo valores do menu!");
            Debug.Log($"   Level: {level}");
            Debug.Log($"   Força: {strength}");
            Debug.Log($"   Fé: {faith}");
            ApplyStatsToKnight();
        }
    }

    // ====================================================================
    //  RECEBER DADOS DO MENU OU SAVE
    // ====================================================================
    public void SetStatsFromMenuOrSave(
        CharacterClass newClass,
        int newLevel,
        int newStrength,
        float newSharpness,
        float newMaxHealth,
        float newMaxStamina,
        int newFaith
    ) {
        Debug.Log("[PlayerStats] 📥 SetStatsFromMenuOrSave() chamado");

        currentClass = newClass;
        level = newLevel;
        strength = newStrength;
        bladeSharpness = newSharpness;
        faith = newFaith;

        Debug.Log($"   → Level: {level}");
        Debug.Log($"   → Força: {strength}");
        Debug.Log($"   → Fé: {faith}");
        Debug.Log($"   → Afiação: {bladeSharpness}");

        // Envia a Vida e a Estamina para o PlayerBase lidar com elas
        player.SetCoreStatsFromMenu(newMaxHealth, newMaxStamina);

        // 🔑 MARCA: Os stats foram carregados! Start() não vai resetar
        statsAlreadyLoaded = true;

        Debug.Log($"[PlayerStats] ✅ Stats do menu carregados com sucesso!");
    }

    // ====================================================================
    //  CONFIGURAÇÃO DE CLASSE PADRÃO (APENAS SE NÃO VEM DO MENU)
    // ====================================================================
    private void ConfigurarClasseInicial() {
        Debug.Log($"[PlayerStats] 🎮 ConfigurarClasseInicial() chamado para {currentClass}");

        switch (currentClass) {
            case CharacterClass.Warrior:
                level = 1;
                strength = 12;
                faith = 6;
                Debug.Log("   → Warrior padrão: Force 12, Fé 6");
                break;
            case CharacterClass.Paladin:
                level = 1;
                strength = 10;
                faith = 14;
                player.RestoreHealth(20f);
                Debug.Log("   → Paladin padrão: Force 10, Fé 14");
                break;
        }
        ApplyStatsToKnight();
    }

    // ====================================================================
    //  APLICAR STATS
    // ====================================================================
    public void ApplyStatsToKnight() {
        float mult = CalculateDamageMultiplier();
        player.SetLightDamage(baseLightDamage * mult);
        player.SetHeavyDamage(baseHeavyDamage * mult);
        Debug.Log($"[PlayerStats] 📊 Dano aplicado: Light={baseLightDamage * mult:F1} | Heavy={baseHeavyDamage * mult:F1}");
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

    public bool UpgradeLevel() {
        if (SoulManager.Instance == null) return false;
        if (!SoulManager.Instance.SpendSouls(strengthUpgradeCost)) return false;

        level++;
        OnLevelChanged?.Invoke(level);
        Debug.Log($"[PlayerStats] Level → {level}!");
        return true;
    }
}