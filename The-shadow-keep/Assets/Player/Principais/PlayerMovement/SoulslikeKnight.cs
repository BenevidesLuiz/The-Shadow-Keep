using UnityEngine;

/// <summary>
/// SoulslikeKnight — Motor de inputs do jogador na cena.
/// Se adapta dinamicamente caso o save seja de um Paladino.
/// </summary>
public class SoulslikeKnight : PlayerBase {

    // CORRIGIDO: Usando 'protected override' para respeitar a herança do PlayerBase
    protected override void Start() {
        // Executa o fluxo de Novo Jogo / Carregamento da classe pai
        base.Start();
    }

    // CORRIGIDO: Usando 'protected override' para respeitar o método abstrato do PlayerBase
    protected override void ReadCombatInput() {
        bool fatigued = playerStats != null && playerStats.isFatigued;
        if (fatigued) return;

        // Inputs de ataque compartilhados
        if (Input.GetKeyDown(KeyCode.P)) TryAttackLight();
        if (Input.GetKeyDown(KeyCode.U)) TryAttackSpecial1();
        if (Input.GetKeyDown(KeyCode.I)) TryAttackSpecial2();

        // Se o status do PlayerStats indicar que este guerreiro é mecanicamente um Paladino:
        if (Input.GetKeyDown(KeyCode.R) && playerStats != null && playerStats.currentClass == PlayerStats.CharacterClass.Paladin) {
            RestoreStamina(MaxStamina);
            Debug.Log($"[Paladin-Skill] Bênção Divina executada! Stamina restaurada.");
        }
    }

    // Callback de dano para roubo de vida (ativado se for mecanicamente Paladino)
    public void OnHolyHit(float damageDealt) {
        if (playerStats != null && playerStats.currentClass == PlayerStats.CharacterClass.Paladin) {
            float heal = damageDealt * 0.20f;
            if (heal > 0f) {
                RestoreHealth(heal);
                Debug.Log($"[Paladin-Skill] Roubo de Vida Sagrado: +{heal:F1} de HP.");
            }
        }
    }
}