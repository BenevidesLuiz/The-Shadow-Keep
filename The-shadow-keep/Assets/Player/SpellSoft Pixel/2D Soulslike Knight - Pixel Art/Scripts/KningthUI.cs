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

    private void Start()
    {
        // Força a atualização inicial para as barras não começarem vazias
        if (knight != null)
        {
            // Como não temos acesso às variáveis privadas, chamamos os métodos de update
            // O ideal é que o SoulslikeKnight tenha propriedades públicas para Health e Stamina
            UpdateHealthBar(100, 100);
            UpdateStaminaBar(100, 100);
        }
    }

    private void OnEnable()
    {
        // Se inscreve nos eventos que já existem no seu script SoulslikeKnight
        if (knight != null)
        {
            knight.OnHealthChanged += UpdateHealthBar;
            knight.OnStaminaChanged += UpdateStaminaBar;
        }
    }

    private void OnDisable()
    {
        // Cancela a inscrição ao desativar para evitar erros
        if (knight != null)
        {
            knight.OnHealthChanged -= UpdateHealthBar;
            knight.OnStaminaChanged -= UpdateStaminaBar;
        }
    }

    // Este método é chamado automaticamente pelo Player quando a vida muda
    private void UpdateHealthBar(float current, float max)
    {
        if (healthFill == null) return;

        float pct = max > 0 ? current / max : 0f;
        healthFill.fillAmount = pct;

        // Lógica de cor dinâmica: fica mais escuro se a vida estiver crítica
        //healthFill.color = (pct <= criticalHealthPercent) ? healthColorCritical : healthColorNormal;
    }

    // Este método é chamado automaticamente pelo Player quando a estamina muda (esquiva/ataque)
    private void UpdateStaminaBar(float current, float max)
    {
        if (staminaFill == null) return;

        float pct = max > 0 ? current / max : 0f;
        staminaFill.fillAmount = pct;
    }
}