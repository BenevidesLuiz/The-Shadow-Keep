using System.Collections;
using UnityEngine;

/// <summary>
/// Enemy — Estilo Dark Souls.
/// Anda pelo cenario detectando paredes/bordas → ao ver o player persegue →
/// chega perto → windup → ataque → recua → repete.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    private enum State { Wandering, Chasing, Windup, Attacking, Recovery, Hurt, Dead }
    private State state = State.Wandering;

    // ── Componentes ──────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private SoulslikeKnight playerKnight;

    // ── Animacoes ────────────────────────────────────────────────────────
    private const string A_IDLE = "Idle";
    private const string A_WALK = "Walk";
    private const string A_ATTACK = "Attack";
    private const string A_HURT = "Hurt";
    private const string A_DEATH = "Death";

    // ── Vida ─────────────────────────────────────────────────────────────
    [Header("Vida")]
    [SerializeField] private float maxHealth = 50f;
    [SerializeField] private float hurtStunTime = 0.35f;
    private float hp;

    // ── Movimento ────────────────────────────────────────────────────────
    [Header("Movimento")]
    [SerializeField] private float wanderSpeed = 1.8f;  // velocidade andando pelo cenario
    [SerializeField] private float chaseSpeed = 3.5f;  // velocidade perseguindo
    [SerializeField] private float stopDistance = 1.1f;  // distancia para parar e atacar

    // ── Wander ───────────────────────────────────────────────────────────
    [Header("Wander")]
    [SerializeField] private float minWanderTime = 1.5f; // tempo minimo andando numa direcao
    [SerializeField] private float maxWanderTime = 3.5f; // tempo maximo
    [SerializeField] private float wallCheckDist = 0.4f; // distancia para detectar parede
    [SerializeField] private LayerMask groundLayer;       // layer do chao (para borda)

    [Header("Deteccao de Borda")]
    [SerializeField] private float edgeCheckDist = 0.6f;  // distancia a frente do pe
    [SerializeField] private Transform groundCheck;          // transform no pe do inimigo

    private float wanderDir = 1f;   // direcao atual: 1 = direita, -1 = esquerda
    private float wanderTimer = 0f;   // tempo restante nessa direcao
    private bool isWanderPaused = false;

    // ── Deteccao ─────────────────────────────────────────────────────────
    [Header("Deteccao")]
    [SerializeField] private float aggroRange = 6f;
    [SerializeField] private float deaggroRange = 10f;

    // ── Ataque ───────────────────────────────────────────────────────────
    [Header("Ataque")]
    [SerializeField] private float attackDamage = 15f;
    [SerializeField] private float windupTime = 0.2f;
    [SerializeField] private float attackDuration = 0.35f;
    [SerializeField] private float recoveryTime = 0.5f;
    [SerializeField] private float recoverySpeed = 2f;
    [SerializeField] private float attackRange = 1.3f;

    // ── Knockback ────────────────────────────────────────────────────────
    private Vector2 pendingKnockback;

    // ── Eventos ──────────────────────────────────────────────────────────
    public event System.Action<float, float> OnHealthChanged;

    // ====================================================================
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        hp = maxHealth;

        // Tenta pela Tag primeiro
        GameObject p = GameObject.FindGameObjectWithTag("Player");

        // Fallback: busca pelo componente diretamente se Tag falhar
        if (p == null)
        {
            SoulslikeKnight found = FindFirstObjectByType<SoulslikeKnight>();
            if (found != null) p = found.gameObject;
        }

        if (p != null)
        {
            player = p.transform;
            playerKnight = p.GetComponent<SoulslikeKnight>();
            Debug.Log($"[Enemy] Player encontrado: {p.name}, Tag={p.tag}, Knight={playerKnight != null}");
        }
        else
        {
            Debug.LogError("[Enemy] PLAYER NAO ENCONTRADO por Tag nem por componente!");
        }

        // Comeca andando numa direcao aleatoria
        wanderDir = Random.value > 0.5f ? 1f : -1f;
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);

        // CRITICO: ignora colisao fisica entre inimigo e player
        // Isso evita que o inimigo empurre o player e trave o movimento
        GameObject playerObj = player != null ? player.gameObject : GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Collider2D[] enemyCols = GetComponents<Collider2D>();
            Collider2D[] playerCols = playerObj.GetComponents<Collider2D>();

            foreach (Collider2D ec in enemyCols)
                foreach (Collider2D pc in playerCols)
                    Physics2D.IgnoreCollision(ec, pc, true);

            Debug.Log("[Enemy] Colisao fisica com player DESATIVADA");
        }
    }

    private void Update()
    {
        if (state == State.Dead) return;

        if (pendingKnockback != Vector2.zero)
        {
            rb.linearVelocity = pendingKnockback;
            pendingKnockback = Vector2.zero;
        }

        switch (state)
        {
            case State.Wandering: UpdateWander(); break;
            case State.Chasing: UpdateChasing(); break;
        }

        UpdateFacing();
    }

    private void FixedUpdate()
    {
        // Para completamente durante estados que nao devem mover
        // Isso evita que o inimigo deslize e empurre o player
        if (state == State.Dead ||
            state == State.Hurt ||
            state == State.Windup ||
            state == State.Attacking ||
            state == State.Recovery)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    // ── WANDER ───────────────────────────────────────────────────────────
    // Anda pelo cenario, detecta parede e borda, muda de direcao
    private void UpdateWander()
    {
        // Detectou o player?
        if (player != null && Dist() <= aggroRange)
        {
            state = State.Chasing;
            return;
        }

        if (isWanderPaused)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            PlayAnim(A_IDLE);
            return;
        }

        // Checa parede a frente
        bool hitWall = Physics2D.Raycast(
            transform.position,
            new Vector2(wanderDir, 0f),
            wallCheckDist,
            groundLayer);

        // Checa borda (se nao tem chao a frente)
        bool atEdge = false;
        if (groundCheck != null)
        {
            Vector3 checkPos = groundCheck.position + new Vector3(wanderDir * edgeCheckDist, 0f, 0f);
            atEdge = !Physics2D.OverlapCircle(checkPos, 0.15f, groundLayer);
        }

        if (hitWall || atEdge)
        {
            // Bate na parede ou borda: pausa e vira
            StartCoroutine(WanderPause());
            return;
        }

        // Diminui timer da direcao atual
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0f)
        {
            // Tempo esgotou: pausa e escolhe nova direcao
            StartCoroutine(WanderPause());
            return;
        }

        rb.linearVelocity = new Vector2(wanderDir * wanderSpeed, rb.linearVelocity.y);
        PlayAnim(A_WALK);
    }

    private IEnumerator WanderPause()
    {
        isWanderPaused = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        PlayAnim(A_IDLE);

        // Pausa breve (0.3 a 0.8s) antes de mudar de direcao
        yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));

        // Escolhe nova direcao e novo timer
        wanderDir = -wanderDir;
        wanderTimer = Random.Range(minWanderTime, maxWanderTime);
        isWanderPaused = false;
    }

    // ── CHASING ──────────────────────────────────────────────────────────
    private void UpdateChasing()
    {
        if (player == null) { state = State.Wandering; return; }

        float dist = Dist();

        // Perdeu o player: volta a andar
        if (dist > deaggroRange)
        {
            state = State.Wandering;
            return;
        }

        // Chegou perto: ataca
        if (dist <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            StartCoroutine(AttackSequence());
            return;
        }

        float dir = Mathf.Sign(player.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
        PlayAnim(A_WALK);
    }

    // ── ATAQUE ───────────────────────────────────────────────────────────
    private IEnumerator AttackSequence()
    {
        state = State.Windup;
        rb.linearVelocity = Vector2.zero;
        PlayAnim(A_IDLE);
        yield return new WaitForSeconds(windupTime);
        if (state != State.Windup) yield break;

        state = State.Attacking;
        PlayAnim(A_ATTACK);
        yield return new WaitForSeconds(attackDuration * 0.5f);

        float distAtHit = Dist();
        Debug.Log($"[Enemy] HIT CHECK — Dist={distAtHit:F2} / Range={attackRange * 1.4f:F2} / State={state} / Knight={playerKnight != null}");

        if (player != null && distAtHit <= attackRange * 1.4f)
        {
            if (playerKnight != null)
            {
                playerKnight.TakeDamage(attackDamage);
                Debug.Log($"[Enemy] DANO APLICADO: {attackDamage}");
            }
            else
            {
                // Tenta pegar o componente de novo caso tenha falhado no Start
                playerKnight = player.GetComponent<SoulslikeKnight>();
                if (playerKnight != null)
                {
                    playerKnight.TakeDamage(attackDamage);
                    Debug.Log($"[Enemy] DANO APLICADO (retry): {attackDamage}");
                }
                else
                {
                    Debug.LogError("[Enemy] SoulslikeKnight NAO encontrado no Player!");
                }
            }
        }

        yield return new WaitForSeconds(attackDuration * 0.5f);
        if (state != State.Attacking) yield break;

        // Recua
        state = State.Recovery;
        float recoverDir = player != null ? -Mathf.Sign(player.position.x - transform.position.x) : wanderDir;
        float elapsed = 0f;
        while (elapsed < recoveryTime)
        {
            rb.linearVelocity = new Vector2(recoverDir * recoverySpeed, rb.linearVelocity.y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (state == State.Recovery)
            state = (player != null && Dist() <= deaggroRange) ? State.Chasing : State.Wandering;
    }

    // ── DANO ─────────────────────────────────────────────────────────────
    public void TakeDamage(float amount, Vector2 knockback = default)
    {
        if (state == State.Dead) return;

        hp = Mathf.Max(0f, hp - amount);
        OnHealthChanged?.Invoke(hp, maxHealth);

        if (knockback != default)
            pendingKnockback = knockback;

        if (hp <= 0f) { StartCoroutine(DieRoutine()); return; }

        StopAllCoroutines();
        StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        state = State.Hurt;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        PlayAnim(A_HURT);
        yield return new WaitForSeconds(hurtStunTime);

        if (state == State.Hurt)
            state = (player != null && Dist() <= deaggroRange) ? State.Chasing : State.Wandering;
    }

    private IEnumerator DieRoutine()
    {
        StopAllCoroutines();
        state = State.Dead;
        rb.linearVelocity = Vector2.zero;
        PlayAnim(A_DEATH);

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        // Se tem animacao de morte, espera ela terminar; senao some rapido
        float destroyDelay = (anim != null && anim.runtimeAnimatorController != null) ? 1.5f : 0.1f;
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }

    // ── AUXILIARES ───────────────────────────────────────────────────────
    private float Dist() =>
        player != null ? Vector2.Distance(transform.position, player.position) : float.MaxValue;

    private void UpdateFacing()
    {
        if (state == State.Dead) return;

        float dir = 0f;
        if (state == State.Chasing && player != null)
            dir = player.position.x - transform.position.x;
        else if (state == State.Wandering)
            dir = wanderDir;
        else if (rb.linearVelocity.x != 0f)
            dir = rb.linearVelocity.x;

        if (Mathf.Abs(dir) > 0.01f)
            transform.localScale = new Vector3(dir > 0 ? 1f : -1f, 1f, 1f);
    }

    private void PlayAnim(string name)
    {
        if (anim != null && anim.runtimeAnimatorController != null)
            anim.Play(name);
    }

    // ── GIZMOS ───────────────────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, deaggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stopDistance);

        // Mostra raio de deteccao de parede
        if (Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, Vector3.right * wallCheckDist);
        Gizmos.DrawRay(transform.position, Vector3.left * wallCheckDist);
    }
}