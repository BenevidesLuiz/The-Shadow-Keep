using UnityEngine;
using System.Collections;

public class AtravessarPlataforma : MonoBehaviour
{
    private Collider2D colisorPlataforma;
    private SpriteRenderer spritePlataforma; // Opcional: caso queira que ela suma visualmente também

    void Awake()
    {
        // Pega o colisor da própria plataforma
        colisorPlataforma = GetComponent<Collider2D>();
        spritePlataforma = GetComponent<SpriteRenderer>();
    }

    // Usamos o Trigger ou Collision. Para garantir, esse método checa quem está pisando nela por proximidade física básica
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Se o objeto que está colidindo for o jogador
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Player"))
        {
            // Se apertar a tecla 'S' ou Seta para Baixo
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                Debug.Log("[PLATAFORMA]: Tecla S detectada! Desligando colisor por 1.5 segundo.");
                StartCoroutine(DesativarPlataformaTemporariamente());
            }
        }
    }

    // Caso o seu Platform Effector esteja configurado como Trigger por algum motivo
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.name.Contains("Player"))
        {
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                Debug.Log("[PLATAFORMA (Trigger)]: Tecla S detectada! Desligando colisor por 1.5 segundo.");
                StartCoroutine(DesativarPlataformaTemporariamente());
            }
        }
    }

    private IEnumerator DesativarPlataformaTemporariamente()
    {
        // 1. Desliga o colisor físico da plataforma (ela deixa de existir para o motor de física)
        if (colisorPlataforma != null)
        {
            colisorPlataforma.enabled = false;
        }

        // [OPCIONAL]: Se você quiser que a plataforma fique meio transparente ou suma enquanto estiver desativada, descomente a linha abaixo:
        // if (spritePlataforma != null) spritePlataforma.color = new Color(1f, 1f, 1f, 0.3f); 

        // 2. Espera o tempo exato que você pediu (1.5 segundo)
        yield return new WaitForSeconds(0.9f);

        // 3. Liga o colisor novamente
        if (colisorPlataforma != null)
        {
            colisorPlataforma.enabled = true;
            Debug.Log("[PLATAFORMA]: 0.9 segundo se passou. Colisor reativado e sólido novamente!");
        }

        // Reverte a cor ao normal caso tenha mudado ali em cima
        // if (spritePlataforma != null) spritePlataforma.color = Color.white;
    }
}