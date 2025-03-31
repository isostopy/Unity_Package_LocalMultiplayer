using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ServerApp : MonoBehaviour
{
    public static ServerApp Instance { get; private set; }

    private UdpClient udpClient;
    private IPEndPoint broadcastEndPoint;
    private AndroidJavaObject multicastLock;

    private string localIP;
    private string broadcastIP;

    Camera targetCamera;
    public float lerpSpeed = 5f;
    private Vector3 cameraTargetPosition;
    private Quaternion cameraTargetRotation;

    [SerializeField] RefsBridge refsBridge;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this object when changing scenes
            InitializeServer();
            InvokeRepeating(nameof(DiscoverDevices), 5f, 5f);
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }

        targetCamera = FindFirstObjectByType<Camera>();
        cameraTargetPosition = targetCamera.transform.position;
        cameraTargetRotation = targetCamera.transform.rotation;
    }

    #region Connection Management

    private void InitializeServer()
    {
        try
        {
            udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, 50000));
            Debug.Log("Port 50000 is accessible.");
        }
        catch
        {
            Debug.Log("Port 50000 is not accessible.");
        }

        udpClient.EnableBroadcast = true;

        localIP = GetLocalIPAddress();
        broadcastIP = localIP.Substring(0, localIP.LastIndexOf('.')) + ".255";
        broadcastEndPoint = new IPEndPoint(IPAddress.Parse(broadcastIP), 50000);

        EnableMulticastLock();

        ListenForMessages();
    }

    void DiscoverDevices()
    {
        if (udpClient == null) return;
        SendMessageMulticast("DiscoverCLients");
    }

    public void SelectClient(string clientIP)
    {

    }

    private string GetLocalIPAddress()
    {
        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork && !ip.Address.ToString().StartsWith("127."))
                    {
                        return ip.Address.ToString();
                    }
                }
            }
        }
        return "192.168.43.255";
    }

    private void EnableMulticastLock()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                using (AndroidJavaObject wifiManager = activity.Call<AndroidJavaObject>("getSystemService", "wifi"))
                {
                    multicastLock = wifiManager.Call<AndroidJavaObject>("createMulticastLock", "mylock");
                    multicastLock.Call("setReferenceCounted", true);
                    multicastLock.Call("acquire");
                    Debug.Log("Multicast lock acquired!");
                }
            }
        }
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

                ProcessMessage(receivedMessage);
            }
            catch (SocketException ex)
            {
                Debug.LogError($"UDP Receive Error: {ex.Message}");
                break;
            }
        }
    }

    public void SendMessageMulticast(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        try
        {
            udpClient.Send(data, data.Length, broadcastEndPoint);
            Debug.Log($"Broadcast Message sent: {message} to {broadcastIP}");
        }
        catch (SocketException ex)
        {
            Debug.LogError("Broadcast failed: " + ex.Message);
        }
    }

    void ProcessMessage(string message)
    {
        string[] parts = message.Split('|');

        if (parts.Length == 3 && parts[0] == "Transform")
        {
            string[] posData = parts[1].Split(';');
            string[] rotData = parts[2].Split(';');

            if (posData.Length == 3 && rotData.Length == 4)
            {
                Vector3 position = new Vector3(
                    float.Parse(posData[0]),
                    float.Parse(posData[1]),
                    float.Parse(posData[2])
                );

                Quaternion rotation = new Quaternion(
                    float.Parse(rotData[0]),
                    float.Parse(rotData[1]),
                    float.Parse(rotData[2]),
                    float.Parse(rotData[3])
                );

                ApplyTransform(position, rotation);
            }
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

    }

    #endregion


    private void ApplyTransform(Vector3 position, Quaternion rotation)
    {
        if (targetCamera != null)
        {
            cameraTargetPosition = position;
            cameraTargetRotation = rotation;
        }
        else
        {
            Debug.LogWarning("No target object assigned in the inspector!");
            targetCamera = FindFirstObjectByType<Camera>();
        }
    }

    void LateUpdate()
    {
        targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, cameraTargetPosition, Time.deltaTime * lerpSpeed);
        targetCamera.transform.rotation = Quaternion.Slerp(targetCamera.transform.rotation, cameraTargetRotation, Time.deltaTime * lerpSpeed);  
    }

}
