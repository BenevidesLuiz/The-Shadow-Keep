using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement; 

[System.Serializable]
public class PlayerData {
    // PlayerBase
    public float currentHealth;
    public float maxHealth;
    public float currentStamina;
    public float maxStamina;
    public int currentEstusCharges;
    public int maxEstusCharges;
    public string playerName;

    // PlayerStats
    public int level;
    public int strength;
    public float bladeSharpness;
    public int faith;
    public PlayerStats.CharacterClass characterClass;

    // SoulManager
    public int souls;

    // Posição e Fase
    public float[] position;
    public string currentScene; 

    // Dano
    public float lightDamage;
    public float heavyDamage;

    public PlayerData() {
        position = new float[2];
    }

    public PlayerData(PlayerBase player, PlayerStats stats) {
        currentHealth = player.CurrentHealth;
        maxHealth = player.MaxHealth;
        currentStamina = player.CurrentStamina;
        maxStamina = player.MaxStamina;
        currentEstusCharges = player.CurrentEstusCharges;
        maxEstusCharges = player.MaxEstusCharges;

        level = stats.level;
        strength = stats.strength;
        bladeSharpness = stats.bladeSharpness;
        faith = stats.faith;
        characterClass = stats.currentClass;

        souls = (SoulManager.Instance != null) ? SoulManager.Instance.CurrentSouls : 0;

        lightDamage = player.GetLightDamage();
        heavyDamage = player.GetHeavyDamage();

        position = new float[] {
            player.transform.position.x,
            player.transform.position.y
        };

        currentScene = SceneManager.GetActiveScene().name;
    }
}