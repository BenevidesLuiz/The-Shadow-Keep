using UnityEngine;

/// <summary>
/// SoulslikeKnight v7 — Herda de PlayerBase.
///
/// RESPONSABILIDADE ÚNICA:
///   - Input de ataque leve (P) e pesado (U)
///   - Dispara triggers "attack" e "special" no Animator
///   - Mantém compatibilidade total com PlayerHitbox, PlayerStats e KnightUI
///
/// Toda a lógica de movimento, vida, stamina, roll, bloqueio e morte
/// está em PlayerBase — não duplique aqui.
///
/// SETUP NO UNITY: igual ao anterior. Troque o script antigo por este.
/// </summary>
public class SoulslikeKnight : PlayerBase {

    // ================================================================== //
    //  INPUT DE COMBATE  (implementa o método abstrato de PlayerBase)
    // ================================================================== //

    protected override void ReadCombatInput() {
        bool fatigued = playerStats != null && playerStats.isFatigued;
        if (fatigued) return;

        if (Input.GetKeyDown(KeyCode.P)) TryAttackLight();
        if (Input.GetKeyDown(KeyCode.U)) TryAttackSpecial1();
        if (Input.GetKeyDown(KeyCode.I)) TryAttackSpecial2();
    }
}