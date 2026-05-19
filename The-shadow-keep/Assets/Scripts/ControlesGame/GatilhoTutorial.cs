using UnityEngine;

public class GatilhoTutorial : MonoBehaviour {
    public GameObject canvasDoTexto;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            if (canvasDoTexto != null) {
                canvasDoTexto.SetActive(false); // O texto some do mapa!
                Destroy(gameObject); // Destrói o gatilho para ele não rodar de novo
            }
        }
    }
}