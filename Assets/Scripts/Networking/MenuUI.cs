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

    [PunRPC]
    public void UpdateLobbyUI()
    {
        playerListText.text = "";
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            playerListText.text += player.NickName + "\n";
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
        if (cachedView != null)
        {
            cachedView.RPC("UpdateLobbyUI", RpcTarget.All);
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        //base.OnPlayerLeftRoom(otherPlayer);
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

        PhotonView networkView = NetworkManager.instance?.photonView;
        PhotonView gameManagerView = MultiplayerGameManager.Instance?.photonView;

        if (networkView == null || gameManagerView == null)
        {
            if (networkView == null) Debug.LogError("[MenuUI] NetworkManager PhotonView not found.");
            if (gameManagerView == null) Debug.LogError("[MenuUI] MultiplayerGameManager PhotonView not found.");
            Debug.LogError("[MenuUI] Unable to locate NetworkManager or MultiplayerGameManager PhotonViews; aborting StartGame.");
            return;
        }

        networkView.RPC("ChangeScene", RpcTarget.All, sceneName);
        gameManagerView.RPC("BeginMatch", RpcTarget.AllBuffered);
    }
}
