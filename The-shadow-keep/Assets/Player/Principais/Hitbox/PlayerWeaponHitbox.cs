using UnityEngine;

public class PlayerWeaponHitbox : MonoBehaviour {
    [Header("Configurações do Golpe")]
    public bool isHeavyAttack = false;

    private PlayerBase player;

    void Start() {
        player = GetComponentInParent<PlayerBase>();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        // 1. O CONSOLE VAI AVISAR TUDO O QUE A ESPADA ENCOSTAR!
        Debug.Log("A espada bateu no objeto: " + collision.gameObject.name + " | A Tag dele é: " + collision.tag);

        if (collision.CompareTag("Enemy")) {
            Boss bossScript = collision.GetComponent<Boss>();

            // 2. O CONSOLE VAI AVISAR SE ACHOU O SCRIPT DO BOSS
            if (bossScript != null) {
                Debug.Log("Script do Boss encontrado! Aplicando dano...");
                float danoCalculado = isHeavyAttack ? player.GetHeavyDamage() : player.GetLightDamage();

                bossScript.TomarDano((int)danoCalculado);

                if (player is PaladinKnight paladino) {
                    paladino.OnHolyHit(danoCalculado);
                }
                else if (player is SoulslikeKnight guerreiro) {
                    guerreiro.OnHolyHit(danoCalculado);
                }
            }
            else {
                Debug.Log("O objeto tem a Tag Enemy, mas o script 'Boss' não foi encontrado nele!");
            }
        }
    }
}