using System;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

public enum PlayType
{
    ClientAndServer = 0,
    Client = 1,
    Server = 2,
    None = 3
}

public class ConnectionManager : MonoBehaviour
{
    public InputField nameText;
    public InputField ipText;
    public InputField portText;
    public Dropdown clientServerPicker;
    public InputField numThinClientsText;

    private new static string name = "";

    public static string Name
    {
        get
        {
            if (name == "")
                return "Onur";
            else
                return name;
        }
    }
    
    private static string ipAddress = "";

    public static string IPAddress
    {
        get
        {
            if (ipAddress != "")
                return ipAddress;
#if UNITY_EDITOR
            if (NetcodeBootstrap.RequestedAutoConnect != "")
                return NetcodeBootstrap.RequestedAutoConnect;
#endif
            return "127.0.0.1";
        }
    }

    public static ushort port = 7979;
    private static PlayType playType = PlayType.None;

    public static PlayType RequestedPlayType
    {
        get
        {
            if (playType != PlayType.None)
                return playType;

            return (PlayType)ClientServerBootstrap.RequestedPlayType;
        }
    }
    
    private static int numThinClients = -1;

    public static int NumThinClients
    {
        get
        {
            if (numThinClients != -1)
                return numThinClients;
#if UNITY_EDITOR
            return ClientServerBootstrap.RequestedNumThinClients;
#else
            return 0;
#endif
        }
    }

    private void Update()
    {
        name = nameText.text;
        if (name == "")
            name = "Onur";
        ipAddress = ipText.text;
        port = Convert.ToUInt16(portText.text);
        switch (clientServerPicker.value)
        {
            case 0:
                playType = PlayType.ClientAndServer;
                break;
            case 1:
                playType = PlayType.Client;
                break;
            case 2:
                playType = PlayType.Server;
                break;
        }

        numThinClients = Convert.ToInt32(numThinClientsText.text);
    }
}