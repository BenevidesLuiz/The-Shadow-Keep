using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class Boss : MonoBehaviour {
    [Header("Objetos Filhos")]
    public GameObject hitboxObjeto;

    [Header("UI e Áudio de Vitória")]
    public AudioClip musicaVitoria;
    public GameObject textoVitoriaUI;
    public float tempoTocando;

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

    [Header("Drop de Almas")]
    [SerializeField] private int soulDropAmount = 3200;
    [Tooltip("Variação aleatória: valor real = soulDropAmount ± soulDropVariance")]
    [SerializeField] private int soulDropVariance = 20;

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
            rb2D.linearVelocity = new Vector2(0, rb2D.linearVelocity.y);
            rb2D.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            return;
        }
        else {
            rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;
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

        float tempoEspera = 0.4f;
        float tempoHitbox = 0.2f;

        if (golpeSorteado == 1) {
            MudarAnimacao(animAtaque1);
            tempoEspera = 0.4f;
            tempoHitbox = 0.2f;
        }
        else if (golpeSorteado == 2) {
            MudarAnimacao(animAtaque2);
            tempoEspera = 0.6f;
            tempoHitbox = 0.3f;
        }
        else {
            MudarAnimacao(animAtaque3);
            tempoEspera = 0.5f;
            tempoHitbox = 0.3f;
        }

        yield return new WaitForSeconds(tempoEspera);
        if (hitboxObjeto != null) hitboxObjeto.SetActive(true);

        yield return new WaitForSeconds(tempoHitbox);
        if (hitboxObjeto != null) hitboxObjeto.SetActive(false);

        yield return new WaitForSeconds(0.4f);

        MudarAnimacao(animIdle);
        yield return new WaitForSeconds(tempoEntreAtaques);

        rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;

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

    private void DropSouls() {
        if (SoulManager.Instance == null) return;

        int variance = Random.Range(-soulDropVariance, soulDropVariance + 1);
        int finalDrop = Mathf.Max(1, soulDropAmount + variance);

        SoulManager.Instance.AddSouls(finalDrop);
        Debug.Log($"[Enemy] Morreu → +{finalDrop} almas dropadas.");
    }

    private void Morrer() {
        StopAllCoroutines();

        estaMorto = true;

        rb2D.linearVelocity = Vector2.zero;
        rb2D.gravityScale = 0;
        rb2D.bodyType = RigidbodyType2D.Kinematic;
        GetComponent<Collider2D>().enabled = false;
        if (hitboxObjeto != null) hitboxObjeto.SetActive(false);

        MudarAnimacao(animMorte);

        StartCoroutine(SequenciaDeVitoria());
    }

    //Controla o tempo da vitória
    private IEnumerator SequenciaDeVitoria() {

        yield return new WaitForSeconds(2.0f);

        DropSouls();

        GameObject objetoMusicaFase = GameObject.Find("MusicaFundo");
        if (objetoMusicaFase != null) {
            AudioSource audioFase = objetoMusicaFase.GetComponent<AudioSource>();
            if (audioFase != null) audioFase.Stop();
        }

        if (musicaVitoria != null) {
            GameObject tocaFitasTemp = new GameObject("MusicaVitoria_Temp");
            tocaFitasTemp.transform.position = Camera.main.transform.position;

            AudioSource fonteDeAudio = tocaFitasTemp.AddComponent<AudioSource>();
            fonteDeAudio.clip = musicaVitoria;
            fonteDeAudio.volume = 1f;
            fonteDeAudio.Play();

            Destroy(tocaFitasTemp, 40f);
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

        yield return new WaitForSeconds(7f);

        if (textoVitoriaUI != null) {
            textoVitoriaUI.SetActive(false);
        }

        Destroy(gameObject);
    }
    private IEnumerator EsconderTextoVitoria() {
        yield return new WaitForSeconds(7f);
        if (textoVitoriaUI != null) {
            textoVitoriaUI.SetActive(false);
        }
    }

}

    