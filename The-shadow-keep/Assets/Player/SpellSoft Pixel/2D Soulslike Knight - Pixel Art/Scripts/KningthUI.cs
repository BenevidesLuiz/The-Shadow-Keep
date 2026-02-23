using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// KnightUI — Conecta a UI do Canvas ao SoulslikeKnight.
///
/// Como usar:
///   1. Adicione este script em um GameObject vazio dentro do Canvas (ex: "UIManager").
///   2. Arraste o objeto do cavaleiro para o campo "Knight".
///   3. Arraste os Sliders de HP e Stamina para os campos correspondentes.
///      (Se usar Image com Fill Amount, troque Slider por Image e ajuste o codigo abaixo)
/// </summary>
public class KnightUI : MonoBehaviour
{
    [Header("Referencia ao Cavaleiro")]
    [SerializeField] private SoulslikeKnight knight;

    [Header("Barra de Vida")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image healthFill;   // opcional: muda cor quando critico

    [Header("Barra de Stamina")]
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private Image staminaFill;  // opcional

    [Header("Cores")]
    [SerializeField] private Color healthColorNormal = new Color(0.18f, 0.72f, 0.18f);
    [SerializeField] private Color healthColorCritical = new Color(0.85f, 0.15f, 0.15f);
    [SerializeField] private Color staminaColorNormal = new Color(0.95f, 0.82f, 0.10f);
    [SerializeField] private Color staminaColorEmpty = new Color(0.45f, 0.45f, 0.45f);

    // Porcentagem abaixo da qual a barra de HP fica vermelha
    [SerializeField] private float criticalHealthPercent = 0.25f;

    private void OnEnable()
    {
        if (knight == null)
        {
            Debug.LogError("[KnightUI] Nenhum SoulslikeKnight atribuido no Inspector!");
            return;
        }

        knight.OnHealthChanged += UpdateHealthBar;
        knight.OnStaminaChanged += UpdateStaminaBar;
    }

    private void OnDisable()
    {
        if (knight == null) return;
        knight.OnHealthChanged -= UpdateHealthBar;
        knight.OnStaminaChanged -= UpdateStaminaBar;
    }

    private void Start()
    {
        // Inicializa as barras com valores maximos
        if (healthSlider != null) { healthSlider.minValue = 0; healthSlider.maxValue = 1; healthSlider.value = 1; }
        if (staminaSlider != null) { staminaSlider.minValue = 0; staminaSlider.maxValue = 1; staminaSlider.value = 1; }

        SetFillColor(healthFill, healthColorNormal);
        SetFillColor(staminaFill, staminaColorNormal);
    }

    // ------------------------------------------------------------------ //
    //  Callbacks dos eventos do cavaleiro
    // ------------------------------------------------------------------ //

    private void UpdateHealthBar(float current, float max)
    {
        float pct = max > 0 ? current / max : 0f;

        if (healthSlider != null)
            healthSlider.value = pct;

        // Muda a cor da barra ao ficar critico
        if (healthFill != null)
            SetFillColor(healthFill, pct <= criticalHealthPercent ? healthColorCritical : healthColorNormal);
    }

    private void UpdateStaminaBar(float current, float max)
    {
        float pct = max > 0 ? current / max : 0f;

        if (staminaSlider != null)
            staminaSlider.value = pct;

        if (staminaFill != null)
            SetFillColor(staminaFill, pct <= 0.05f ? staminaColorEmpty : staminaColorNormal);
    }

    // ------------------------------------------------------------------ //
    //  Auxiliar
    // ------------------------------------------------------------------ //

    private void SetFillColor(Image img, Color color)
    {
        if (img != null) img.color = color;
    }
}