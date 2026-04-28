using UnityEngine;
using UnityEngine.UI; // Necessário para acessar componentes de UI

public class PlayerAvatarUI : MonoBehaviour {
    [Header("Referência da UI")]
    [Tooltip("Arraste o objeto AvatarImage da sua hierarquia para cá.")]
    public Image avatarImage;

    [Header("Fotos (Sprites) das Classes")]
    public Sprite warriorSprite;
    public Sprite paladinSprite;

    // Opcional: Referência ao seu script PlayerStats para já mudar a classe lá também
    [Header("Referência do Jogador (Opcional)")]
    public PlayerStats playerStats;

    /// <summary>
    /// Chama este método no botão "ChooseWarrior"
    /// </summary>
    public void SelectWarrior() {
        // 1. Muda a foto na UI
        if (avatarImage != null && warriorSprite != null) {
            avatarImage.sprite = warriorSprite;
            avatarImage.enabled = true; // Garante que a imagem apareça
        }

        // 2. Opcional: Já altera a classe no PlayerStats
        if (playerStats != null) {
            playerStats.currentClass = PlayerStats.CharacterClass.Warrior;
            // Se precisar re-aplicar os status após a troca:
            // playerStats.ApplyStatsToKnight(); 
        }

        Debug.Log("[PlayerAvatar] Classe Guerreiro escolhida e avatar atualizado.");
    }

    /// <summary>
    /// Chama este método no botão "ChoosePaladin"
    /// </summary>
    public void SelectPaladin() {
        // 1. Muda a foto na UI
        if (avatarImage != null && paladinSprite != null) {
            avatarImage.sprite = paladinSprite;
            avatarImage.enabled = true;
        }

        // 2. Opcional: Já altera a classe no PlayerStats
        if (playerStats != null) {
            playerStats.currentClass = PlayerStats.CharacterClass.Paladin;
        }

        Debug.Log("[PlayerAvatar] Classe Paladino escolhida e avatar atualizado.");
    }
}