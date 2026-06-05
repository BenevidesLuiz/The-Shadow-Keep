using UnityEngine;
using System.Collections;

public class Boss : MonoBehaviour {
    [Header("Objetos Filhos")]
    public GameObject hitboxObjeto;

    [Header("UI e Áudio de Vitória")]
    public AudioClip musicaVitoria;
    public GameObject textoVitoriaUI; 

    [Header("Status do Boss")]
    public int vidaMax = 100;
    private int vidaAtual;

    [Header("Configurações do Boss")]
    public float velocidade = 3.5f;
    public float distanciaVisao = 50f;
    public float distanciaAtaque = 1.8f;
    public float tempoEntreAtaques = 1.5f;

    private Transform player;
    private Rigidbody2D rb2D;
    private Animator animator;
    private bool viradoParaDireita = true;
    private bool estaAtacando = false;
    private bool estaMorto = false;

    private string animAtual = "";
    private string animIdle = "Idle";
    private string animRun = "Run";
    private string animAtaque1 = "Attack1";
    private string animAtaque2 = "Attack2";
    private string animAtaque3 = "Attack3";
    private string animMorte = "Death";

    void Start() {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        vidaAtual = vidaMax;

        SpriteRenderer renderizador = GetComponent<SpriteRenderer>();
        if (renderizador != null) renderizador.sortingOrder = 15;

        if (hitboxObjeto != null) hitboxObjeto.SetActive(false);
    }

    void Update() {
        if (estaMorto) return;

        if (player == null) {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            return;
        }

        if (estaAtacando) {
            // Trava o eixo X constantemente durante o ataque para ele não escorregar
            rb2D.linearVelocity = new Vector2(0, rb2D.linearVelocity.y);
            return;
        }

        float distanciaX = Mathf.Abs(transform.position.x - player.position.x);

        if (distanciaX <= distanciaVisao && distanciaX > distanciaAtaque) {
            PerseguirPlayer();
        }
        else if (distanciaX <= distanciaAtaque) {
            PararEAatacar();
        }
        else {
            FicarParado();
        }
    }

    void MudarAnimacao(string novaAnim) {
        if (animAtual == novaAnim) return;
        if (animator != null) animator.Play(novaAnim);
        animAtual = novaAnim;
    }

    void PerseguirPlayer() {
        float direcaoX = Mathf.Sign(player.position.x - transform.position.x);
        rb2D.linearVelocity = new Vector2(direcaoX * velocidade, rb2D.linearVelocity.y);

        MudarAnimacao(animRun);
        VirarSprite(direcaoX);
    }

    void PararEAatacar() {
        rb2D.linearVelocity = new Vector2(0, rb2D.linearVelocity.y);
        StartCoroutine(RotinaDeAtaque());
    }

    void FicarParado() {
        rb2D.linearVelocity = new Vector2(0, rb2D.linearVelocity.y);
        MudarAnimacao(animIdle);
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
        rb2D.linearVelocity = Vector2.zero;

        float direcaoX = Mathf.Sign(player.position.x - transform.position.x);
        VirarSprite(direcaoX);

        int golpeSorteado = Random.Range(1, 4);
        if (golpeSorteado == 1) MudarAnimacao(animAtaque1);
        else if (golpeSorteado == 2) MudarAnimacao(animAtaque2);
        else MudarAnimacao(animAtaque3);

        yield return new WaitForSeconds(0.4f);
        if (hitboxObjeto != null) hitboxObjeto.SetActive(true);

        yield return new WaitForSeconds(0.2f);
        if (hitboxObjeto != null) hitboxObjeto.SetActive(false);

        yield return new WaitForSeconds(0.4f);

        MudarAnimacao(animIdle);
        yield return new WaitForSeconds(tempoEntreAtaques);
        estaAtacando = false;
    }

    public void TomarDano(int dano) {
        if (estaMorto) return;

        vidaAtual -= dano;
        Debug.Log($"Boss tomou {dano} de dano! Vida restante: {vidaAtual}");

        if (vidaAtual <= 0) {
            Morrer();
        }
        else {
            StartCoroutine(PiscarDano());
        }
    }

    private IEnumerator PiscarDano() {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            sr.color = Color.white;
        }
    }

    private void Morrer() {
        estaMorto = true;

        rb2D.linearVelocity = Vector2.zero;
        rb2D.gravityScale = 0;
        rb2D.bodyType = RigidbodyType2D.Kinematic;

        MudarAnimacao(animMorte);

        if (musicaVitoria != null) {
            AudioSource.PlayClipAtPoint(musicaVitoria, Camera.main.transform.position, 1f);
        }
        else {
            Debug.LogWarning("⚠️ Falta arrastar o áudio de vitória no Inspector!");
        }

        if (textoVitoriaUI != null) {
            textoVitoriaUI.SetActive(true);
        }
        else {
            Debug.LogWarning("⚠️ Falta arrastar o Objeto de Texto de Vitória no Inspector!");
        }

        GetComponent<Collider2D>().enabled = false;
        if (hitboxObjeto != null) hitboxObjeto.SetActive(false);
        Destroy(gameObject, 5f);
    }
}