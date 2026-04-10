using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem {

    private static string Path => Application.persistentDataPath + "/player.fun";

    public static bool HasSave() => File.Exists(Path);

    public static void SavePlayer(PlayerBase player, PlayerStats stats) {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(Path, FileMode.Create);
        PlayerData data = new PlayerData(player, stats);
        formatter.Serialize(stream, data);
        stream.Close();
        Debug.Log("[SaveSystem] Jogo salvo.");
    }

    public static PlayerData LoadPlayer() {
        if (!File.Exists(Path)) {
            Debug.LogError("[SaveSystem] Save não encontrado em: " + Path);
            return null;
        }
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(Path, FileMode.Open);
        PlayerData data = formatter.Deserialize(stream) as PlayerData;
        stream.Close();
        return data;
    }
}