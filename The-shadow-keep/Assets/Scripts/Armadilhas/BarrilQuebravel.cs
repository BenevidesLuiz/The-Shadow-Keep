using UnityEngine;

public class BarrilQuebravel : MonoBehaviour
{
    [Header("Configurações Visuais")]
    [SerializeField] private Sprite barrilInteiro;
    [SerializeField] private Sprite barrilQuebrado;
    private SpriteRenderer spriteRenderer;

    [Header("Interface da Foto (UI)")]
    [SerializeField] private GameObject painelFotoUI;

    private bool jaQuebrou = false;
    private bool fotoEstaNaTela = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Garante que começa com o visual inteiro
        if (spriteRenderer != null && barrilInteiro != null)
        {
            spriteRenderer.sprite = barrilInteiro;
        }

        // Garante que a foto comece fechada
        if (painelFotoUI != null) painelFotoUI.SetActive(false);
    }

    void Update()
    {
        // Se a foto estiver na tela e o jogador apertar o botão de ação (ex: 'E', 'Espaço' ou clique)
        if (fotoEstaNaTela && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            FecharFoto();
        }
    }

    // Detecta o impacto do Player (mude para OnCollisionEnter2D se o colisor não for Trigger)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!jaQuebrou && other.CompareTag("Player"))
        {
            QuebrarBarril();
        }
    }

    private void QuebrarBarril()
    {
        jaQuebrou = true;

        // 1. Muda a foto do barril para o estado quebrado
        if (spriteRenderer != null && barrilQuebrado != null)
        {
            spriteRenderer.sprite = barrilQuebrado;
        }

        // 2. Abre a foto no meio da tela
        if (painelFotoUI != null)
        {
            painelFotoUI.SetActive(true);
            fotoEstaNaTela = true;

            // 3. Pausa o tempo do jogo para o player ler com calma
            Time.timeScale = 0f; 
        }
    }

    private void FecharFoto()
    {
        fotoEstaNaTela = false;
        
        if (painelFotoUI != null)
        {
            painelFotoUI.SetActive(false);
        }

        // Despausa o jogo voltando o tempo ao normal
        Time.timeScale = 1f; 
    }
}
