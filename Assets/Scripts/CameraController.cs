using DungeonGenerator.Character;
using Networking;
using Photon.Pun;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerSpawnController spawnController;
    [SerializeField] private Transform manualTarget;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 0f, -10f);
    [SerializeField, Tooltip("Lower values follow more tightly; higher values lag more.")]
    private float followSmoothTime = 0.15f;
    [SerializeField, Tooltip("Maximum distance the camera can shift toward the mouse cursor.")]
    private float maxMouseOffset = 3f;
    [SerializeField, Tooltip("Responsiveness of cursor-driven offset smoothing.")]
    private float mouseOffsetResponsiveness = 10f;

    private Transform followTarget;
    private Camera cachedCamera;
    private Vector3 followVelocity;
    private Vector3 currentMouseOffset;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        if (spawnController == null)
        {
            spawnController = FindAnyObjectByType<PlayerSpawnController>();
        }

        if (spawnController != null)
        {
            spawnController.PlayerSpawned += OnPlayerSpawned;
            if (spawnController.SpawnedPlayers.Count > 0)
            {
                GameObject firstPlayer = spawnController.SpawnedPlayers[0];
                if (firstPlayer != null)
                {
                    SetFollowTarget(firstPlayer.transform);
                }
            }
        }

        // Also listen for network-spawned local player (multiplayer flow)
        if (MultiplayerGameManager.Instance != null)
        {
            MultiplayerGameManager.Instance.LocalPlayerSpawned += OnPlayerSpawned;
        }

        // Try to immediately target an already-spawned local network player
        TrySetLocalNetworkPlayerAsTarget();

        if (manualTarget != null)
        {
            SetFollowTarget(manualTarget);
        }

        if (cachedCamera == null)
        {
            cachedCamera = GetComponent<Camera>();
        }

        if (cachedCamera == null)
        {
            cachedCamera = Camera.main;
        }
    }

    private void OnDisable()
    {
        if (spawnController != null)
        {
            spawnController.PlayerSpawned -= OnPlayerSpawned;
        }

        if (MultiplayerGameManager.Instance != null)
        {
            MultiplayerGameManager.Instance.LocalPlayerSpawned -= OnPlayerSpawned;
        }
    }

    private void OnPlayerSpawned(GameObject player)
    {
        if (player != null)
        {
            SetFollowTarget(player.transform);
        }
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        // Snap immediately to the target when a new follow target is assigned.
        if (followTarget != null)
        {
            Vector3 desiredPosition = followTarget.position + cameraOffset;
            transform.position = desiredPosition;
            currentMouseOffset = Vector3.zero;
            followVelocity = Vector3.zero;
        }
    }

    private void TrySetLocalNetworkPlayerAsTarget()
    {
        // Find a locally-owned network player and follow it.
        NetworkPlayerController[] players = FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None);
        foreach (var npc in players)
        {
            if (npc != null && npc.photonView != null && npc.photonView.IsMine)
            {
                SetFollowTarget(npc.transform);
                return;
            }
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            return;
        }

        Vector3 basePosition = followTarget.position + cameraOffset;
        Vector3 desiredMouseOffset = CalculateMouseOffset();
        float lerpFactor = 1f - Mathf.Exp(-mouseOffsetResponsiveness * Time.deltaTime);
        currentMouseOffset = Vector3.Lerp(currentMouseOffset, desiredMouseOffset, lerpFactor);

        Vector3 desiredPosition = basePosition + currentMouseOffset;
        float smoothTime = Mathf.Max(0.001f, followSmoothTime);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, smoothTime);
    }

    private Vector3 CalculateMouseOffset()
    {
        if (cachedCamera == null || maxMouseOffset <= 0f)
        {
            return Vector3.zero;
        }

        Vector3 mouseScreen = Input.mousePosition;
        Vector3 cameraToTarget = followTarget.position - cachedCamera.transform.position;
        float planeDistance = Vector3.Dot(cameraToTarget, cachedCamera.transform.forward);

        if (planeDistance <= 0f)
        {
            planeDistance = Mathf.Abs(cameraOffset.z);
        }

        Vector3 mouseWorld = cachedCamera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, planeDistance));
        Vector3 offset = mouseWorld - followTarget.position;
        offset.z = 0f;

        float magnitude = offset.magnitude;
        if (magnitude > maxMouseOffset)
        {
            offset = offset.normalized * maxMouseOffset;
        }

        return offset;
    }
}
