using ClassSystem.Classes;
using ClassSystem.Items;
using ClassSystem.Runtime;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Applies the selected class (from Photon custom properties) to the spawned player.
    /// Ensures class-system runtime components exist and equips default loadout.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    [DisallowMultipleComponent]
    public class NetworkPlayerLoadout : MonoBehaviourPunCallbacks
    {
        [Tooltip("Ordered list of classes matching the lobby selection indices.")]
        [SerializeField] private PlayerClass[] classRegistry;

        [Header("Optional Defaults")]
        [SerializeField] private Weapon[] defaultWeaponsByClass;
        [SerializeField] private Armor[] defaultArmorsByClass;

        private CharacterStats stats;
        private EquipmentManager equip;
        private PhotonView view;

        private void Awake()
        {
            view = GetComponent<PhotonView>();
            stats = GetComponent<CharacterStats>();
            if (stats == null) stats = gameObject.AddComponent<CharacterStats>();

            equip = GetComponent<EquipmentManager>();
            if (equip == null) equip = gameObject.AddComponent<EquipmentManager>();

            if (GetComponent<Attacker>() == null) gameObject.AddComponent<Attacker>();
            if (GetComponent<SpellCaster>() == null) gameObject.AddComponent<SpellCaster>();
        }

        private void Start()
        {
            ApplyLoadoutForOwner();
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
        {
            if (targetPlayer != null && view != null && targetPlayer == view.Owner)
            {
                ApplyLoadoutForOwner();
            }
        }

        private void ApplyLoadoutForOwner()
        {
            if (view == null || view.Owner == null)
            {
                Debug.LogWarning("[NetworkPlayerLoadout] Missing PhotonView or owner.");
                return;
            }

            int classIndex = ResolveClassIndex(view.Owner);
            if (classIndex < 0 || classRegistry == null || classIndex >= classRegistry.Length)
            {
                Debug.LogWarning($"[NetworkPlayerLoadout] No valid class for actor {view.Owner.ActorNumber}.");
                return;
            }

            PlayerClass pc = classRegistry[classIndex];
            stats.AssignClass(pc, fullRestore: true);

            // Equip defaults if provided
            if (defaultWeaponsByClass != null && classIndex < defaultWeaponsByClass.Length)
            {
                var w = defaultWeaponsByClass[classIndex];
                if (w != null)
                {
                    equip.TryEquipWeapon(w, pc, out _);
                }
            }

            if (defaultArmorsByClass != null && classIndex < defaultArmorsByClass.Length)
            {
                var a = defaultArmorsByClass[classIndex];
                if (a != null)
                {
                    equip.EquipArmor(a);
                }
            }

            Debug.Log($"[NetworkPlayerLoadout] Applied class '{pc.displayName}' to actor {view.Owner.ActorNumber}.");
        }

        private int ResolveClassIndex(Player player)
        {
            if (player == null || player.CustomProperties == null) return -1;
            if (player.CustomProperties.TryGetValue("classId", out object value) && value is int idx)
            {
                return idx;
            }
            return -1;
        }
    }
}
