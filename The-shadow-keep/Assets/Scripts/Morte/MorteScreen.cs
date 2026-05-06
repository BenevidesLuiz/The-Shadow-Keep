using UnityEngine;
using UnityEngine.Video;
using System.Collections;

public class MorteScreen : MonoBehaviour {

    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Configuração de Tempo")]
    [Tooltip("Quanto tempo (em segundos) a tela fica parada APÓS o vídeo acabar?")]
    [SerializeField] private float tempoDeEspera = 2.5f;

    private void Start() {
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();
    }

    private void OnVideoEnd(VideoPlayer vp) {
        videoPlayer.loopPointReached -= OnVideoEnd;

        StartCoroutine(EsperarECarregar());
    }

    private IEnumerator EsperarECarregar() {
        yield return new WaitForSeconds(tempoDeEspera);

        string cenaAtual = GameManager.Instance.currentScene;
        GameManager.Instance.GoToScene(cenaAtual, loadSave: true);
    }
}