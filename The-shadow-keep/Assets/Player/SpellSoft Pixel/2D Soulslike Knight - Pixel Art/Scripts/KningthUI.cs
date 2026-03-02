using UnityEngine;
using UnityEngine.UI;

public class KnightUI : MonoBehaviour
{
    [Header("Referência ao Cavaleiro")]
    [SerializeField] private SoulslikeKnight knight;

    [Header("Barra de Vida")]
    [SerializeField] private Image healthFill;

    [Header("Barra de Stamina")]
    [SerializeField] private Image staminaFill;

    // ------------------------------------------------------------------ //
    //  ESTUS — Dark Souls: ícones de frasco na tela
    //  Opcao A: usar ícones individuais (arraste cada Image no Inspector)
    //  Opcao B: usar um texto simples (ex: "3 / 5")
    // ------------------------------------------------------------------ //
    [Header("Estus / Pocao")]
    [Tooltip("Opcao A: arraste cada icone de frasco aqui em ordem")]
    [SerializeField] private Image[] estusIcons;

    [Tooltip("Opcao B: texto simples '3 / 5' — pode usar junto ou separado")]
    [SerializeField] private Text estusText;

    [Tooltip("Cor do icone quando o frasco ESTA disponivel")]
    [SerializeField] private Color estusFullColor = Color.white;

    [Tooltip("Cor do icone quando o frasco FOI usado (vazio)")]
    [SerializeField] private Color estusEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    // ==================================================================== //
    //  UNITY CALLBACKS
    // ==================================================================== //

    private void Start()
    {
        if (knight == null) return;
        UpdateHealthBar(100, 100);
        UpdateStaminaBar(100, 100);
        // Inicializa Estus com valores reais do knight
        UpdateEstus(knight.CurrentEstusCharges, knight.MaxEstusCharges);
    }

    private void OnEnable()
    {
        if (knight == null) return;
        knight.OnHealthChanged += UpdateHealthBar;
        knight.OnStaminaChanged += UpdateStaminaBar;
        knight.OnEstusChanged += UpdateEstus;      // novo
    }

    private void OnDisable()
    {
        if (knight == null) return;
        knight.OnHealthChanged -= UpdateHealthBar;
        knight.OnStaminaChanged -= UpdateStaminaBar;
        knight.OnEstusChanged -= UpdateEstus;      // novo
    }

    // ==================================================================== //
    //  UPDATES DE UI
    // ==================================================================== //

    private void UpdateHealthBar(float current, float max)
    {
        if (healthFill == null) return;
        healthFill.fillAmount = max > 0 ? current / max : 0f;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaFill == null) return;
        staminaFill.fillAmount = max > 0 ? current / max : 0f;
    }

    /// <summary>
    /// Atualiza os ícones de Estus.
    /// Ícones até 'current' ficam brancos; os restantes ficam escuros (vazios).
    /// </summary>
    private void UpdateEstus(int current, int max)
    {
        // Opcao A: ícones individuais
        if (estusIcons != null)
        {
            for (int i = 0; i < estusIcons.Length; i++)
            {
                if (estusIcons[i] == null) continue;
                estusIcons[i].color = (i < current) ? estusFullColor : estusEmptyColor;
            }
        }

        // Opcao B: texto simples
        if (estusText != null)
            estusText.text = $"{current} / {max}";
    }
}