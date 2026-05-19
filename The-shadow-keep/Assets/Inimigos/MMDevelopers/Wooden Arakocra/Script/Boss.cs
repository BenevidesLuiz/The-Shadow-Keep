using UnityEngine;
using System.Collections;

public class Boss : MonoBehaviour {
    [Header("Objetos Filhos")]
    public GameObject hitboxObjeto;

    [Header("Status do Boss")]
    public int vidaMax = 50;
    private int vidaAtual;

    [Header("Configurações do Boss")]
    public float velocidade = 3f;
    public float distanciaVisao = 25f;
    public float distanciaAtaque = 1.8f;
    public float tempoEntreAtaques = 2f;

    [Header("Áudio de Vitória")]
    public AudioClip musicaVitoria;

    private Transform player;
    private Rigidbody2D rb2D;
    private Animator animator;
    private bool viradoParaDireita = true;
    private bool estaAtacando = false;
    private bool estaMorto = false;

    private string animIdle = "Wooden Aarakocra Idle Animation";
    private string animAtaque1 = "Wooden Aarakocra Attack 1Animation";

    void Start() {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        vidaAtual = vidaMax;

        // Pega o renderizador de imagem do Boss
        SpriteRenderer renderizador = GetComponent<SpriteRenderer>();
        if (renderizador != null) {
            // Um número alto como 15 garante que ele seja desenhado por cima de qualquer fundo ou Tilemap!
            renderizador.sortingOrder = 15;
            Debug.Log("[Boss] Camada visual forçada para a frente (sortingOrder = 15).");
        }
       
        if (hitboxObjeto != null) hitboxObjeto.SetActive(false);
    }

    void Update() {
        if (estaMorto) return;

        if (player == null) {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) {
                player = p.transform;

                if (player.name.Contains("Paladin") || player.name.Contains("paladin")) {
                    vidaMax = vidaMax * 2;
                    vidaAtual = vidaMax;
                    Debug.Log("MODO DIFÍCIL: O Boss viu que é o Paladino e dobrou a vida para " + vidaMax + "!");
                }
                else {
                    Debug.Log("MODO NORMAL: Guerreiro detectado. Vida do Boss mantida em " + vidaMax + ".");
                }
            }
            return;
        }

        if (estaAtacando) return;

        float distancia = Vector2.Distance(transform.position, player.position);

        if (distancia <= distanciaVisao && distancia > distanciaAtaque) {
            PerseguirPlayer();
        }
        else if (distancia <= distanciaAtaque) {
            PararEAatacar();
        }
        else {
            FicarParado();
        }
    }

    void PerseguirPlayer() {
        // Calcula a direção real em direção ao Player
        Vector2 direcao = (player.position - transform.position).normalized;

        rb2D.linearVelocity = new Vector2(direcao.x * velocidade, direcao.y * velocidade);

        if (animator != null) animator.Play(animIdle);

        VirarSprite(direcao.x);
    }

    void PararEAatacar() {
        rb2D.linearVelocity = new Vector2(0, rb2D.linearVelocity.y);
        StartCoroutine(RotinaDeAtaque());
    }

    void FicarParado() {
        rb2D.linearVelocity = new Vector2(0, rb2D.linearVelocity.y);
        if (animator != null) animator.Play(animIdle);
    }

    void VirarSprite(float direcaoX) {
        if (direcaoX > 0 && !viradoParaDireita) {
            viradoParaDireita = true;
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (direcaoX < 0 && viradoParaDireita) {
            viradoParaDireita = false;
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    private IEnumerator RotinaDeAtaque() {
        estaAtacando = true;
        if (animator != null) animator.Play(animAtaque1);

        yield return new WaitForSeconds(0.4f);
        if (hitboxObjeto != null) hitboxObjeto.SetActive(true);

        // Deixa o golpe ativo por um curto período (0.2s) para registrar o dano e DESLIGA
        yield return new WaitForSeconds(0.2f);
        if (hitboxObjeto != null) hitboxObjeto.SetActive(false);

        // Espera o resto da animação do ataque terminar antes de liberar o Boss
        yield return new WaitForSeconds(0.4f);

        if (animator != null) animator.Play(animIdle);
        yield return new WaitForSeconds(tempoEntreAtaques);
        estaAtacando = false;
    }

    public void TomarDano(int dano) {
        if (estaMorto) return;

        vidaAtual -= dano;
        Debug.Log("Boss tomou: " + dano + " de dano! Vida restante: " + vidaAtual);

        if (vidaAtual <= 0) {
            Morrer();
        }
    }

    private void Morrer() {
        estaMorto = true;
        rb2D.linearVelocity = Vector2.zero;

        GameObject gerenciadorMusica = GameObject.Find("Musica");
        if (gerenciadorMusica != null) {
            AudioSource audioFundo = gerenciadorMusica.GetComponent<AudioSource>();
            if (audioFundo != null) audioFundo.Stop();
        }

        if (musicaVitoria != null) {
            AudioSource.PlayClipAtPoint(musicaVitoria, Camera.main.transform.position, 1f);
        }

        GameObject textoVitoria = GameObject.Find("CanvasVidaeEstamina/TextoVitoriaTutorial");
        if (textoVitoria == null) textoVitoria = GameObject.Find("TextoVitoriaTutorial");

        if (textoVitoria != null) {
            textoVitoria.SetActive(true);
            Debug.Log("Mensagem de vitória ativada na tela!");
        }

        GetComponent<Collider2D>().enabled = false;
        GetComponent<SpriteRenderer>().enabled = false;
        if (hitboxObjeto != null) hitboxObjeto.SetActive(false);

        Destroy(gameObject, 5f);
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaVisao);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
}