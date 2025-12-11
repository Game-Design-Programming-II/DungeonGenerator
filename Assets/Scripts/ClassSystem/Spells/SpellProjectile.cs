using UnityEngine;
using ClassSystem.Combat;

namespace ClassSystem.Spells
{
    public class SpellProjectile : MonoBehaviour
    {
        public Spell sourceSpell;
        public Transform caster;
        public Vector2 direction;
        public float speed = 8f;
        public float maxDistance = 10f;
        public float lifetime = 5f;
        public LayerMask hitLayers = ~0; // which layers this projectile should affect/collide with

        private Vector2 _start;
        private float _time;

        void Start()
        {
            _start = transform.position;
            // Proactively ignore collisions with caster's colliders (including children)
            TryIgnoreCasterCollisions();
        }

        void Update()
        {
            _time += Time.deltaTime;
            if (_time >= lifetime)
            {
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)(direction.normalized * speed * Time.deltaTime);
            if (Vector2.Distance(_start, transform.position) >= maxDistance)
            {
                Destroy(gameObject);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!ShouldProcessCollider(other)) return;

            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                ApplyEffect(dmg);
            }
            if (sourceSpell != null && sourceSpell.impactVfxPrefab != null)
            {
                Instantiate(sourceSpell.impactVfxPrefab, other.bounds.center, Quaternion.identity);
            }
            Destroy(gameObject);
        }

        // In case a prefab uses non-trigger collider by mistake, handle that too.
        void OnCollisionEnter2D(Collision2D collision)
        {
            var other = collision.collider;
            if (!ShouldProcessCollider(other)) return;

            var dmg = other.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                ApplyEffect(dmg);
            }
            if (sourceSpell != null && sourceSpell.impactVfxPrefab != null)
            {
                Instantiate(sourceSpell.impactVfxPrefab, other.bounds.center, Quaternion.identity);
            }
            Destroy(gameObject);
        }

        void ApplyEffect(IDamageable target)
        {
            if (sourceSpell == null || target == null) return;
            switch (sourceSpell.effect)
            {
                case SpellEffectKind.Damage:
                    if (sourceSpell.continuous)
                    {
                        target.GetTransform().gameObject.AddComponent<TimedEffect>()
                            .BeginDamageOverTime(sourceSpell, caster);
                    }
                    else
                    {
                        var amount = sourceSpell.RollAmount();
                        var bundle = new DamageBundle(new DamagePacket { type = sourceSpell.element, amount = amount });
                        target.ReceiveDamage(bundle, caster);
                    }
                    break;
                case SpellEffectKind.Heal:
                    if (sourceSpell.continuous)
                    {
                        target.GetTransform().gameObject.AddComponent<TimedEffect>()
                            .BeginHealOverTime(sourceSpell, caster);
                    }
                    else
                    {
                        var amount = sourceSpell.RollAmount();
                        target.ReceiveHealing(amount, caster);
                    }
                    break;
            }
        }

        bool ShouldProcessCollider(Collider2D other)
        {
            if (other == null) return false;
            // Ignore our own caster (and their children)
            if (caster != null && (other.transform == caster || other.transform.IsChildOf(caster)))
                return false;
            // Respect the hit layer mask
            int layer = other.gameObject.layer;
            if ((hitLayers.value & (1 << layer)) == 0)
                return false;
            return true;
        }

        void TryIgnoreCasterCollisions()
        {
            if (caster == null) return;
            var myCols = GetComponentsInChildren<Collider2D>(true);
            var casterCols = caster.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < myCols.Length; i++)
            {
                for (int j = 0; j < casterCols.Length; j++)
                {
                    if (myCols[i] && casterCols[j])
                        Physics2D.IgnoreCollision(myCols[i], casterCols[j], true);
                }
            }
        }
    }
}
