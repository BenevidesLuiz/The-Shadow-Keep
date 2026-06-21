using UnityEngine;
using Unity.Cinemachine;

public class TransicaoCameras : MonoBehaviour
{
    [Header("Configuração da Câmera")]
    [Tooltip("Arraste a Cinemachine Camera DESTA sala aqui")]
    [SerializeField] private CinemachineCamera minhaCameraDaSala;

    private void Start()
    {
        // Garante que o objeto tem um Collider2D
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError($"[Erro - {name}]: Não há nenhum Collider2D neste objeto! O sistema de transição não vai funcionar.");
            return;
        }

        // Garante que o Is Trigger está marcado para não bloquear o jogador fisicamente
        if (!collider.isTrigger)
        {
            collider.isTrigger = true;
            Debug.LogWarning($"[Aviso - {name}]: O Collider não estava marcado como 'Is Trigger'. Corrigido automaticamente por código.");
        }

        if (minhaCameraDaSala == null)
        {
            Debug.LogError($"[Erro - {name}]: Você esqueceu de arrastar a CinemachineCamera no Inspector deste script!");
        }
    }

    // Identifica quando ALGUÉM entrou no limite desta sala
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se quem entrou foi o jogador usando a Tag
        if (collision.CompareTag("Player"))
        {
            Debug.Log($"[Sucesso - {name}]: O Player entrou nesta sala! Ativando a câmera correspondente.");

            // 1. Procura por TODAS as CinemachineCameras que estão ativas na cena e desativa-las
            CinemachineCamera[] todasAsCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            foreach (CinemachineCamera cam in todasAsCameras)
            {
                cam.gameObject.SetActive(false);
            }

            // 2. Ativa apenas a câmera desta sala específica
            if (minhaCameraDaSala != null)
            {
                minhaCameraDaSala.gameObject.SetActive(true);
                Debug.Log($"[Sucesso - {name}]: Câmera '{minhaCameraDaSala.name}' ativada com sucesso!");
            }
            else
            {
                Debug.LogError($"[Erro - {name}]: Não foi possível ativar a câmera porque a referência está vazia.");
            }
        }
    }
}