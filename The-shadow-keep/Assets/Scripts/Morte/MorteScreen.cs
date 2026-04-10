using UnityEngine;
using UnityEngine.Video;

public class MorteScreen : MonoBehaviour {

    [SerializeField] private VideoPlayer videoPlayer;

    private void Start() {
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.Play();
    }

    private void OnVideoEnd(VideoPlayer vp) {
        videoPlayer.loopPointReached -= OnVideoEnd;
        string cenaAtual = GameManager.Instance.currentScene;
        GameManager.Instance.GoToScene(cenaAtual, loadSave: true);
    }
}