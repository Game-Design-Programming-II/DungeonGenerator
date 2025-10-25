using DungeonGenerator.Character;
using Photon.Pun;
using UnityEngine;

namespace Networking
{
    /// <summary>
    /// Bridges Photon ownership with the local PlayerCharacterController.
    /// Disables input on remote avatars and streams basic transform data so
    /// other clients see smooth movement.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PlayerCharacterController))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class NetworkPlayerController : MonoBehaviourPun, IPunObservable
    {
        [Header("References")]
        [SerializeField] private PlayerCharacterController characterController;
        [SerializeField] private Rigidbody2D body;

        [Header("Remote Smoothing")]
        [SerializeField, Tooltip("How quickly remote avatars interpolate toward their networked position.")]
        private float remoteLerpSpeed = 12f;

        private Vector3 networkPosition;
        private Vector2 networkVelocity;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<PlayerCharacterController>();
            }

            if (body == null)
            {
                body = GetComponent<Rigidbody2D>();
            }
        }

        private void OnEnable()
        {
            bool isMine = photonView.IsMine;

            if (characterController != null)
            {
                characterController.enabled = isMine;
            }

            if (body != null)
            {
                // body.isKinematic = !isMine;
                body.bodyType = isMine ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            }

            MultiplayerGameManager.Instance?.RegisterPlayerInstance(photonView.OwnerActorNr, gameObject);
        }

        private void OnDisable()
        {
            MultiplayerGameManager.Instance?.UnregisterPlayerInstance(photonView.OwnerActorNr, gameObject);
        }

        private void Update()
        {
            if (photonView.IsMine || body == null)
            {
                return;
            }

            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * remoteLerpSpeed);
        }

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(body.linearVelocity);
            }
            else
            {
                networkPosition = (Vector3)stream.ReceiveNext();
                networkVelocity = (Vector2)stream.ReceiveNext();
            }
        }
    }
}
