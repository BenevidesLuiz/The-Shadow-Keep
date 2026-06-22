using UnityEngine;
using System.Collections;

public class ArmadilhaEspada : MonoBehaviour
{
    [Header("Configurações de Tempo")]
    [Tooltip("Tempo máximo de queda antes de sumir por segurança")]
    [SerializeField] private float tempoMaximoQueda = 1.5f;

    [Header("Configurações de Dano")]
    [SerializeField] private int quantidadeDano = 1;

    private Rigidbody2D rb2D;
    private SpriteRenderer spriteRenderer;
    private Collider2D meuCollider;
    private Vector3 posicaoInicial;
    private bool jaDeuDanoNesteCiclo = false;

    void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        meuCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        // Salva o ponto exato do teto configurado no Unity
        posicaoInicial = transform.position;

        // Inicia o "Timer" em segundo plano (substituto seguro da Thread)
        StartCoroutine(CicloDoEspinhoComTimerAleatorio());
    }

    private IEnumerator CicloDoEspinhoComTimerAleatorio()
    {
        while (true)
        {
  
            rb2D.linearVelocity = Vector2.zero;
            rb2D.gravityScale = 0f;
            transform.position = posicaoInicial;
            jaDeuDanoNesteCiclo = false;
            
            spriteRenderer.enabled = true; 
            meuCollider.enabled = true;    

            float tempoEsperaAleatorio = Random.Range(0f, 10f);
            
            yield return new WaitForSeconds(tempoEsperaAleatorio);

            rb2D.gravityScale = 0.09f; 

            yield return new WaitForSeconds(tempoMaximoQueda); 
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Detecção de dano no Player
        if (collision.CompareTag("Player") && !jaDeuDanoNesteCiclo)
        {
            jaDeuDanoNesteCiclo = true;
            Debug.Log($"[Sucesso]: {name} acertou o Player!");

            PlayerBase scriptPlayer = collision.GetComponent<PlayerBase>(); 
            if (scriptPlayer != null)
            {
                scriptPlayer.TakeDamage(quantidadeDano);
            }

            SumirEspinho();
        }
        
        // Detecção de colisão com o Chão (pelo nome ou pela Tag)
        if (collision.gameObject.name.Contains("Chao") || collision.CompareTag("Ground"))
        {
            SumirEspinho();
        }
    }

    private void SumirEspinho()
    {
        rb2D.linearVelocity = Vector2.zero;
        rb2D.gravityScale = 0f;
        spriteRenderer.enabled = false;
        meuCollider.enabled = false;
    }
}