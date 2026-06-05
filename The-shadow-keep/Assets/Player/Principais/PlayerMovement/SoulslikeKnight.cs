using UnityEngine;
using System.Collections; 

/// <summary>
/// SoulslikeKnight — Motor de inputs do jogador na cena.
/// Se adapta dinamicamente caso o save seja de um Paladino.
/// </summary>
public class SoulslikeKnight : PlayerBase {

    [Header("Referências de Hitbox")]
    public GameObject hitboxArm; 
    protected override void Start() {
        base.Start();
        if (hitboxArm != null) hitboxArm.SetActive(false);
    }

    protected override void ReadCombatInput() {
        bool fatigued = playerStats != null && playerStats.isFatigued;
        if (fatigued) return;

        if (Input.GetKeyDown(KeyCode.P)) {
            if (TryAttackLight()) {
                StartCoroutine(RotinaHitboxEspada(hitboxArm, 0.15f, 0.2f));
            }
        }

        if (Input.GetKeyDown(KeyCode.U)) {
            if (TryAttackSpecial1()) {
                StartCoroutine(RotinaHitboxEspada(hitboxArm, 0.25f, 0.25f));
            }
        }

        if (Input.GetKeyDown(KeyCode.I)) {
            if (TryAttackSpecial2()) {
                StartCoroutine(RotinaHitboxEspada(hitboxArm, 0.25f, 0.25f));
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && playerStats != null && playerStats.currentClass == PlayerStats.CharacterClass.Paladin) {
            RestoreStamina(MaxStamina);
            Debug.Log($"[Paladin-Skill] Bênção Divina executada! Stamina restaurada.");
        }
    }

    private IEnumerator RotinaHitboxEspada(GameObject hitbox, float tempoEspera, float tempoAtiva) {
        if (hitbox == null) yield break;

        // Espera o tempo do frame da espadada chegar para a frente
        yield return new WaitForSeconds(tempoEspera);
        hitbox.SetActive(true);

        // Tempo que o corte fica ativo dando dano
        yield return new WaitForSeconds(tempoAtiva);
        hitbox.SetActive(false);
    }

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