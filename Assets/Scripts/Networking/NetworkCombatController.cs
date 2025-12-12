using System.Collections.Generic;
using Character;
using ClassSystem.Combat;
using ClassSystem.Runtime;
using ClassSystem.Spells;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Networking
{
    /// <summary>
    /// Handles local combat input and forwards melee hits to the master client for authority.
    /// Left click ("Attack" action) performs a cone melee toward the mouse.
    /// spell casting can be wired to the "spacebar action" input.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Attacker))]
    public class NetworkCombatController : MonoBehaviourPun
    {
        [Header("Melee")]
        [SerializeField] private float meleeRange = 2.5f;
        [SerializeField, Range(0f, 180f)] private float meleeHalfAngleDegrees = 75f;
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField, Tooltip("Optional origin for melee checks; defaults to this transform.")]
        private Transform attackOrigin;
        [SerializeField, Tooltip("Log debug info for melee/spell events. Turn off in production.")]
        private bool debugLogs = false;

        [Header("Spells")]
        [SerializeField] private Spell equippedSpell;
        [SerializeField] private Transform spellMuzzle;

        private readonly Collider2D[] _meleeBuffer = new Collider2D[32];
        private readonly HashSet<IDamageable> _dedupDamageables = new HashSet<IDamageable>();
        private readonly HashSet<CharacterHealth> _dedupHealth = new HashSet<CharacterHealth>();

        private PlayerInput _playerInput;
        private InputAction _attackAction;
        private InputAction _spellAction;
        private Attacker _attacker;
        private SpellCaster _spellCaster;

        void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _attacker = GetComponent<Attacker>();
            _spellCaster = GetComponent<SpellCaster>();
        }

        void OnEnable()
        {
            _attackAction = _playerInput.actions?.FindAction("Attack", throwIfNotFound: false);
            _spellAction = _playerInput.actions?.FindAction("spacebar action", throwIfNotFound: false);

            if (_attackAction != null)
                _attackAction.performed += OnAttackPerformed;

            if (_spellAction != null)
                _spellAction.performed += OnSpellPerformed;
        }

        void OnDisable()
        {
            if (_attackAction != null)
                _attackAction.performed -= OnAttackPerformed;

            if (_spellAction != null)
                _spellAction.performed -= OnSpellPerformed;
        }

        void OnAttackPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            HandleMelee();
        }

        void OnSpellPerformed(InputAction.CallbackContext ctx)
        {
            if (!ctx.performed) return;
            HandleSpell();
        }

        void HandleMelee()
        {
            if (!HasInputAuthority()) return;
            if (_attacker == null || !_attacker.CanAttack()) return;

            Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
            Vector2 aimDir = GetAimDirection(origin);
            float cosHalfAngle = Mathf.Cos(meleeHalfAngleDegrees * Mathf.Deg2Rad);

            int hitCount = Physics2D.OverlapCircleNonAlloc(origin, meleeRange, _meleeBuffer, targetLayers);
            var viewIds = new List<int>(hitCount);
            var directDamageables = new List<IDamageable>();
            var directHealth = new List<CharacterHealth>();
            var viewDamageables = new List<IDamageable>();
            var viewHealth = new List<CharacterHealth>();
            _dedupDamageables.Clear();
            _dedupHealth.Clear();

            if (debugLogs)
            {
                Debug.Log($"[NetworkCombatController] Melee attempt. Hits in radius: {hitCount}, aimDir: {aimDir}, origin: {origin}", this);
            }

            for (int i = 0; i < hitCount; i++)
            {
                var col = _meleeBuffer[i];
                if (col == null) continue;

                Transform targetTransform = col.transform;
                if (targetTransform == null) continue;
                if (targetTransform == transform || targetTransform.IsChildOf(transform)) continue;

                Vector2 toTarget = (Vector2)targetTransform.position - (Vector2)origin;
                if (toTarget.sqrMagnitude < 0.0001f) continue;
                Vector2 toTargetDir = toTarget.normalized;
                if (Vector2.Dot(aimDir, toTargetDir) < cosHalfAngle) continue;

                var targetView = col.GetComponentInParent<PhotonView>();
                var dmg = col.GetComponentInParent<IDamageable>();
                var health = col.GetComponentInParent<CharacterHealth>();
                if (debugLogs)
                {
                    float dot = Vector2.Dot(aimDir, toTargetDir);
                    string layerName = LayerMask.LayerToName(col.gameObject.layer);
                    Debug.Log($"[NetworkCombatController]   candidate '{col.name}' layer '{layerName}' view:{(targetView ? targetView.ViewID.ToString() : "none")} dmg:{(dmg!=null)} health:{(health!=null)} dot:{dot:0.00}", this);
                }
                if (targetView != null)
                {
                    if (!viewIds.Contains(targetView.ViewID))
                        viewIds.Add(targetView.ViewID);
                }
                else
                {
                    if (dmg != null && _dedupDamageables.Add(dmg))
                    {
                        directDamageables.Add(dmg);
                    }
                    else if (health != null && _dedupHealth.Add(health))
                    {
                        directHealth.Add(health);
                    }
                }
            }

            bool isNetworked = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
            if (isNetworked)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    ApplyMeleeToViews(viewIds, viewDamageables, viewHealth);
                    if (_attacker != null)
                    {
                        // Merge all targets and resolve once so cooldown is applied a single time.
                        var mergedDamageables = new List<IDamageable>(viewDamageables.Count + directDamageables.Count);
                        mergedDamageables.AddRange(viewDamageables);
                        mergedDamageables.AddRange(directDamageables);
                        var mergedHealth = new List<CharacterHealth>(viewHealth.Count + directHealth.Count);
                        mergedHealth.AddRange(viewHealth);
                        mergedHealth.AddRange(directHealth);
                        _attacker.TryAttackTargets(mergedDamageables, mergedHealth);
                    }
                    if (debugLogs) Debug.Log($"[NetworkCombatController] Master applied melee locally. Views:{viewIds.Count} DirectIDmg:{directDamageables.Count} DirectHealth:{directHealth.Count}", this);
                }
                else
                {
                    if (viewIds.Count > 0)
                        photonView.RPC(nameof(RPC_ApplyMelee), RpcTarget.MasterClient, viewIds.ToArray());

                    // Non-networked targets (if any) are handled locally by the owner.
                    bool appliedLocal = false;
                    if (directDamageables.Count > 0 || directHealth.Count > 0)
                    {
                        ApplyMeleeDirect(directDamageables, directHealth);
                        appliedLocal = true;
                    }

                    // If we only swung at networked targets, still consume cooldown locally.
                    if (!appliedLocal && viewIds.Count > 0 && _attacker != null)
                    {
                        _attacker.ConsumeCooldown();
                    }

                    if (debugLogs)
                    {
                        Debug.Log($"[NetworkCombatController] Sent melee RPC to master. ViewIds:{viewIds.Count} direct local:{directDamageables.Count + directHealth.Count}", this);
                    }
                }
            }
            else
            {
                ApplyMeleeDirect(directDamageables, directHealth);
                if (debugLogs) Debug.Log($"[NetworkCombatController] Offline melee applied locally. Direct total:{directDamageables.Count + directHealth.Count}", this);
            }
        }

        void HandleSpell()
        {
            // Local only; network replication for spells can be added when ready.
            if (!HasInputAuthority()) return;
            if (_spellCaster == null || equippedSpell == null) return;

            if (spellMuzzle != null)
            {
                _spellCaster.muzzle = spellMuzzle;
            }

            Vector3 origin = spellMuzzle != null ? spellMuzzle.position : transform.position;
            Vector2 aimDir = GetAimDirection(origin);
            _spellCaster.Cast(equippedSpell, aimDir);
        }

        [PunRPC]
        void RPC_ApplyMelee(int[] targetViewIds, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            _dedupDamageables.Clear();
            _dedupHealth.Clear();
            var dmg = new List<IDamageable>();
            var health = new List<CharacterHealth>();
            ApplyMeleeToViews(targetViewIds, dmg, health);
            if (_attacker != null)
            {
                _attacker.TryAttackTargets(dmg, health);
            }
            if (debugLogs) Debug.Log($"[NetworkCombatController] Master RPC_ApplyMelee from actor {info.Sender?.ActorNumber}, targets:{targetViewIds?.Length ?? 0}", this);
        }

        void ApplyMeleeToViews(IReadOnlyList<int> targetViewIds, List<IDamageable> damageables, List<CharacterHealth> healthTargets)
        {
            if (targetViewIds == null) return;
            for (int i = 0; i < targetViewIds.Count; i++)
            {
                var pv = PhotonView.Find(targetViewIds[i]);
                if (pv == null) continue;
                var dmg = pv.GetComponentInParent<IDamageable>();
                if (dmg != null && _dedupDamageables.Add(dmg))
                {
                    damageables.Add(dmg);
                }
                else
                {
                    var ch = pv.GetComponentInParent<CharacterHealth>();
                    if (ch != null && _dedupHealth.Add(ch))
                    {
                        healthTargets.Add(ch);
                    }
                }
            }
        }

        void ApplyMeleeDirect(IReadOnlyList<IDamageable> damageables, IReadOnlyList<CharacterHealth> healthTargets)
        {
            if (_attacker == null) return;
            _attacker.TryAttackTargets(damageables, healthTargets);
        }

        Vector2 GetAimDirection(Vector3 origin)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector3)Input.mousePosition;
                Vector3 world = cam.ScreenToWorldPoint(mouseScreen);
                world.z = 0f;
                Vector2 dir = (Vector2)(world - origin);
                if (dir.sqrMagnitude > 0.0001f)
                {
                    return dir.normalized;
                }
            }

            return Vector2.right; // default forward
        }

        bool HasInputAuthority()
        {
            if (!PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode) return true;
            return photonView.IsMine;
        }
    }
}
