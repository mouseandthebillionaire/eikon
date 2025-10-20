using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class RNBOController : MonoBehaviour
{
    private UdpClient udpClient;
    private string ipAddress = "127.0.0.1"; // localhost
    private int port = 7400; // Match this with your RNBO patch's listening port

    void Start()
    {
        udpClient = new UdpClient();
    }

    public void SendToRNBO(string parameter, float value)
    {
        try
        {
            // Format the message as: /parameter value
            string message = $"/{parameter} {value}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, ipAddress, port);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error sending OSC message: {e.Message}");
        }
    }

    // Example usage:
    public void UpdateShimmerSpeed(float speed)
    {
        SendToRNBO("shimmerSpeed", speed);
    }

    public void UpdateShimmerBreadth(float breadth)
    {
        SendToRNBO("shimmerBredth", breadth);
    }

    void OnDestroy()
    {
        if (udpClient != null)
            udpClient.Close();
    }
} 