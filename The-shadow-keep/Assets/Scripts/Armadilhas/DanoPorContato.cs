using UnityEngine;

public class DanoPorContato : MonoBehaviour
{
    [Header("Configurações de Dano")]
    [Tooltip("Quantidade de dano que o player vai sofrer ao entrar no trigger")]
    [SerializeField] private float quantidadeDano = 20f;

    // Substituímos o OnCollisionEnter2D por OnTriggerEnter2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verifica se o objeto que entrou na área do gatilho tem a Tag "Player"
        if (other.CompareTag("Player"))
        {
            // Como 'other' já é o próprio Collider2D, pegamos o script direto dele
            PlayerBase scriptPlayer = other.GetComponent<PlayerBase>();

            // Se encontrou o script do Player, aplica o dano usando sua função
            if (scriptPlayer != null)
            {
                Debug.Log($"[GATILHO]: {gameObject.name} detectou o Player e causou dano!");
                scriptPlayer.TakeDamage(quantidadeDano);
            }
        }
    }
}
