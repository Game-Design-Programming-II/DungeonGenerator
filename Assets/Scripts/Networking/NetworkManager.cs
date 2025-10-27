using UnityEngine;
using Photon.Pun;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            PhotonView view = GetComponent<PhotonView>();
            if (view != null && view.ViewID == 0)
            {
                view.ViewID = 5;
            }
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        //connect to the server
        PhotonNetwork.ConnectUsingSettings();
    }

    /*public override void OnConnectedToMaster()
    {
        //base.OnConnectedToMaster();
        Debug.Log("Connected to master server");
    }*/

    public void CreateRoom(string roomName)
    {
        PhotonNetwork.CreateRoom(roomName);
    }

    /*public override void OnCreatedRoom()
    {
        //base.OnCreatedRoom();
        Debug.Log("Created room: " + PhotonNetwork.CurrentRoom.Name);
    }*/

    public void JoinRoom(string roomName)
    {
        PhotonNetwork.JoinRoom(roomName);
    }

    [PunRPC]
    public void ChangeScene(string sceneName)
    {
        PhotonNetwork.LoadLevel(sceneName);
    }
}
