using UnityEngine;

public class Camerafollow : MonoBehaviour {
    public Transform target;   // Player
    public Vector3 offset;     // Distancia da c�mera
    public float smoothSpeed = 0.125f;
    public bool travarCamera = false;
    [Header("Limites da c�mera")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;
    public void TravarCamera() {
        travarCamera = true;
    }

    void LateUpdate() {
        if (target != null) {
            Vector3 desiredPosition = target.position + offset;

            // Limita a posicao da camera
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

