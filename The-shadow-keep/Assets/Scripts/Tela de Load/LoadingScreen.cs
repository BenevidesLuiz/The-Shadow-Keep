using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class LoadingScreen : MonoBehaviour {

    [Header("UI do Loading")]
    [SerializeField] private Image progressBar;
    [SerializeField] private TMP_Text tipText;

    [Header("Configurações")]
    [SerializeField] private float minLoadTime = 3f;

    private string[] tips = {
        // Sobrevivência e Punição
        "Descanse em uma fogueira para recuperar seus Estus e salvar seu progresso.",
        "Almas perdidas ficam no chão. Recupere-as antes de morrer novamente, ou se perderão para sempre.",
        "A ganância é a sua pior inimiga. Recue, cure-se e espere o momento certo para atacar.",
        "Use suas Almas para subir de nível e fortalecer seus atributos. A morte cobra um preço alto.",
        
        //Combate       
        "Bloqueie com 'Q' para reduzir drasticamente o dano, mas preste atenção na sua Estâmina.",
        "Gerencie sua Estâmina com sabedoria. Ficar sem fôlego no meio da batalha é uma sentença de morte.",
        "A esquiva ('E') concede uma breve janela de invulnerabilidade. Use-a no momento exato.",
        "Ataques pesados causam dano devastador, mas deixam você vulnerável por mais tempo.",
        "Estude o padrão de movimentos do inimigo antes de desferir o primeiro golpe.",

        // Mecânicas e Classes
        "Afie sua lâmina em altares. Uma arma cega mal arranha a armadura de seus inimigos.",
        "A poção de Estus ('O') leva um tempo precioso para ser bebida. Encontre uma abertura segura.",
        "A Fé do Paladino permite que ele recupere vida ao desferir golpes sagrados.",
        "O Guerreiro confia na força bruta, causando mais dano físico para compensar a falta de milagres."
    };

    private void Start() {
        if (tipText != null) {
            tipText.text = tips[Random.Range(0, tips.Length)];
        }

        StartCoroutine(LoadAsync());
    }

    private IEnumerator LoadAsync() {
        // 1. Descobre para onde o GameManager quer ir. Se der erro, o "plano B" é a Fase1.
        string targetScene = "Fase1";

        if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.targetScene)) {
            targetScene = GameManager.Instance.targetScene;
        }

        Debug.Log($"[LoadingScreen] Iniciando carregamento da cena: {targetScene}");

        // 2. Inicia o carregamento assíncrono em segundo plano
        AsyncOperation op = SceneManager.LoadSceneAsync(targetScene);

        if (op == null) {
            Debug.LogError($"[LoadingScreen] ERRO: A cena '{targetScene}' não foi adicionada no Build Settings!");
            yield break;
        }

        // Impede que o jogo mude de tela bruscamente assim que carregar (segura no 90%)
        op.allowSceneActivation = false;
        float elapsed = 0f;

        // 3. Preenche a barra dependendo do que for mais lento: o carregamento real ou o 'minLoadTime'
        while (op.progress < 0.9f || elapsed < minLoadTime) {
            elapsed += Time.deltaTime;

            float loadPercent = Mathf.Clamp01(op.progress / 0.9f);
            float timePercent = Mathf.Clamp01(elapsed / minLoadTime);

            if (progressBar != null) {
                progressBar.fillAmount = Mathf.Min(loadPercent, timePercent);
            }

            yield return null;
        }

        if (progressBar != null) {
            progressBar.fillAmount = 1f;
        }

        // Uma pequena pausa dramática de meio segundo para o jogador ver a barra cheia
        yield return new WaitForSeconds(0.5f);

        // 5. Destranca a porta e joga o player na fase!
        op.allowSceneActivation = true;
    }
}