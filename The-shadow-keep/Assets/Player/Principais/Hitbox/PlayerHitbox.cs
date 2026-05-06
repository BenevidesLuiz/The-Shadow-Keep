using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerHitbox — Coloque nos filhos do Player:
///   Player
///   ├── HitboxLeve   (Box Collider 2D, Is Trigger = ON) + este script (isHeavy = false)
///   └── HitboxForte  (Box Collider 2D, Is Trigger = ON) + este script (isHeavy = true)
///
/// COMO ATIVAR O DANO:
///   Opcao A (Animation Events): chame ActivateHitbox() e DeactivateHitbox() nos clips
///   Opcao B (automatico):       marque autoActivate = true — ativa sozinho quando atacar
/// </summary>
public class PlayerHitbox : MonoBehaviour {
    [Header("Configuracao")]
    [SerializeField] private bool isHeavy = false;
    [SerializeField] private float knockbackForce = 5f;

    [Tooltip("Ativa automaticamente durante ataques sem precisar de Animation Events")]
    [SerializeField] private bool autoActivate = true;

    private SoulslikeKnight knight;
    private Collider2D hitCollider;
    private HashSet<Collider2D> hitThisSwing = new HashSet<Collider2D>();

    // Controle do auto-activate
    private bool wasAttacking = false;

    private void Awake() {
        knight = GetComponentInParent<SoulslikeKnight>();
        hitCollider = GetComponent<Collider2D>();
        hitCollider.enabled = false;
    }

    private void Update() {
        if (!autoActivate || knight == null) return;

        // Detecta quando o knight entra/sai do estado de ataque via reflection
        // Usa a velocidade do animator como proxy — durante ataque a velocidade pode variar
        // Metodo mais simples: expoe uma propriedade publica no knight
        bool isAttacking = knight.IsAttacking;

        if (isAttacking && !wasAttacking) {
            ActivateHitbox();
        }
        else if (!isAttacking && wasAttacking) {
            DeactivateHitbox();
        }

        wasAttacking = isAttacking;
    }

    public void ActivateHitbox() {
        hitThisSwing.Clear();
        hitCollider.enabled = true;
    }

    public void DeactivateHitbox() {
        hitCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other) {
        if (other.transform.IsChildOf(knight.transform)) return;
        if (hitThisSwing.Contains(other)) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        hitThisSwing.Add(other);

        float damage = isHeavy ? knight.GetHeavyDamage() : knight.GetLightDamage();
        float dir = Mathf.Sign(other.transform.position.x - knight.transform.position.x);
        Vector2 knockback = new Vector2(dir * knockbackForce, 2f);

        enemy.TakeDamage(damage, knockback);
        Debug.Log($"[PlayerHitbox] Dano {damage} aplicado em {other.name}");
    }
}