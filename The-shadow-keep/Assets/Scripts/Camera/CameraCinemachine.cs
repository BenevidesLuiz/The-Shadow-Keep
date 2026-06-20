using UnityEngine;
using Unity.Cinemachine;

public class CameraFollowSetup : MonoBehaviour
{
    // Mudamos para LateUpdate para rodar continuamente após os spawners agirem
    void LateUpdate()
    {
        // 1. Tenta encontrar o jogador na cena
        GameObject player = GameObject.FindWithTag("Player"); 

        if (player != null)
        {
            // 2. Encontra a Cinemachine Camera
            CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();

            if (vcam != null)
            {
                // 3. Cola o jogador na câmera
                vcam.Follow = player.transform;
                
                // 4. DESATIVA este script! Já achamos o Player, não precisamos mais procurar.
                this.enabled = false; 
            }
        }
    }
}