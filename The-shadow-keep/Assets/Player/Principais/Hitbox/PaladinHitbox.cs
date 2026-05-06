using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PaladinHitbox — Versão sagrada do PlayerHitbox para o PaladinKnight.
///
/// HIERARQUIA NO PREFAB:
///   Player_Paladin
///   ├── HitboxSagrado   (Box Collider 2D, Is Trigger = ON) + PaladinHitbox (isHeavy = false)
///   └── HitboxDivino    (Box Collider 2D, Is Trigger = ON) + PaladinHitbox (isHeavy = true)
///
/// DIFERENÇAS em relação ao PlayerHitbox do Knight:
///
///   ATAQUE LEVE (isHeavy = false) — "Ataque Sagrado"
///     - Aplica dano normal
///     - Chama paladin.OnHolyHit(dano) → life steal de 20% do dano
///     - Pode tocar múltiplos inimigos por swing (padrão)
///
///   ATAQUE PESADO (isHeavy = true) — "Golpe Divino"
///     - Aplica dano pesado com knockback maior
///     - Ativa uma área de dano sagrado ao redor do ponto de impacto
///       (raio divineBlastRadius, atinge todos os inimigos na área)
///     - Congela o inimigo atingido por divineStunDuration segundos
///     - NÃO aplica life steal (troca cura por poder de área)
///
/// COMO ATIVAR O DANO:
///   Opção A (Animation Events): chame ActivateHitbox() e DeactivateHitbox() nos clips
///   Opção B (automático):       marque autoActivate = true
/// </summary>
public class PaladinHitbox : MonoBehaviour {

    // ------------------------------------------------------------------ //
    //  Inspector
    // ------------------------------------------------------------------ //
    [Header("Configuração")]
    [SerializeField] private bool isHeavy = false;
    [SerializeField] private float knockbackForce = 5f;

    [Tooltip("Ativa automaticamente enquanto IsAttacking = true no Paladino")]
    [SerializeField] private bool autoActivate = true;

    [Header("Ataque Pesado — Golpe Divino")]
    [Tooltip("Raio da explosão sagrada ao redor do ponto de impacto")]
    [SerializeField] private float divineBlastRadius = 2.5f;

    [Tooltip("Multiplicador de dano para inimigos atingidos pela explosão (não o alvo principal)")]
    [SerializeField] private float blastDamageRatio = 0.5f;

    [Tooltip("Duração do stun no alvo principal do Golpe Divino")]
    [SerializeField] private float divineStunDuration = 0.8f;

    [Tooltip("Prefab de efeito visual/sonoro no impacto do Golpe Divino (opcional)")]
    [SerializeField] private GameObject divineImpactVFX;

    [Tooltip("Layer dos inimigos para o OverlapCircle do Golpe Divino")]
    [SerializeField] private LayerMask enemyLayer;

    // ------------------------------------------------------------------ //
    //  Referências
    // ------------------------------------------------------------------ //
    private PaladinKnight paladin;
    private Collider2D hitCollider;
    private HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();
    private bool wasAttacking = false;

    // ================================================================== //
    //  UNITY CALLBACKS
    // ================================================================== //

    private void Awake() {
        paladin = GetComponentInParent<PaladinKnight>();
        hitCollider = GetComponent<Collider2D>();
        hitCollider.enabled = false;

        if (paladin == null)
            Debug.LogError("[PaladinHitbox] PaladinKnight não encontrado no pai! " +
                           "Certifique-se de que este script está num filho do Player_Paladin.");
    }

    private void Update() {
        if (!autoActivate || paladin == null) return;

        bool isAttacking = paladin.IsAttacking;

        if (isAttacking && !wasAttacking) ActivateHitbox();
        else if (!isAttacking && wasAttacking) DeactivateHitbox();

        wasAttacking = isAttacking;
    }

    // ================================================================== //
    //  ATIVAR / DESATIVAR  (chamáveis por Animation Events)
    // ================================================================== //

    public void ActivateHitbox() {
        hitThisSwing.Clear();
        hitCollider.enabled = true;
    }

    public void DeactivateHitbox() {
        hitCollider.enabled = false;
    }

    // ================================================================== //
    //  COLISÃO COM INIMIGO
    // ================================================================== //

