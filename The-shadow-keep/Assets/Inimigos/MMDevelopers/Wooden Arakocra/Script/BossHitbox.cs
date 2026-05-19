using UnityEngine;

public class BossHitbox : MonoBehaviour {
    [Header("Configurações de Dano")]
    public int danoDoGolpe = 15;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            // Tenta pegar o script de controle do Player (PlayerBase)
            PlayerBase player = collision.GetComponent<PlayerBase>();

            if (player != null) {
                // Se o jogador estiver no estado de Bloqueio (Blocking), o escudo protege!
                if (player.CurrentPlayerState == PlayerBase.State.Blocking) {
                    Debug.Log("BLOQUEADO! O jogador usou o escudo.");
                    return;
                }

                // Se o jogador NÃO estiver defendendo, ele toma o dano normal
                player.TakeDamage(danoDoGolpe);
                Debug.Log("O jogador tomou " + danoDoGolpe + " de dano do Boss!");
            }
        }
    }
}