using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerBase))]
public class PlayerStats : MonoBehaviour {
    public enum CharacterClass { Warrior, Paladin }

    [Header("Identidade")]
    public CharacterClass currentClass = CharacterClass.Warrior;

    [Header("Atributos Base")]
    public int level = 1;
    public int strength = 10;
    public int faith = 10;
    public int vitality = 10;
    public int stamina = 10;

    [Header("Mecânica: Lâmina Cega")]
    [Tooltip("100 = Afiada, 0 = Completamente Cega")]
    [Range(0f, 100f)]
    public float bladeSharpness = 100f;

    [SerializeField] private float sharpnessCostPerAttack = 0.000000002f;

    [Header("Fadiga")]
    [Tooltip("Se verdadeiro, o knight fica temporariamente sem stamina")]
    public bool isFatigued = false;
    [SerializeField] private float fatigueDuration = 3f;

    private PlayerBase player;
    private float baseLightDamage;
    private float baseHeavyDamage;

    public bool statsAlreadyLoaded = false;

    public event System.Action<float> OnSharpnessChanged;
    public event System.Action<bool> OnFatigueChanged;
    public event System.Action<int> OnStrengthChanged;
    public event System.Action<int> OnLevelChanged;

    public float DamageMultiplier => CalculateDamageMultiplier();
    public float BladeSharpness => bladeSharpness;
    public int Level => level;

    private void Awake() {
        player = GetComponent<PlayerBase>();
        baseLightDamage = player.GetLightDamage();
        baseHeavyDamage = player.GetHeavyDamage();
    }

    private void Start() {
        if (!statsAlreadyLoaded) {
            ConfigurarClasseInicial();
        }
        else {
            ApplyStatsToKnight();
        }
    }

    public void SetStatsFromMenuOrSave(
        CharacterClass newClass,
        int newLevel,
        int newStrength,
        float newSharpness,
        float newMaxHealth,
        float newMaxStamina,
        int newFaith
    ) {
        currentClass = newClass;
        level = newLevel;
        strength = newStrength;
        bladeSharpness = newSharpness;
        faith = newFaith;
        player.SetCoreStatsFromMenu(newMaxHealth, newMaxStamina);
        statsAlreadyLoaded = true;
    }

    private void ConfigurarClasseInicial() {
        switch (currentClass) {
            case CharacterClass.Warrior:
                level = 1;
                strength = 12;
                faith = 6;
                break;
            case CharacterClass.Paladin:
                level = 1;
                strength = 10;
                faith = 14;
                player.RestoreHealth(20f);
                break;
        }
        ApplyStatsToKnight();
    }

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

    public void OnAttackPerformed() => WearBlade(sharpnessCostPerAttack);

    private void WearBlade(float amount) {
        float desgasteReal = amount / 20f;
        bladeSharpness = Mathf.Max(0f, bladeSharpness - desgasteReal);
        OnSharpnessChanged?.Invoke(bladeSharpness);
        ApplyStatsToKnight();
    }

    public void TriggerFatigue() {
        if (!isFatigued) StartCoroutine(FatigueRoutine());
    }

    private IEnumerator FatigueRoutine() {
        isFatigued = true;
        OnFatigueChanged?.Invoke(true);
        yield return new WaitForSeconds(fatigueDuration);
        isFatigued = false;
        OnFatigueChanged?.Invoke(false);
    }

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
            return;
        }

        if (SoulManager.Instance != null)
            SoulManager.Instance.AddSouls(soulsGained);
    }

    public void SharpenBlade() {
        bladeSharpness = 100f;
        OnSharpnessChanged?.Invoke(bladeSharpness);
        ApplyStatsToKnight();
    }

    [Header("Custo de Upgrades")]
    [SerializeField] private int strengthUpgradeCost = 1000;

    public bool UpgradeStrength() {
        if (SoulManager.Instance == null) return false;
        if (!SoulManager.Instance.SpendSouls(strengthUpgradeCost)) return false;

        strength++;
        OnStrengthChanged?.Invoke(strength);
        ApplyStatsToKnight();
        return true;
    }

    public bool UpgradeLevel() {
        if (SoulManager.Instance == null) return false;
        if (!SoulManager.Instance.SpendSouls(strengthUpgradeCost)) return false;

        level++;
        OnLevelChanged?.Invoke(level);
        return true;
    }

    public void UpdateMaxStats() {
        PlayerBase pb = GetComponent<PlayerBase>();
        if (pb != null) {
            float novaVidaMax = CalcularHPDarkSouls(vitality);
            float novaStaminaMax = 50f;
            for (int i = 1; i <= stamina; i++) {
                if (i <= 40) novaStaminaMax += 2.5f;
            }
            pb.SetCoreStatsFromMenu(novaVidaMax, novaStaminaMax);
        }
    }

    public float CalcularHPDarkSouls(int vit) {
        float hpBase = 400f;
        for (int i = 1; i <= vit; i++) {
            if (i <= 15) {
                hpBase += 15f;
            }
            else if (i <= 30) {
                hpBase += 25f;
            }
            else if (i <= 50) {
                hpBase += 18f;
            }
            else {
                hpBase += 8f;
            }
        }
        return hpBase;
    }
}