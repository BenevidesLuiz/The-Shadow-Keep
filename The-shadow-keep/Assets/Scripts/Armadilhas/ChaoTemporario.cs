using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class ChaoTemporario : MonoBehaviour
{
    private TilemapRenderer tilemapRenderer;
    private TilemapCollider2D tilemapCollider;
    private bool jaPisou = false;

    [Header("Configurações")]
    [Tooltip("Tempo em segundos antes do chão sumir")]
    [SerializeField] private float tempoParaSumir = 5f;
    
    [Tooltip("Tempo em segundos para o chão reaparecer (0 para nunca mais voltar)")]
    [SerializeField] private float tempoParaReaparecer = 3f;

    void Awake()
    {
        // Pega os componentes do próprio Tilemap onde o script está anexado
        tilemapRenderer = GetComponent<TilemapRenderer>();
        tilemapCollider = GetComponent<TilemapCollider2D>();
    }

    // Detecta colisões físicas (quando o player pisa)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Só inicia o processo se quem pisou for o Player e se o timer já não tiver começado
        if (!jaPisou && (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Player")))
        {
            // Checa se o contato veio de cima (para evitar ativar batendo a cabeça por baixo)
            Vector2 normal = collision.contacts[0].normal;
            if (normal.y < -0.5f) 
            {
                jaPisou = true;
                Debug.Log($"[CHÃO]: Player pisou! Sumindo em {tempoParaSumir} segundos...");
                StartCoroutine(IniciarContagemSumir());
            }
        }
    }

    private IEnumerator IniciarContagemSumir()
    {
        // 1. Espera o tempo determinado de 5 segundos
        yield return new WaitForSeconds(tempoParaSumir);

        // 2. Desativa o visual e a colisão do Tilemap
        if (tilemapRenderer != null) tilemapRenderer.enabled = false;
        if (tilemapCollider != null) tilemapCollider.enabled = false;
        Debug.Log("[CHÃO]: Chão desapareceu!");

        // 3. [Opcional] Faz o chão voltar depois de um tempo
        if (tempoParaReaparecer > 0f)
        {
            yield return new WaitForSeconds(tempoParaReaparecer);
            
            if (tilemapRenderer != null) tilemapRenderer.enabled = true;
            if (tilemapCollider != null) tilemapCollider.enabled = true;
            
            jaPisou = false; // Permite que seja ativado novamente
            Debug.Log("[CHÃO]: Chão reapareceu e está pronto de novo!");
        }
    }
}