using System.Collections;
using UnityEngine;
using ClassSystem.Combat;

namespace ClassSystem.Spells
{
    public class TimedEffect : MonoBehaviour
    {
        Coroutine _routine;

        public TimedEffect BeginDamageOverTime(Spell spell, Transform caster)
        {
            Stop();
            _routine = StartCoroutine(RunDOT(spell, caster));
            return this;
        }

        public TimedEffect BeginHealOverTime(Spell spell, Transform caster)
        {
            Stop();
            _routine = StartCoroutine(RunHOT(spell, caster));
            return this;
        }

        IEnumerator RunDOT(Spell spell, Transform caster)
        {
            var damageable = GetComponentInParent<IDamageable>();
            if (damageable == null || spell == null) yield break;
            float time = 0f;
            float tick = Mathf.Max(0.05f, spell.tickInterval);
            while (time < spell.duration)
            {
                int amount = spell.RollAmount();
                var bundle = new DamageBundle(new DamagePacket { type = spell.element, amount = amount });
                damageable.ReceiveDamage(bundle, caster);
                time += tick;
                yield return new WaitForSeconds(tick);
            }
            Destroy(this);
        }

        IEnumerator RunHOT(Spell spell, Transform caster)
        {
            var damageable = GetComponentInParent<IDamageable>();
            if (damageable == null || spell == null) yield break;
            float time = 0f;
            float tick = Mathf.Max(0.05f, spell.tickInterval);
            while (time < spell.duration)
            {
                int amount = spell.RollAmount();
                damageable.ReceiveHealing(amount, caster);
                time += tick;
                yield return new WaitForSeconds(tick);
            }
            Destroy(this);
        }

        void Stop()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = null;
        }

        void OnDestroy()
        {
            Stop();
        }
    }
}

