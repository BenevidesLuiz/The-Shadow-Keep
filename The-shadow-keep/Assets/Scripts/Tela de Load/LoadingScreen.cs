using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreen : MonoBehaviour {

    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private float minLoadTime = 3f;

    private string[] tips = {
        "Descanse em uma fogueira para recuperar seus Estus.",
        "Almas perdidas ficam no chão. Recupere-as antes de morrer novamente.",
        "Bloqueie com Q para reduzir 80% do dano recebido.",
        "Afie sua lâmina em altares para manter o dano máximo."
    };

    private void Start() {
        if (tipText != null)
            tipText.text = tips[Random.Range(0, tips.Length)];
        StartCoroutine(LoadAsync());
    }

    private IEnumerator LoadAsync() {
        string targetScene = (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.targetScene))
            ? GameManager.Instance.targetScene
            : "Fase1";

        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);

        if (op == null) {
            Debug.LogError($"[LoadingScreen] Cena '{targetScene}' não encontrada no Build!");
            yield break;
        }

        op.allowSceneActivation = false;
        float elapsed = 0f;

        while (op.progress < 0.9f || elapsed < minLoadTime) {
            elapsed += Time.deltaTime;
            float loadPercent = Mathf.Clamp01(op.progress / 0.9f);
            float timePercent = Mathf.Clamp01(elapsed / minLoadTime);

            if (progressBar != null)
                progressBar.fillAmount = Mathf.Min(loadPercent, timePercent);

            yield return null;
        }

        if (progressBar != null)
            progressBar.fillAmount = 1f;

        yield return new WaitForSeconds(0.5f);
        op.allowSceneActivation = true;
    }
}