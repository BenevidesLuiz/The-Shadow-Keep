using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy3 — Inimigo amaldiçoado. Herda de Enemy.
///
/// MECÂNICA ESPECIAL:
/// ┌─────────────────────────────────────────────────────────────────────┐
/// │  GUERREIRO (SoulslikeKnight)                                        │
/// │    • Só causa dano enquanto o efeito do Estus estiver ativo         │
/// │    • Atacar sem Estus ativo → dano é REFLETIDO de volta no guerreiro│
/// │                                                                     │
/// │  PALADINO (PaladinKnight)                                           │
/// │    • Causa dano normalmente, sem restrição                          │
/// │    • A luz sagrada do Paladino ignora a maldição                    │
/// └─────────────────────────────────────────────────────────────────────┘
///
/// SETUP NO UNITY:
///   1. Crie um prefab Enemy3 baseado em qualquer Enemy existente
///   2. Troque o script Enemy por Enemy3
///   3. Configure os campos herdados normalmente no Inspector
///   4. Opcionalmente arraste um VFX de escudo em curseShieldVFX
///      e um VFX de reflexão em reflectVFX
/// </summary>
public class Enemy3 : Enemy {

    [Header("Maldição — Enemy3")]
    [Tooltip("Multiplicador do dano refletido de volta ao Guerreiro (1 = mesmo dano, 2 = dobro)")]
    [SerializeField] private float reflectMultiplier = 1f;

    [Tooltip("VFX ativado enquanto o escudo amaldiçoado está ativo (opcional)")]
    [SerializeField] private GameObject curseShieldVFX;

    [Tooltip("VFX instanciado no Guerreiro ao refletir dano (opcional)")]
    [SerializeField] private GameObject reflectVFX;

    [Tooltip("Duração mínima que o Estus precisa estar ativo para o Guerreiro causar dano")]
    [SerializeField] private float estusWindowMin = 0.1f;

    // ================================================================== //
    //  OVERRIDE DE TakeDamage
    // ================================================================== //

    public override void TakeDamage(float amount, Vector2 knockback = default) {
        // Paladino → passa direto, sem restrição
        if (playerKnight is PaladinKnight) {
            base.TakeDamage(amount, knockback);
            return;
        }

        // Guerreiro → verifica se o Estus está ativo
        if (playerKnight is SoulslikeKnight) {
            if (playerKnight.IsHealing) {
                // Estus ativo: causa dano normalmente
                base.TakeDamage(amount, knockback);
                ShowShield(false);
            }
            else {
                // Sem Estus: reflete o dano de volta
                ReflectDamage(amount);
            }
            return;
        }

        // Fallback para qualquer outro PlayerBase desconhecido: bloqueia o dano
        ReflectDamage(amount);
    }

    // ================================================================== //
    //  REFLEXÃO DE DANO
    // ================================================================== //

    private void ReflectDamage(float originalAmount) {
        if (playerKnight == null) return;

        float reflectedDamage = originalAmount * reflectMultiplier;

        // VFX de reflexão no player
        if (reflectVFX != null)
            Instantiate(reflectVFX, playerKnight.transform.position, Quaternion.identity);

        // Aplica o dano refletido no guerreiro
        playerKnight.TakeDamage(reflectedDamage);

        Debug.Log($"[Enemy3] Dano refletido! {reflectedDamage} de volta ao Guerreiro.");

        // Ativa feedback visual do escudo
        ShowShield(true);
    }

    // ================================================================== //
    //  FEEDBACK VISUAL DO ESCUDO
    // ================================================================== //

    private void ShowShield(bool active) {
        if (curseShieldVFX != null)
            curseShieldVFX.SetActive(active);
    }

    // ================================================================== //
    //  UNITY CALLBACKS
    // ================================================================== //

    protected override void Start() {
        base.Start();  // inicializa tudo do Enemy normal
        ShowShield(true);  // começa com escudo ativo
    }
}