    private void OnTriggerEnter2D(Collider2D other) {
        // Ignora filhos do próprio paladino e hits já registrados neste swing
        if (other.transform.IsChildOf(paladin.transform)) return;
        if (hitThisSwing.Contains(other)) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        hitThisSwing.Add(other);

        if (isHeavy)
            ApplyDivineStrike(enemy, other);
        else
            ApplyHolyAttack(enemy, other);
    }

    // ================================================================== //
    //  ATAQUE SAGRADO (leve)
    //  Dano normal + notifica o Paladino para o life steal
    // ================================================================== //

    private void ApplyHolyAttack(Enemy enemy, Collider2D col) {
        float damage = paladin.GetLightDamage();
        float dir = Mathf.Sign(col.transform.position.x - paladin.transform.position.x);
        Vector2 knockback = new Vector2(dir * knockbackForce, 1.5f);

        enemy.TakeDamage(damage, knockback);

        // Life steal — devolve uma fração do dano como vida ao Paladino
        paladin.OnHolyHit(damage);

        Debug.Log($"[PaladinHitbox] Ataque Sagrado: {damage} dano em {col.name} | life steal ativado");
    }

    // ================================================================== //
    //  GOLPE DIVINO (pesado)
    //  Dano alto no alvo + explosão em área + stun + VFX
    // ================================================================== //

    private void ApplyDivineStrike(Enemy primaryEnemy, Collider2D col) {
        float heavyDamage = paladin.GetHeavyDamage();
        float dir = Mathf.Sign(col.transform.position.x - paladin.transform.position.x);
        Vector2 knockback = new Vector2(dir * knockbackForce * 1.5f, 3f);  // knockback maior

        // ── 1. Dano no alvo principal ──
        primaryEnemy.TakeDamage(heavyDamage, knockback);

        // ── 2. Stun no alvo principal ──
        StartCoroutine(StunEnemy(primaryEnemy, divineStunDuration));

        // ── 3. Explosão sagrada em área ──
        Vector3 impactPoint = col.transform.position;
        ApplyDivineBlast(impactPoint, primaryEnemy);

        // ── 4. VFX no ponto de impacto ──
        if (divineImpactVFX != null) {
            GameObject vfx = Instantiate(divineImpactVFX, impactPoint, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        Debug.Log($"[PaladinHitbox] Golpe Divino: {heavyDamage} dano em {col.name} + explosão r={divineBlastRadius}");
    }

    // ── Explosão sagrada em área ──────────────────────────────────────

    private void ApplyDivineBlast(Vector3 center, Enemy excludePrimary) {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, divineBlastRadius, enemyLayer);

        float blastDamage = paladin.GetHeavyDamage() * blastDamageRatio;

        foreach (Collider2D hit in hits) {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy == null || enemy == excludePrimary) continue;
            if (hitThisSwing.Contains(hit)) continue;

            hitThisSwing.Add(hit);

            float blastDir = Mathf.Sign(hit.transform.position.x - center.x);
            Vector2 blastKnockback = new Vector2(blastDir * knockbackForce * 0.7f, 1f);

            enemy.TakeDamage(blastDamage, blastKnockback);
            Debug.Log($"[PaladinHitbox] Explosão Divina: {blastDamage} dano em {hit.name}");
        }
    }

    // ── Stun temporário ───────────────────────────────────────────────

    /// <summary>
    /// Aplica stun chamando TakeDamage com 0 dano e knockback zero repetidamente
    /// durante divineStunDuration. Funciona com qualquer Enemy sem modificar a classe.
    ///
    /// Alternativa: se Enemy tiver um método Stun(float), chame diretamente.
    /// </summary>
    private IEnumerator StunEnemy(Enemy enemy, float duration) {
        if (enemy == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration && enemy != null) {
            // Reaplica o hurt state sem dano para manter o inimigo travado
            enemy.TakeDamage(0f, Vector2.zero);
            elapsed += 0.15f;
            yield return new WaitForSeconds(0.15f);
        }
    }

    // ================================================================== //
    //  GIZMOS  — visualiza o raio do Golpe Divino no Editor
    // ================================================================== //

    private void OnDrawGizmosSelected() {
        if (!isHeavy) return;
        Gizmos.color = new Color(0.8f, 0.7f, 0.2f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, divineBlastRadius);
        Gizmos.color = new Color(0.8f, 0.7f, 0.2f, 0.1f);
        Gizmos.DrawSphere(transform.position, divineBlastRadius);
    }
}