using Networking;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using Unity.VisualScripting;

[RequireComponent(typeof(PhotonView))]
public class MenuUI : MonoBehaviourPunCallbacks
{
    [Header("Screens")]
    public GameObject mainScreen;
    public GameObject lobbyScreen;

    [Header("Main Screen")]
    public Button createRoomButton;
    public Button joinRoomButton;

    [Header("Lobby Screen")]
    public TextMeshProUGUI playerListText;
    public Button startGameButton;
    
    [Header("Scene References")]
    public string sceneName;

    private PhotonView cachedView;

    void Start()
    {
        cachedView = GetComponent<PhotonView>();
        if (cachedView == null)
        {
            cachedView = gameObject.AddComponent<PhotonView>();
        }
        if (cachedView.ViewID == 0)
        {
            cachedView.ViewID = 1002;
        }

        createRoomButton.interactable = false;
        joinRoomButton.interactable = false;
    }

    public override void OnConnectedToMaster()
    {
        //base.OnConnectedToMaster();
        createRoomButton.interactable = true;
        joinRoomButton.interactable = true;
    }

    private void SetScreen(GameObject screen)
    {
        mainScreen.SetActive(false);
        lobbyScreen.SetActive(false);
        screen.SetActive(true);
    }

    public void OnCreateRoomButton(TMP_InputField roomNameInput)
    {
        NetworkManager.instance.CreateRoom(roomNameInput.text);
    }

    public void OnJoinRoomButton(TMP_InputField roomNameInput)
    {
        NetworkManager.instance.JoinRoom(roomNameInput.text);
    }

    public void OnPlayerNameUpdate(TMP_InputField playerNameInput)
    {
        PhotonNetwork.NickName = playerNameInput.text;
    }

    public void UpdateLobbyUI()
    {
        playerListText.text = "";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerListText.text += $"{player.ActorNumber} : {player.NickName}\n";
        }

        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.interactable = true;
        }
        else
        {
            startGameButton.interactable = false;
        }
    }

    public override void OnJoinedRoom()
    {
        //base.OnJoinedRoom();
        SetScreen(lobbyScreen);
        UpdateLobbyUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Refresh lobby list when someone joins
        UpdateLobbyUI();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //base.OnPlayerLeftRoom(otherPlayer);
        UpdateLobbyUI();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        // Update start button interactable state
        UpdateLobbyUI();
    }

    public void OnLeaveLobbyButton()
    {
        PhotonNetwork.LeaveRoom();
        SetScreen(mainScreen);
    }

    public void StartGameButton()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[MenuUI] Scene name not assigned for StartGameButton.");
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogWarning("[MenuUI] StartGame pressed by non-master; ignoring.");
            return;
        }

        // Create a deterministic seed for this match
        int seed = (int)(System.DateTime.UtcNow.Ticks & 0x7FFFFFFF);

        // Master loads the scene; AutomaticallySyncScene will move all clients.
        PhotonNetwork.LoadLevel(sceneName);

        // Signal match start to all clients (buffered for late-joiners) with the shared seed.
        PhotonView gameManagerView = MultiplayerGameManager.Instance?.photonView;
        if (gameManagerView != null)
        {
            gameManagerView.RPC("BeginMatch", RpcTarget.AllBuffered, seed);
        }
        else
        {
            Debug.LogError("[MenuUI] MultiplayerGameManager PhotonView not found; BeginMatch not sent.");
        }
    }
}
