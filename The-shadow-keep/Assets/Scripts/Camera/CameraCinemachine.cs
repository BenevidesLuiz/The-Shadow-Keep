using UnityEngine;
using Unity.Cinemachine; // ATENÇÃO: Obrigatório para o Unity 6!

public class CameraFollowSetup : MonoBehaviour
{
    private CinemachineCamera vcam;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
    }

    void LateUpdate()
    {
        if (vcam != null && vcam.Follow != null) return;
        GameObject player = GameObject.FindWithTag("Player"); 

        if (player != null && vcam != null)
        {
            vcam.Follow = player.transform;
            Debug.Log("Câmera encontrou o Player e começou a seguir!");
        }
    }
}