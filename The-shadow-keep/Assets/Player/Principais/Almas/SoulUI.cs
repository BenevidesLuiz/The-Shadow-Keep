using TMPro;
using UnityEngine;
using UnityEngine.UI; 
public class SoulUI : MonoBehaviour {

    [Header("Referência da Tela")]
    [Tooltip("Arraste o seu objeto de Texto de almas aqui")]
    public TextMeshProUGUI textoAlmas; 

    private void Start() {
        AtualizarTexto(SoulManager.Instance.CurrentSouls);

        SoulManager.Instance.OnSoulsChanged += AtualizarTexto;
    }

    private void OnDestroy() {
        if (SoulManager.Instance != null) {
            SoulManager.Instance.OnSoulsChanged -= AtualizarTexto;
        }
    }

    private void AtualizarTexto(int quantidade) {
        if (textoAlmas != null) {
            textoAlmas.text = quantidade.ToString();
        }
    }
}