using UnityEngine;
using ClassSystem.Spells;
using ClassSystem.Combat;

namespace ClassSystem.Runtime
{
    [RequireComponent(typeof(CharacterStats))]
    public class SpellCaster : MonoBehaviour
    {
        public Transform muzzle;
        CharacterStats _stats;
        float _lastCastTime;

        void Awake()
        {
            _stats = GetComponent<CharacterStats>();
        }

        public bool CanCast(Spell spell)
        {
            if (spell == null) return false;
            if (Time.time < _lastCastTime + spell.cooldown) return false;
            if (_stats.currentMana < spell.manaCost) return false;
            return true;
        }

        public bool Cast(Spell spell, Vector2 direction)
        {
            if (!CanCast(spell)) return false;
            if (!_stats.TryConsumeMana(spell.manaCost)) return false;
            _lastCastTime = Time.time;

            if (spell.castVfxPrefab)
            {
                Instantiate(spell.castVfxPrefab, transform.position, Quaternion.identity);
            }

            switch (spell.targeting)
            {
                case SpellTargetingMode.Self:
                    ApplySelf(spell);
                    break;
                case SpellTargetingMode.Projectile:
                    FireProjectile(spell, direction);
                    break;
            }
            return true;
        }

        void ApplySelf(Spell spell)
        {
            var target = (IDamageable)_stats;
            if (spell.effect == SpellEffectKind.Damage)
            {
                if (spell.continuous)
                {
                    gameObject.AddComponent<TimedEffect>().BeginDamageOverTime(spell, transform);
                }
                else
                {
                    var amt = spell.RollAmount();
                    var bundle = new DamageBundle(new DamagePacket { type = spell.element, amount = amt });
                    target.ReceiveDamage(bundle, this);
                }
            }
            else // Heal
            {
                if (spell.continuous)
                {
                    gameObject.AddComponent<TimedEffect>().BeginHealOverTime(spell, transform);
                }
                else
                {
                    target.ReceiveHealing(spell.RollAmount(), this);
                }
            }
        }

        void FireProjectile(Spell spell, Vector2 direction)
        {
            Vector3 pos = muzzle ? muzzle.position : transform.position;
            GameObject go;
            if (spell.projectilePrefab != null)
            {
                go = Instantiate(spell.projectilePrefab, pos, Quaternion.identity);
            }
            else
            {
                go = new GameObject($"{spell.displayName}_Projectile");
                go.transform.position = pos;
                go.layer = gameObject.layer;
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                var rb = go.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
                // Ensure it's visible even without a prefab
                var sr = go.AddComponent<SpriteRenderer>();
                var sp = Resources.Load<Sprite>("DemoSprites/Projectile");
                if (sp == null && spell.icon != null) sp = spell.icon; // last-resort: use spell icon
                sr.sprite = sp;
                sr.sortingOrder = 5;
            }
            // Ensure a SpriteRenderer exists even if prefab lacked one
            var srExisting = go.GetComponent<SpriteRenderer>();
            if (srExisting == null)
            {
                srExisting = go.AddComponent<SpriteRenderer>();
                var sp = Resources.Load<Sprite>("DemoSprites/Projectile");
                if (sp == null && spell.icon != null) sp = spell.icon;
                srExisting.sprite = sp;
                srExisting.sortingOrder = 5;
            }

            var proj = go.GetComponent<SpellProjectile>();
            if (proj == null) proj = go.AddComponent<SpellProjectile>();
            proj.sourceSpell = spell;
            proj.caster = transform;
            proj.direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.right;
            proj.speed = spell.projectileSpeed;
            proj.maxDistance = spell.projectileMaxDistance;
            proj.hitLayers = spell.hitLayers; // respect spell's configured layer mask
        }
    }
}
