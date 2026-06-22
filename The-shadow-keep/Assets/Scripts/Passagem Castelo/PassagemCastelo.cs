using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class TransicaoAposChefe : MonoBehaviour {
    [Header("Configurações de Transição")]
    public string nomeDaCenaDestino = "CenaInteriorCastelo";
    public float tempoDeEspera = 15f; 

    [Tooltip("Se marcado, vai passar pela tela de Loading. Se desmarcado, carrega a cena direto.")]
    public bool usarTelaDeLoad = true;
    public static string cenaParaCarregar;

    [Header("Referência do Inimigo")]
    [Tooltip("Arraste o GameObject do Boss aqui pelo Inspector")]
    public Boss scriptBoss;

    private bool jaIniciouTransicao = false;

    private void Update() {
        if (!jaIniciouTransicao && scriptBoss != null && scriptBoss.estaMorto) {
            jaIniciouTransicao = true; 
            StartCoroutine(EsperarEMudarCena());
        }
    }

    private IEnumerator EsperarEMudarCena() {
        Debug.Log("Chefe derrotado! Aguardando " + tempoDeEspera + " segundos...");

        yield return new WaitForSeconds(tempoDeEspera);

        if (usarTelaDeLoad) {
            if (GameManager.Instance != null) {
                GameManager.Instance.targetScene = nomeDaCenaDestino;
            }
            else {
                Debug.LogError("GameManager não encontrado na cena!");
            }

            SceneManager.LoadScene("Loading");
        }
        else {
            if (GameManager.Instance != null) {
                GameManager.Instance.GoToSceneInstant(nomeDaCenaDestino);
            }
            else {
                SceneManager.LoadScene(nomeDaCenaDestino);
            }
        }
    }
}