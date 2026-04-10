using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerData {
    // Almas
    public int souls;
    // Vida
    public float currentHealth;
    public float maxHealth;
    // Stamina
    public float currentStamina;
    public float maxStamina;
    // Estus
    public int currentEstusCharges;
    public int maxEstusCharges;
    // PlayerStats
    public int strength;
    public float bladeSharpness;
    public PlayerStats.CharacterClass characterClass;
    // Dano
    public float lightDamage;
    public float heavyDamage;
    // Posição
    public float[] position;

    public PlayerData(PlayerBase player, PlayerStats stats) {
        souls = SoulManager.Instance.CurrentSouls;
        currentHealth = player.CurrentHealth;
        maxHealth = player.MaxHealth;
        currentStamina = player.CurrentStamina;
        maxStamina = player.MaxStamina;
        currentEstusCharges = player.CurrentEstusCharges;
        maxEstusCharges = player.MaxEstusCharges;
        strength = stats.strength;
        bladeSharpness = stats.bladeSharpness;
        characterClass = stats.currentClass;
        lightDamage = player.GetLightDamage();
        heavyDamage = player.GetHeavyDamage();
        position = new float[] {
            player.transform.position.x,
            player.transform.position.y
        };
    }

}