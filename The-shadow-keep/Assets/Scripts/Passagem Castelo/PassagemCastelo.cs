using UnityEngine;
using System.Collections;

public class PassagemCastelo : MonoBehaviour {
    [Header("Configurações")]
    public string nomeDaCenaDestino = "CenaInteriorCastelo";
    public float tempoDeAnimacao = 0.5f;

    [Header("Requisito para Passar")]
    [Tooltip("Arraste o inimigo aqui. A porta só abre se ele for destruído (ficar vazio).")]
    public GameObject inimigo;

    private Animator anim;
    private bool jaEntrou = false;

    private void Start() {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        if (inimigo != null) {
            Debug.Log("A porta está trancada! Derrote o inimigo primeiro.");
            return; 
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

        if (GameManager.Instance != null) {
            GameManager.Instance.GoToScene(nomeDaCenaDestino);
        }
        else {
            Debug.LogError("GameManager não encontrado! Arraste o Prefab do GameManager para esta cena.");
        }
    }
}