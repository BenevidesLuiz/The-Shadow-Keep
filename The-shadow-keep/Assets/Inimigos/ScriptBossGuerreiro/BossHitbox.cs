using UnityEngine;

public class BossHitbox : MonoBehaviour {
    [Header("Configurações de Dano")]
    public int danoDoGolpe = 15;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            PlayerBase player = collision.GetComponent<PlayerBase>();

            if (player != null) {
                if (player.CurrentPlayerState == PlayerBase.State.Blocking) {
                    Debug.Log("BLOQUEADO! O jogador usou o escudo.");
                    return;
                }

                player.TakeDamage(danoDoGolpe);
                Debug.Log("O jogador tomou " + danoDoGolpe + " de dano do Boss!");
            }
        }
    }
}