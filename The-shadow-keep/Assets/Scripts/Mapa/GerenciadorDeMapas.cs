using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GerenciadorDeMapas : MonoBehaviour {

    SoulslikeKnight Player = new SoulslikeKnight();


    [SerializeField] private string CarregarFase;
    [SerializeField] private string CenaMorte;
    [SerializeField] private string CenaVitoria;

    public GameObject meuPrefab;
    GameObject ObjetoDeletar;
    private Transform playerTransform;
    public float Pontox = 20f;
   

    void Start() {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null) {
            playerTransform = player.transform;
        }

        ObjetoDeletar = Instantiate(meuPrefab, new Vector3(Pontox, 0, 0), Quaternion.identity);
    }

    void Update() {
        if (playerTransform != null) {
            if (playerTransform.position.x > Pontox) {

                if (ObjetoDeletar != null) {
                    Destroy(ObjetoDeletar);
                }
                Pontox += 20f;

                ObjetoDeletar = Instantiate(meuPrefab, new Vector3(Pontox, 0, 0), Quaternion.identity);
            }
        }
    }

    public void CarregarProximaFase() {
        SceneManager.LoadScene(CarregarFase);
    }

    public void CarregarCenaMorte() {
        if (Player.CompareTag("Dead")) {
            SceneManager.LoadScene(CenaMorte);
        }
    }

}
