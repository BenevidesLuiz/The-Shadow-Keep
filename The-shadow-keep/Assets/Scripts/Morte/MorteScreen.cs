using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.SceneManagement;

public class MorteScreen : MonoBehaviour {
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Configuração de Tempo")]
    [Tooltip("Quanto tempo (em segundos) a tela fica parada APÓS o vídeo acabar?")]
    [SerializeField] private float tempoDeEspera = 5.4f;

    private string cenaParaCarregar;

    private void Start() {
        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.currentScene)) {
            cenaParaCarregar = GameManager.Instance.currentScene;
            Debug.Log($"[MorteScreen] Cena armazenada do GameManager: {cenaParaCarregar}");
        }
        else {
            cenaParaCarregar = PlayerPrefs.GetString("LastScene", "Fase1");
            Debug.Log($"[MorteScreen] Cena recuperada do PlayerPrefs: {cenaParaCarregar}");
        }

        if (videoPlayer != null) {
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Play();
        }
        else {
            Debug.LogWarning("[MorteScreen] VideoPlayer não foi atribuído no Inspector!");
            StartCoroutine(EsperarECarregar());
        }
    }

    private void OnVideoEnd(VideoPlayer vp) {
        if (videoPlayer != null) {
            videoPlayer.loopPointReached -= OnVideoEnd;
        }
        StartCoroutine(EsperarECarregar());
    }

    private IEnumerator EsperarECarregar() {
        yield return new WaitForSeconds(tempoDeEspera);

        if (!string.IsNullOrEmpty(cenaParaCarregar)) {
            Debug.Log($"[MorteScreen] Carregando cena: {cenaParaCarregar}");
            SceneManager.LoadScene(cenaParaCarregar);
        }
        else {
            Debug.LogError("[MorteScreen] Nome da cena está vazio! Carregando 'Fase1' como fallback.");
            SceneManager.LoadScene("Fase1");
        }
    }

    private void OnDestroy() {
        if (videoPlayer != null) {
            videoPlayer.loopPointReached -= OnVideoEnd;
        }
    }
}