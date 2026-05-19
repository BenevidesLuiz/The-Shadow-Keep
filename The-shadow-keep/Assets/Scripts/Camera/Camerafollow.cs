using UnityEngine;

public class Camerafollow : MonoBehaviour {
    public Transform target;   // Player
    public Vector3 offset;     // Distancia da câmera
    public float smoothSpeed = 0.125f;
    public bool travarCamera = false;

    [Header("Limites da camera")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    public void TravarCamera() {
        travarCamera = true;
    }

    void LateUpdate() {
        
        if (target == null || target.name == "PlayerSpawner") {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) {
                target = player.transform;
            }
            else {
                // Se o Player ainda não deu spawn, o script sai daqui em segurança e tenta de novo no próximo frame
                return;
            }
        }

        if (target != null) {
            Vector3 desiredPosition = target.position + offset;

            float clampedX = Mathf.Clamp(desiredPosition.x, minX, maxX);
            float clampedY = Mathf.Clamp(desiredPosition.y, minY, maxY);
            Vector3 boundedPosition;

            if (travarCamera) {
                boundedPosition = new Vector3(maxX, maxY, desiredPosition.z);
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, boundedPosition, 0.05f);
                transform.position = smoothedPosition;
            }
            else {
                boundedPosition = new Vector3(clampedX, clampedY, desiredPosition.z);
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, boundedPosition, smoothSpeed);
                transform.position = smoothedPosition;
            }
        }
    }
}