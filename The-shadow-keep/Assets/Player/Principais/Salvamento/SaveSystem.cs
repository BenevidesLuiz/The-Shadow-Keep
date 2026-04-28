using UnityEngine;

/// <summary>
/// SaveSystem — Sistema de múltiplos slots (Slot 1, 2 e 3)
/// </summary>
public static class SaveSystem {

    // O jogo vai usar essa variável internamente para saber em qual slot estamos jogando agora
    public static int CurrentSlot = 1;

    // Cria a chave do arquivo dinamicamente (Ex: "PlayerSaveData_1")
    private static string GetSaveKey(int slot) {
        return $"PlayerSaveData_{slot}";
    }

    /// <summary>
    /// Salva o jogador no Slot atual (CurrentSlot)
    /// </summary>
    public static void SavePlayer(PlayerBase player, PlayerStats stats) {
        if (player == null || stats == null) {
            Debug.LogError("[SaveSystem] Não é possível salvar: PlayerBase ou PlayerStats é null!");
            return;
        }

        PlayerData data = new PlayerData(player, stats);
        string json = JsonUtility.ToJson(data, true);

        // Salva na gaveta do Slot atual
        string key = GetSaveKey(CurrentSlot);
        PlayerPrefs.SetString(key, json);

        // Salva uma lembrança de qual foi o último slot jogado (para o botão Continuar)
        PlayerPrefs.SetInt("LastPlayedSlot", CurrentSlot);

        PlayerPrefs.Save();

        Debug.Log($"[SaveSystem] ✅ Jogo salvo no Slot {CurrentSlot}: Level {data.level}, HP {data.currentHealth}");
    }

    /// <summary>
    /// Carrega o jogador do Slot atual (usado durante o gameplay)
    /// </summary>
    public static PlayerData LoadPlayer() {
        return LoadPlayer(CurrentSlot);
    }

    /// <summary>
    /// Carrega os dados de um Slot ESPECÍFICO (Usado no Menu para mostrar a lista de saves)
    /// </summary>
    public static PlayerData LoadPlayer(int slot) {
        string key = GetSaveKey(slot);

        if (!PlayerPrefs.HasKey(key)) {
            return null; // O slot está vazio
        }

        string json = PlayerPrefs.GetString(key);
        return JsonUtility.FromJson<PlayerData>(json);
    }

    /// <summary>
    /// Deleta o save de um slot específico
    /// </summary>
    public static void DeleteSave(int slot) {
        string key = GetSaveKey(slot);
        if (PlayerPrefs.HasKey(key)) {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"[SaveSystem] Save do Slot {slot} deletado.");
        }
    }

    /// <summary>
    /// Verifica se existe save em um slot específico
    /// </summary>
    public static bool HasSave(int slot) {
        return PlayerPrefs.HasKey(GetSaveKey(slot));
    }

    /// <summary>
    /// Verifica se existe QUALQUER save (para ligar o botão 'Continuar' no Menu)
    /// </summary>
    public static bool HasAnySave() {
        return HasSave(1) || HasSave(2) || HasSave(3);
    }

    /// <summary>
    /// Descobre qual foi o último slot que o jogador abriu
    /// </summary>
    public static int GetLastPlayedSlot() {
        // Se não achar nada, devolve o slot 1 por padrão
        return PlayerPrefs.GetInt("LastPlayedSlot", 1);
    }
}