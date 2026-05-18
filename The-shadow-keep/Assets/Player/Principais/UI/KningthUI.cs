using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// KnightUI — Controla a exibição das barras de vida, stamina e mecânicas integradas.
/// </summary>
public class KnightUI : MonoBehaviour {

    [Header("Referência ao Personagem")]
    [SerializeField] private PlayerBase knight;
    [SerializeField] private PlayerStats playerStats;

    [Header("Barra de Vida")]
    [SerializeField] private Image healthFill;

    [Header("Barra de Stamina")]
    [SerializeField] private Image staminaFill;

    [Header("Estus / Pocao")]
    [SerializeField] private Image[] estusIcons;
    [SerializeField] private Text estusText;
    [SerializeField] private Color estusFullColor = Color.white;
    [SerializeField] private Color estusEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Almas")]
    [SerializeField] private Text soulsText;
    [SerializeField] private Image pendingSoulsIcon;
    [SerializeField] private Color pendingSoulsColor = new Color(0.8f, 0.5f, 0f);

    [Header("Lâmina (PlayerStats)")]
    [SerializeField] private Image bladeFill;
    [SerializeField] private Color bladeSharpColor = Color.cyan;
    [SerializeField] private Color bladeDullColor = new Color(0.6f, 0.3f, 0.1f);

    [Header("Fadiga")]
    [SerializeField] private GameObject fatigueIndicator;

    [Header("Bênção Divina (apenas Paladino — opcional)")]
    [Tooltip("Barra de cooldown da Bênção. Fica invisível se o jogador for Guerreiro puro.")]
    [SerializeField] private Image blessingCooldownFill;

    private void Start() {
        if (knight != null) {
            UpdateHealthBar(100, 100);
            UpdateStaminaBar(100, 100);
            UpdateEstus(knight.CurrentEstusCharges, knight.MaxEstusCharges);
        }

        if (SoulManager.Instance != null) UpdateSouls(SoulManager.Instance.CurrentSouls);
        if (playerStats != null) UpdateBlade(playerStats.BladeSharpness);
        if (fatigueIndicator != null) fatigueIndicator.SetActive(false);
        if (pendingSoulsIcon != null) pendingSoulsIcon.gameObject.SetActive(false);

        // Se o jogador não for mecanicamente um Paladino, esconde a barra de cooldown do 'R'
        if (blessingCooldownFill != null) {
            blessingCooldownFill.fillAmount = 0f;
            if (playerStats != null && playerStats.currentClass != PlayerStats.CharacterClass.Paladin) {
                blessingCooldownFill.gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable() {
        if (knight != null) {
            knight.OnHealthChanged += UpdateHealthBar;
            knight.OnStaminaChanged += UpdateStaminaBar;
            knight.OnEstusChanged += UpdateEstus;
        }

        if (SoulManager.Instance != null) {
            SoulManager.Instance.OnSoulsChanged += UpdateSouls;
            SoulManager.Instance.OnSoulsLost += OnSoulsLost;
            SoulManager.Instance.OnSoulsRecovered += OnSoulsRecovered;
        }

        if (playerStats != null) {
            playerStats.OnSharpnessChanged += UpdateBlade;
            playerStats.OnFatigueChanged += UpdateFatigue;
        }
    }

    private void OnDisable() {
        if (knight != null) {
            knight.OnHealthChanged -= UpdateHealthBar;
            knight.OnStaminaChanged -= UpdateStaminaBar;
            knight.OnEstusChanged -= UpdateEstus;
        }

        if (SoulManager.Instance != null) {
            SoulManager.Instance.OnSoulsChanged -= UpdateSouls;
            SoulManager.Instance.OnSoulsLost -= OnSoulsLost;
            SoulManager.Instance.OnSoulsRecovered -= OnSoulsRecovered;
        }

        if (playerStats != null) {
            playerStats.OnSharpnessChanged -= UpdateBlade;
            playerStats.OnFatigueChanged -= UpdateFatigue;
        }
    }

    // ==================================================================
    //  UPDATES DE UI
    // ==================================================================

    private void UpdateHealthBar(float current, float max) {
        if (healthFill != null) healthFill.fillAmount = max > 0 ? current / max : 0f;
    }

    private void UpdateStaminaBar(float current, float max) {
        if (staminaFill != null) staminaFill.fillAmount = max > 0 ? current / max : 0f;
    }

    private void UpdateEstus(int current, int max) {
        if (estusIcons != null)
            for (int i = 0; i < estusIcons.Length; i++)
                if (estusIcons[i] != null)
                    estusIcons[i].color = (i < current) ? estusFullColor : estusEmptyColor;

        if (estusText != null) estusText.text = $"{current} / {max}";
    }

    private void UpdateSouls(int current) {
        if (soulsText != null) soulsText.text = current.ToString("N0");
    }

    private void OnSoulsLost(int amount, UnityEngine.Vector3 _) {
        if (pendingSoulsIcon != null) {
            pendingSoulsIcon.gameObject.SetActive(true);
            pendingSoulsIcon.color = pendingSoulsColor;
        }
        if (soulsText != null) soulsText.text = "0";
    }

    private void OnSoulsRecovered(int amount) {
        if (pendingSoulsIcon != null) pendingSoulsIcon.gameObject.SetActive(false);
    }

    private void UpdateBlade(float sharpness) {
        if (bladeFill == null) return;
        bladeFill.fillAmount = sharpness / 100f;
        bladeFill.color = Color.Lerp(bladeDullColor, bladeSharpColor, sharpness / 100f);
    }

    private void UpdateFatigue(bool fatigued) {
        if (fatigueIndicator != null) fatigueIndicator.SetActive(fatigued);
    }
}