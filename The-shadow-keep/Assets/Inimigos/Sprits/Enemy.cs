using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour {
    protected enum State { Wandering, Chasing, Windup, Attacking, Recovery, Hurt, Dead }
    protected State state = State.Wandering;

    protected Rigidbody2D rb;
    protected Animator anim;
    protected Transform player;
    protected PlayerBase playerKnight;

    private const string A_IDLE = "Idle";
    private const string A_WALK = "Walk";
    private const string A_ATTACK = "Attack";
    private const string A_HURT = "Hurt";
    private const string A_DEATH = "Death";


    private string currentAnimName = "";

    [Header("Áudio (Música)")]
    [SerializeField] private AudioClip musicaLuta;
    [SerializeField] private AudioClip musicaVitoria;
    private AudioSource audioSource;
    private bool tocandoLuta = false;
    private bool tocouVitoria = false;

    [Tooltip("Tempo em segundos que a música de vitória vai tocar")]
    [SerializeField] private float tempoMusicaVitoria = 15f;

    [Header("Vida")]
    [SerializeField] protected float maxHealth = 50f;
    [SerializeField] protected float hurtStunTime = 0.35f;
    protected float hp;

    [Header("Drop de Almas")]
    [SerializeField] private int soulDropAmount = 100;
    [Tooltip("Variação aleatória: valor real = soulDropAmount ± soulDropVariance")]
    [SerializeField] private int soulDropVariance = 20;

    [Header("Movimento")]
    [SerializeField] private float wanderSpeed = 1.8f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float stopDistance = 1.1f;

    [Header("Wander")]
    [SerializeField] private float minWanderTime = 1.5f;
    [SerializeField] private float maxWanderTime = 3.5f;
    [SerializeField] private float wallCheckDist = 0.4f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Deteccao de Borda")]
    [SerializeField] private float edgeCheckDist = 0.6f;
    [SerializeField] private Transform groundCheck;

    private float wanderDir = 1f;
    private float wanderTimer = 0f;
    private bool isWanderPaused = false;

    [Header("Deteccao")]
    [SerializeField] private float aggroRange = 6f;
    [SerializeField] private float deaggroRange = 10f;

    [Header("Ataque")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float windupTime = 0.2f;
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float recoveryTime = 0.5f;
    [SerializeField] private float recoverySpeed = 2f;
    [SerializeField] private float attackRange = 1.3f;

    private Vector2 pendingKnockback;

    public event System.Action<float, float> OnHealthChanged;

    protected virtual void Start() {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        hp = maxHealth;

        // Configuração do Áudio de Luta
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0f; // Som 2D (toca igual nas duas caixas de som)
        audioSource.loop = true; // A música de luta repete até acabar o combate

        BuscarPlayer();

        wanderDir = Random.value > 0.5f ? 1f : -1f;
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
    }

    private void BuscarPlayer() {
        if (player != null) return;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) {
            PlayerBase found = FindFirstObjectByType<PlayerBase>();
            if (found != null) p = found.gameObject;
        }

        if (p != null) {
            player = p.transform;
            playerKnight = p.GetComponent<PlayerBase>();

            Collider2D[] enemyCols = GetComponents<Collider2D>();
            Collider2D[] playerCols = p.GetComponents<Collider2D>();
            foreach (Collider2D ec in enemyCols) {
                foreach (Collider2D pc in playerCols) {
                    Physics2D.IgnoreCollision(ec, pc, true);
                }
            }
        }
    }

    private void Update() {
        if (state == State.Dead) return;

        if (player == null) BuscarPlayer();

        if (pendingKnockback != Vector2.zero) {
            rb.linearVelocity = pendingKnockback;
            pendingKnockback = Vector2.zero;
        }

        switch (state) {
            case State.Wandering: UpdateWander(); break;
            case State.Chasing: UpdateChasing(); break;
        }

        UpdateFacing();
    }

    private void FixedUpdate() {
        if (state == State.Dead || state == State.Hurt ||
            state == State.Windup || state == State.Attacking || state == State.Recovery) {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void UpdateWander() {
        // Se o player chegar perto, inicia a perseguição e a música!
        if (player != null && Dist() <= aggroRange) {
            state = State.Chasing;
            TocarMusicaLuta();
            return;
        }

        if (isWanderPaused) {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            PlayAnim(A_IDLE);
            return;
        }

        bool hitWall = Physics2D.Raycast(transform.position, new Vector2(wanderDir, 0f), wallCheckDist, groundLayer);
        bool atEdge = false;
        if (groundCheck != null) {
            Vector3 checkPos = groundCheck.position + new Vector3(wanderDir * edgeCheckDist, 0f, 0f);
            atEdge = !Physics2D.OverlapCircle(checkPos, 0.15f, groundLayer);
        }

        if (hitWall || atEdge) { StartCoroutine(WanderPause()); return; }

        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f) { StartCoroutine(WanderPause()); return; }

        rb.linearVelocity = new Vector2(wanderDir * wanderSpeed, rb.linearVelocity.y);
        PlayAnim(A_WALK);
    }

    private IEnumerator WanderPause() {
        isWanderPaused = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        PlayAnim(A_IDLE);
        yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));
        wanderDir = -wanderDir;
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
        isWanderPaused = false;
    }

    private void UpdateChasing() {
        if (player == null) { state = State.Wandering; PararMusicaLuta(); return; }

        float dist = Dist();

        // Se o player fugir para longe, desiste e para a música
        if (dist > deaggroRange) {
            state = State.Wandering;
            PararMusicaLuta();
            return;
        }

        if (dist <= stopDistance) {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            StartCoroutine(AttackSequence());
            return;
        }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
        PlayAnim(A_WALK);
    }

    private IEnumerator AttackSequence() {
        state = State.Windup;
        rb.linearVelocity = Vector2.zero;
        PlayAnim(A_IDLE);
        yield return new WaitForSeconds(windupTime);
        if (state != State.Windup) yield break;

        state = State.Attacking;
        PlayAnim(A_ATTACK);
        yield return new WaitForSeconds(attackDuration * 0.5f);

        if (player != null && Dist() <= attackRange * 1.4f) {
            if (playerKnight == null) playerKnight = player.GetComponent<PlayerBase>();
            playerKnight?.TakeDamage(attackDamage);
        }

        yield return new WaitForSeconds(attackDuration * 0.5f);
        if (state != State.Attacking) yield break;

        state = State.Recovery;
        float recoverDir = player != null ? -Mathf.Sign(player.position.x - transform.position.x) : wanderDir;
        float elapsed = 0f;
        while (elapsed < recoveryTime) {
            rb.linearVelocity = new Vector2(recoverDir * recoverySpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (state == State.Recovery) {
            if (player != null && Dist() <= deaggroRange) {
                state = State.Chasing;
            }
            else {
                state = State.Wandering;
                PararMusicaLuta();
            }
        }
    }

    public virtual void TakeDamage(float amount, Vector2 knockback = default) {
        if (state == State.Dead) return;

        TocarMusicaLuta();

        hp = Mathf.Max(0f, hp - amount);
        OnHealthChanged?.Invoke(hp, maxHealth);

        if (knockback != default) pendingKnockback = knockback;

        if (hp <= 0f) { StartCoroutine(DieRoutine()); return; }

        StopAllCoroutines();
        StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine() {
        state = State.Hurt;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        PlayAnim(A_HURT);
        yield return new WaitForSeconds(hurtStunTime);

        if (state == State.Hurt)
            state = (player != null && Dist() <= deaggroRange) ? State.Chasing : State.Wandering;
    }

    private IEnumerator DieRoutine() {
        StopAllCoroutines();
        state = State.Dead;
        rb.linearVelocity = Vector2.zero;
        PlayAnim(A_DEATH);

        PararMusicaLuta();
        TocarMusicaVitoria();

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        DropSouls();

        float destroyDelay = (anim != null && anim.runtimeAnimatorController != null) ? 1.5f : 0.1f;
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    private void TocarMusicaLuta() {
        if (!tocandoLuta && musicaLuta != null) {
            tocandoLuta = true;
            audioSource.clip = musicaLuta;
            audioSource.Play();
        }
    }

    private void PararMusicaLuta() {
        if (tocandoLuta && audioSource != null) {
            tocandoLuta = false;
            audioSource.Stop();
        }
    }

    private void TocarMusicaVitoria() {
        if (!tocouVitoria && musicaVitoria != null) {
            tocouVitoria = true;

            GameObject tocaFitasFantasma = new GameObject("MusicaVitoria_" + gameObject.name);
            AudioSource sourceTemporario = tocaFitasFantasma.AddComponent<AudioSource>();

            sourceTemporario.clip = musicaVitoria;
            sourceTemporario.spatialBlend = 0f;
            sourceTemporario.volume = 0.8f;
            sourceTemporario.Play();

            Destroy(tocaFitasFantasma, tempoMusicaVitoria);
        }
    }


    private void DropSouls() {
        if (SoulManager.Instance == null) return;

        int variance = Random.Range(-soulDropVariance, soulDropVariance + 1);
        int finalDrop = Mathf.Max(1, soulDropAmount + variance);

        SoulManager.Instance.AddSouls(finalDrop);
        Debug.Log($"[Enemy] Morreu → +{finalDrop} almas dropadas.");
    }

    private float Dist() =>
        player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

    private void UpdateFacing() {
        if (state == State.Dead) return;

        float dir = 0f;
        if (state == State.Chasing && player != null) dir = player.position.x - transform.position.x;
        else if (state == State.Wandering) dir = wanderDir;
        else if (rb.linearVelocity.x != 0f) dir = rb.linearVelocity.x;

        if (Mathf.Abs(dir) > 0.01f) {
            float tamanhoOriginalY = transform.localScale.y;
            float tamanhoOriginalX = Mathf.Abs(transform.localScale.x);
            transform.localScale = new Vector3(dir > 0 ? tamanhoOriginalX : -tamanhoOriginalX, tamanhoOriginalY, 1f);
        }
    }

    private void PlayAnim(string name) {
        if (anim != null && anim.runtimeAnimatorController != null) {
            if (currentAnimName == name) return;
            anim.Play(name);
            currentAnimName = name; 
        }
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, deaggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}