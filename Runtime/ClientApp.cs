using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientApp : MonoBehaviour
{
    public static ClientApp Instance { get; private set; }

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    [SerializeField] RefsBridge refsBridge;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeClient();
            InvokeRepeating(nameof(SendCameraTransform), 0.05f, 0.05f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region Connection Management

    void InitializeClient()
    {
        try
        {
            udpClient = new UdpClient(50000);
            Debug.Log("Port 50000 is accessible.");
        }
        catch
        {
            Debug.Log("Port 50000 is not accessible.");
        }

        ListenForMessages();
    }


    #endregion


    #region Message Management

    async void ListenForMessages()
    {
        while (true)
        {
            try
            {
                UdpReceiveResult result = await udpClient.ReceiveAsync();
                string receivedMessage = Encoding.UTF8.GetString(result.Buffer);
                Debug.Log($"Server Received: {receivedMessage} from {result.RemoteEndPoint}");

                serverEndPoint = result.RemoteEndPoint;
                ProcessMessage(receivedMessage);
            }
            catch (SocketException ex)
            {
                Debug.LogError($"UDP Receive Error: {ex.Message}");
                break;
            }
        }
    }

    private void ProcessMessage(string message)
    {
        string[] parts = message.Split('|');

        if (parts.Length >= 1 && parts[0] == "DiscoverClients")
        {
            Debug.Log("Ping received");
            return;
        }

        if (parts.Length >= 3 && parts[0] == "ChangeMaterials") // Example: "ChangeMaterials|GroupID|MaterialID"
        {
            string groupID = parts[1];
            string materialID = parts[2];
            refsBridge.ChangeMaterials(groupID, materialID);
            return;
        }

        if (parts.Length >= 2 && parts[0] == "ChangeLevel") // Example: "ChangeLevel|LevelName"
        {
            string levelName = parts[1];
            SceneManager.LoadScene(levelName);
            return;
        }

        Debug.LogWarning("Invalid message format received.");

    }

    #endregion

    void SendCameraTransform()
    {
        if (Camera.main == null || serverEndPoint == null || udpClient == null) return;

        Vector3 pos = Camera.main.transform.position;
        Quaternion rot = Camera.main.transform.rotation;

        string message = $"Transform|{pos.x};{pos.y};{pos.z}|{rot.x};{rot.y};{rot.z};{rot.w}";
        byte[] data = Encoding.UTF8.GetBytes(message);

        udpClient.Send(data, data.Length, serverEndPoint);
        Debug.Log("Sending Camera " + message);
    }

}
