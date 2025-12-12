using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Common;
using Photon.Pun;

namespace Character
{
    // Holds health and applies typed damage/healing including over-time effects.
    public class CharacterHealth : MonoBehaviour, IPunObservable
    {
        [Header("Health")]
        [Tooltip("Maximum health points.")]
        public float maxHealth = 10f;

        [Tooltip("Starting health.")]
        public float startHealth = 10f;

        [SerializeField] private float currentHealth;
        public float CurrentHealth => currentHealth;

        public bool IsDead => currentHealth <= 0f;

        [Header("XP Rewards")]
        [Tooltip("Flat experience granted to the last attacker when this character dies.")]
        public int xpOnDeath = 100;

        [Header("Events")] 
        public UnityEvent<float, float> OnHealthChanged; // (current, max)
        public UnityEvent OnDeath;

        // Internal status effect representation.
        private class ActiveEffect
        {
            public bool isHealing;
            public DamageType type;
            public float remaining;
            public float tickInterval;
            public float tickTimer;
            public float amountPerTick;
            public GameObject source;
        }

        private readonly List<ActiveEffect> _effects = new List<ActiveEffect>();
        private ClassSystem.Runtime.CharacterStats _lastDamager;
        private bool _deathHandled;

        private void Awake()
        {
            currentHealth = Mathf.Clamp(startHealth, 0f, maxHealth);
            RaiseHealthChanged();
        }

        private void Update()
        {
            if (_effects.Count == 0) return;

            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                var e = _effects[i];
                e.tickTimer -= Time.deltaTime;
                e.remaining -= Time.deltaTime;

                if (e.tickTimer <= 0f)
                {
                    e.tickTimer += Mathf.Max(0.01f, e.tickInterval);
                    if (e.isHealing)
                        ApplyHealing(Mathf.Max(0f, e.amountPerTick), e.type, e.source);
                    else
                        ApplyDamage(Mathf.Max(0f, e.amountPerTick), e.type, e.source);
                }

                if (e.remaining <= 0f)
                {
                    _effects.RemoveAt(i);
                }
            }
        }

        // Apply immediate typed damage.
        public void ApplyDamage(float amount, DamageType type, GameObject source = null)
        {
            if (IsDead) return;
            _lastDamager = ResolveStatsFromSource(source);
            currentHealth = Mathf.Clamp(currentHealth - Mathf.Max(0f, amount), 0f, maxHealth);
            RaiseHealthChanged();
            if (IsDead)
            {
                HandleDeath();
            }
        }

        // Apply immediate healing.
        public void ApplyHealing(float amount, DamageType type, GameObject source = null)
        {
            if (IsDead) return;
            currentHealth = Mathf.Clamp(currentHealth + Mathf.Max(0f, amount), 0f, maxHealth);
            RaiseHealthChanged();
        }

        // Apply an over-time effect. totalAmount is applied across duration in ticks of tickInterval.
        public void ApplyOverTime(float totalAmount, float duration, float tickInterval, bool isHealing, DamageType type, GameObject source = null)
        {
            if (duration <= 0f || tickInterval <= 0f || totalAmount == 0f) return;

            int ticks = Mathf.Max(1, Mathf.RoundToInt(duration / tickInterval));
            float perTick = totalAmount / ticks;

            _effects.Add(new ActiveEffect
            {
                isHealing = isHealing,
                type = type,
                remaining = duration,
                tickInterval = tickInterval,
                tickTimer = tickInterval,
                amountPerTick = perTick,
                source = source
            });
        }

        private void RaiseHealthChanged()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private ClassSystem.Runtime.CharacterStats ResolveStatsFromSource(GameObject source)
        {
            if (source == null) return null;
            return source.GetComponentInParent<ClassSystem.Runtime.CharacterStats>();
        }

        private void HandleDeath()
        {
            if (_deathHandled) return;
            _deathHandled = true;

            if (_lastDamager != null && xpOnDeath > 0)
            {
                _lastDamager.AddExperience(xpOnDeath);
            }

            OnDeath?.Invoke();

            var pv = GetComponent<PhotonView>();
            bool isNetworked = pv != null && PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

            if (!isNetworked)
            {
                if (pv != null && pv.IsMine)
                {
                    PhotonNetwork.Destroy(pv);
                }
                else
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (PhotonNetwork.IsMasterClient || (pv != null && pv.IsMine))
            {
                PhotonNetwork.Destroy(pv);
            }
            else if (pv != null)
            {
                // Ask the master to destroy this view and buffer so late-joiners also remove it.
                pv.RPC(nameof(RPC_RequestDestroy), RpcTarget.MasterClient);
                pv.RPC(nameof(RPC_ClientDestroy), RpcTarget.AllBuffered);
            }
        }

        [PunRPC]
        void RPC_RequestDestroy()
        {
            var pv = GetComponent<PhotonView>();
            if (pv != null && PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(pv);
            }
        }

        [PunRPC]
        void RPC_ClientDestroy()
        {
            if (_deathHandled) return;
            _deathHandled = true;
            Destroy(gameObject);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(currentHealth);
            }
            else if (stream.IsReading)
            {
                currentHealth = (float)stream.ReceiveNext();
            }
        }
    }
}
