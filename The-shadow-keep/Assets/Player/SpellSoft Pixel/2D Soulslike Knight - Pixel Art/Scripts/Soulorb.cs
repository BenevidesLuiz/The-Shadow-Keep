using UnityEngine;

/// <summary>
/// SoulOrb — Orbe de almas deixado no chão ao morrer (estilo Dark Souls).
///
/// SETUP:
///   1. Crie um Prefab "SoulOrb" com SpriteRenderer + Collider2D (Is Trigger)
///   2. Adicione este script
///   3. Atribua o prefab no SoulOrbSpawner (ou spawne manualmente)
///
/// COMO FUNCIONA:
///   - Aparece na posição de morte do player
///   - Player toca para recuperar as almas perdidas
///   - Se o player morrer de novo antes de tocar, as almas somem para sempre
/// </summary>
public class SoulOrb : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float rotateSpeed = 90f;

    [Header("Auto-destruição")]
    [Tooltip("Tempo em segundos antes do orbe desaparecer (0 = nunca)")]
    [SerializeField] private float lifetime = 0f;

    private Vector3 startPos;
    private float timeAlive = 0f;
    private SpriteRenderer sr;

    private void Start()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();

        // Escuta nova morte para se destruir (almas perdidas para sempre)
        if (SoulManager.Instance != null)
            SoulManager.Instance.OnSoulsLost += OnPlayerDiedAgain;
    }

    private void OnDestroy()
    {
        if (SoulManager.Instance != null)
            SoulManager.Instance.OnSoulsLost -= OnPlayerDiedAgain;
    }

    private void Update()
    {
        // Flutuação
        float y = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);

        // Rotação
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

        // Vida útil
        if (lifetime > 0f)
        {
            timeAlive += Time.deltaTime;

            // Pisca nos últimos 2 segundos
            if (timeAlive > lifetime - 2f && sr != null)
            {
                float alpha = Mathf.PingPong(Time.time * 4f, 1f);
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            if (timeAlive >= lifetime)
            {
                SoulManager.Instance?.DiscardPendingSouls();
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        SoulManager.Instance?.RecoverSouls();
        Destroy(gameObject);
    }

    /// <summary>
    /// Se o player morrer de novo, o orbe some (almas perdidas para sempre).
    /// </summary>
    private void OnPlayerDiedAgain(int _, Vector3 __)
    {
        // Pequeno delay visual antes de sumir
        Destroy(gameObject, 0.5f);
    }
}