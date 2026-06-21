using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PassagemCastelo : MonoBehaviour {
    [Header("Configurações")]
    public string nomeDaCenaDestino = "CenaInteriorCastelo";
    public float tempoDeAnimacao = 0.5f;

    [Tooltip("Se marcado, vai passar pela tela de Loading. Se desmarcado, carrega a cena direto.")]
    public bool usarTelaDeLoad = true;

    [Header("Requisito para Passar")]
    public GameObject inimigo;

    private Animator anim;
    private bool jaEntrou = false;

    public static string cenaParaCarregar;

    private void Start() {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision) {

        if (inimigo != null) {

            Boss scriptBoss = inimigo.GetComponent<Boss>();

            if (scriptBoss == null || !scriptBoss.estaMorto) {
                Debug.Log("A porta está trancada! Derrote o inimigo primeiro.");
                return;
            }
        }

        if (collision.CompareTag("Player") && !jaEntrou) {
            jaEntrou = true;

            if (anim != null) {
                anim.SetTrigger("Abrir");
            }

            StartCoroutine(EsperarEMudarCena());
        }
    }

    private IEnumerator EsperarEMudarCena() {
        yield return new WaitForSeconds(tempoDeAnimacao);

        if (usarTelaDeLoad) {
            cenaParaCarregar = nomeDaCenaDestino;

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