using UnityEngine;
using UnityEngine.Audio;

public class BarrilQuebravel : MonoBehaviour {
    [Header("Configurações Visuais")]
    [SerializeField] private Sprite barrilInteiro;
    [SerializeField] private Sprite barrilQuebrado;
    private SpriteRenderer spriteRenderer;

    [Header("Interface da Foto (UI)")]
    [SerializeField] private GameObject painelFotoUI;

    [Header("Áudio (Música)")]
    [SerializeField] private AudioClip musicaLuta;

    private AudioSource audioSource;

    private bool jaQuebrou = false;
    private bool fotoEstaNaTela = false;

    void Start() {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Garante que começa com o visual inteiro
        if (spriteRenderer != null && barrilInteiro != null) {
            spriteRenderer.sprite = barrilInteiro;
        }

        // Garante que a foto comece fechada
        if (painelFotoUI != null) painelFotoUI.SetActive(false);

        // CONFIGURAÇÃO DO ÁUDIO ADICIONADA AQUI
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Som 2D
        audioSource.ignoreListenerPause = true; // Permite que a música toque mesmo com o jogo pausado!
    }

    void Update() {
        // Se a foto estiver na tela e o jogador apertar o botão de ação (ex: 'E', 'Espaço' ou clique)
        if (fotoEstaNaTela && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))) {
            FecharFoto();
        }
    }

    // Detecta o impacto do Player
    private void OnTriggerEnter2D(Collider2D other) {
        if (!jaQuebrou && other.CompareTag("Player")) {
            QuebrarBarril();
        }
    }

    private void QuebrarBarril() {
        jaQuebrou = true;

        if (spriteRenderer != null && barrilQuebrado != null) {
            spriteRenderer.sprite = barrilQuebrado;
        }

        if (painelFotoUI != null) {
            painelFotoUI.SetActive(true);
            fotoEstaNaTela = true;

            Time.timeScale = 0f;

            TocarMusica();
        }
    }

    private void TocarMusica() {
        if (musicaLuta != null && audioSource != null) {
            audioSource.clip = musicaLuta;
            audioSource.Play();
        }
    }

    private void FecharFoto() {
        fotoEstaNaTela = false;

        if (painelFotoUI != null) {
            painelFotoUI.SetActive(false);
        }
        Time.timeScale = 1f;
    }
}