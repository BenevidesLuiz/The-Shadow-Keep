using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// KnightUI v2 — Integrada com SoulManager e PlayerStats.
///
/// NOVIDADES:
///   - Contador de Almas (conecta ao SoulManager via evento)
///   - Barra de Lâmina (conecta ao PlayerStats via evento)
///   - Ícone de Fadiga (vermelho quando exausto)
///   - Indicador de almas perdidas (pisca quando há orbe de recuperação)
/// </summary>
public class KnightUI : MonoBehaviour
{
    [Header("Referência ao Cavaleiro")]
    [SerializeField] private SoulslikeKnight knight;
    [SerializeField] private PlayerStats playerStats; 

    [Header("Barra de Vida")]
    [SerializeField] private Image healthFill;

    [Header("Barra de Stamina")]
    [SerializeField] private Image staminaFill;

    // ── Estus ──────────────────────────────────────────────────────────
    [Header("Estus / Pocao")]
    [SerializeField] private Image[] estusIcons;
    [SerializeField] private Text estusText;
    [SerializeField] private Color estusFullColor = Color.white;
    [SerializeField] private Color estusEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    // ── Almas ──────────────────────────────────────────────────────────
    [Header("Almas")]
    [Tooltip("Texto que exibe a quantidade de almas (ex: '1.500')")]
    [SerializeField] private Text soulsText;
    [Tooltip("Ícone que pisca quando há almas perdidas para recuperar")]
    [SerializeField] private Image pendingSoulsIcon;
    [SerializeField] private Color pendingSoulsColor = new Color(0.8f, 0.5f, 0f);

    // ── Lâmina ─────────────────────────────────────────────────────────
    [Header("Lâmina (PlayerStats)")]
    [SerializeField] private Image bladeFill;
    [SerializeField] private Color bladeSharpColor = Color.cyan;
    [SerializeField] private Color bladeDullColor = new Color(0.6f, 0.3f, 0.1f);

    // ── Fadiga ──────────────────────────────────────────────────────────
    [Header("Fadiga")]
    [Tooltip("Ícone ou painel que aparece quando o player está exausto")]
    [SerializeField] private GameObject fatigueIndicator;

    // ==================================================================== //
    //  UNITY CALLBACKS
    // ==================================================================== //

    private void Start()
    {
        if (knight != null)
        {
            UpdateHealthBar(100, 100);
            UpdateStaminaBar(100, 100);
            UpdateEstus(knight.CurrentEstusCharges, knight.MaxEstusCharges);
        }

        // Inicializa almas
        if (SoulManager.Instance != null)
            UpdateSouls(SoulManager.Instance.CurrentSouls);

        // Inicializa lâmina
        if (playerStats != null)
            UpdateBlade(playerStats.BladeSharpness);

        if (fatigueIndicator != null)
            fatigueIndicator.SetActive(false);

        if (pendingSoulsIcon != null)
            pendingSoulsIcon.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (knight != null)
        {
            knight.OnHealthChanged += UpdateHealthBar;
            knight.OnStaminaChanged += UpdateStaminaBar;
            knight.OnEstusChanged += UpdateEstus;
        }

        if (SoulManager.Instance != null)
        {
            SoulManager.Instance.OnSoulsChanged += UpdateSouls;
            SoulManager.Instance.OnSoulsLost += OnSoulsLost;
            SoulManager.Instance.OnSoulsRecovered += OnSoulsRecovered;
        }

        if (playerStats != null)
        {
            playerStats.OnSharpnessChanged += UpdateBlade;
            playerStats.OnFatigueChanged += UpdateFatigue;
        }
    }

    private void OnDisable()
    {
        if (knight != null)
        {
            knight.OnHealthChanged -= UpdateHealthBar;
            knight.OnStaminaChanged -= UpdateStaminaBar;
            knight.OnEstusChanged -= UpdateEstus;
        }

        if (SoulManager.Instance != null)
        {
            SoulManager.Instance.OnSoulsChanged -= UpdateSouls;
            SoulManager.Instance.OnSoulsLost -= OnSoulsLost;
            SoulManager.Instance.OnSoulsRecovered -= OnSoulsRecovered;
        }

        if (playerStats != null)
        {
            playerStats.OnSharpnessChanged -= UpdateBlade;
            playerStats.OnFatigueChanged -= UpdateFatigue;
        }
    }

    // ==================================================================== //
    //  UPDATES DE UI
    // ==================================================================== //

    private void UpdateHealthBar(float current, float max)
    {
        if (healthFill != null)
            healthFill.fillAmount = max > 0 ? current / max : 0f;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaFill != null)
            staminaFill.fillAmount = max > 0 ? current / max : 0f;
    }

    private void UpdateEstus(int current, int max)
    {
        if (estusIcons != null)
            for (int i = 0; i < estusIcons.Length; i++)
                if (estusIcons[i] != null)
                    estusIcons[i].color = (i < current) ? estusFullColor : estusEmptyColor;

        if (estusText != null)
            estusText.text = $"{current} / {max}";
    }

    // ── Almas ──────────────────────────────────────────────────────────

    private void UpdateSouls(int current)
    {
        if (soulsText != null)
            soulsText.text = current.ToString("N0"); // ex: "1.500"
    }

    private void OnSoulsLost(int amount, UnityEngine.Vector3 _)
    {
        // Mostra ícone piscante de almas pendentes
        if (pendingSoulsIcon != null)
        {
            pendingSoulsIcon.gameObject.SetActive(true);
            pendingSoulsIcon.color = pendingSoulsColor;
        }

        if (soulsText != null)
            soulsText.text = "0";
    }

    private void OnSoulsRecovered(int amount)
    {
        if (pendingSoulsIcon != null)
            pendingSoulsIcon.gameObject.SetActive(false);
    }

    // ── Lâmina ─────────────────────────────────────────────────────────

    private void UpdateBlade(float sharpness)
    {
        if (bladeFill == null) return;

        bladeFill.fillAmount = sharpness / 100f;

        // Cor interpolada: afiada = cyan, cega = marrom
        float t = sharpness / 100f;
        bladeFill.color = Color.Lerp(bladeDullColor, bladeSharpColor, t);
    }

    // ── Fadiga ──────────────────────────────────────────────────────────

    private void UpdateFatigue(bool fatigued)
    {
        if (fatigueIndicator != null)
            fatigueIndicator.SetActive(fatigued);
    }
}