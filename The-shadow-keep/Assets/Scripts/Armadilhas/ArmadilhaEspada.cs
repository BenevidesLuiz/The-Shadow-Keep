using UnityEngine;
using System.Collections;

public class ArmadilhaEspada : MonoBehaviour {
    [Header("Configurações de Tempo")]
    [Tooltip("Tempo máximo de queda antes de sumir por segurança")]
    [SerializeField] private float tempoMaximoQueda = 1.5f;

    [Header("Configurações de Dano")]
    [SerializeField] private int quantidadeDano = 1;

    [Header("Configurações de Colisão (O que é Chão?)")]
    [Tooltip("Selecione aqui quais Layers a espada deve considerar como chão/plataforma para se destruir")]
    [SerializeField] private LayerMask oQueEChao; // <--- A MÁGICA AQUI

    private Rigidbody2D rb2D;
    private SpriteRenderer spriteRenderer;
    private Collider2D meuCollider;
    private Vector3 posicaoInicial;
    private bool jaDeuDanoNesteCiclo = false;

    void Awake() {
        rb2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        meuCollider = GetComponent<Collider2D>();
    }

    void Start() {
        // Salva o ponto exato do teto configurado no Unity
        posicaoInicial = transform.position;

        // Inicia o "Timer" em segundo plano
        StartCoroutine(CicloDoEspinhoComTimerAleatorio());
    }

    private IEnumerator CicloDoEspinhoComTimerAleatorio() {
        while (true) {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.gravityScale = 0f;
            transform.position = posicaoInicial;
            jaDeuDanoNesteCiclo = false;

            spriteRenderer.enabled = true;
            meuCollider.enabled = true;

            float tempoEsperaAleatorio = Random.Range(0f, 10f);

            yield return new WaitForSeconds(tempoEsperaAleatorio);

            rb2D.linearVelocity = Vector2.zero; // Garante que zera a velocidade antes de cair
            rb2D.gravityScale = 0.09f;

            yield return new WaitForSeconds(tempoMaximoQueda);
        }
    }

    // Dispara se o espinho estiver marcado como "Is Trigger"
    private void OnTriggerEnter2D(Collider2D collision) {
        ProcessarImpacto(collision.gameObject);
    }

    // Dispara se o espinho for uma colisão Sólida
    private void OnCollisionEnter2D(Collision2D collision) {
        ProcessarImpacto(collision.gameObject);
    }

    // Lógica unificada para saber em quem bateu
    private void ProcessarImpacto(GameObject objetoAtingido) {
        // 1. Detecta o Player
        if (objetoAtingido.CompareTag("Player") && !jaDeuDanoNesteCiclo) {
            jaDeuDanoNesteCiclo = true;
            Debug.Log($"[Sucesso]: {name} acertou o Player!");

            PlayerBase scriptPlayer = objetoAtingido.GetComponent<PlayerBase>();
            if (scriptPlayer != null) {
                scriptPlayer.TakeDamage(quantidadeDano);
            }

            SumirEspinho();
            return; 
        }

        if (((1 << objetoAtingido.layer) & oQueEChao) != 0) {
            SumirEspinho();
        }
    }

    private void SumirEspinho() {
        rb2D.linearVelocity = Vector2.zero;
        rb2D.gravityScale = 0f;
        spriteRenderer.enabled = false;
        meuCollider.enabled = false;
    }
}