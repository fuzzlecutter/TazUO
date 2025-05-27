using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Utility;
using ClassicUO.Utility.Logging;
using Discord.Sdk;
using Microsoft.Xna.Framework;
using DClient = Discord.Sdk.Client;

namespace ClassicUO.Game.Managers;

public class DiscordManager
{
    public static DiscordManager Instance { get; } = new DiscordManager();

    private const string TUOLOBBY = "TazUODiscordSocialSDKLobby";
    private static Dictionary<string, string> TUOMETA = new Dictionary<string, string>()
    {
        { "name", "TazUO - Global" }
    };
    private const ulong clientId = 1255990139499577377;
    //Commited on purpose, this is a public discord bot that for some reason the social sdk requires, even though it connects to the player's account :thinking:

    private const int MAX_MSG_HISTORY = 75;
    public string StatusText => statusText;

    public bool Connected => connected;
    public Dictionary<ulong, List<MessageHandle>> MessageHistory => messageHistory;
    public ulong UserID { get; private set; }

    public event EmptyEventHandler OnConnected;
    public event EmptyEventHandler OnStatusTextUpdated;

    /// <summary>
    /// object is MessageHandle
    /// </summary>
    public event SimpleEventHandler OnMessageReceived;

    public event EmptyEventHandler OnUserUpdated;

    /// <summary>
    /// object is LobbyHandle, may be null
    /// </summary>
    public event SimpleEventHandler OnLobbyCreated;


    private DClient client;
    private string codeVerifier;
    private bool authBegan, connected;
    private string statusText = "Ready to connect...";

    private Dictionary<ulong, List<MessageHandle>> messageHistory = new();
    private Dictionary<ulong, Color> userHueMemory = new();

    private DiscordManager()
    {
        client = new DClient();

        client.AddLogCallback(OnLog, LoggingSeverity.Error);
        client.SetStatusChangedCallback(OnStatusChanged);
        client.SetMessageCreatedCallback(OnMessageCreated);
        client.SetUserUpdatedCallback(OnUserUpdatedCallback);
        client.SetLobbyCreatedCallback(OnLobbyCreatedCallback);
        EventSink.OnConnected += OnUOConnected;
    }

    public void Update()
    {
        if (authBegan)
        {
            Discord.Sdk.NativeMethods.Discord_RunCallbacks();
        }
    }

    public IEnumerable<LobbyHandle> GetLobbies()
    {
        var lobbies = client.GetLobbyIds();

        List<LobbyHandle> handles = new();

        foreach (var lobby in lobbies)
        {
            var h = client.GetLobbyHandle(lobby);

            if (h != null)
                handles.Add(h);
        }

        return handles;
    }

    public string GetLobbyName(LobbyHandle handle)
    {
        var meta = handle.Metadata();

        if (meta.ContainsKey("name"))
        {
            return meta["name"];
        }

        return "Lobby";
    }

    public RelationshipHandle[] GetFriends()
    {
        if (!connected)
            return null;

        return client.GetRelationships();
    }

    public ChannelHandle GetChannel(ulong channelId) => client.GetChannelHandle(channelId);
    public LobbyHandle GetLobby(ulong lobbyId) => client.GetLobbyHandle(lobbyId);

    public UserHandle GetUser(ulong userId) => client.GetUser(userId);

    public void SendDM(ulong id, string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        client.SendUserMessage(id, message, SendUserMessageCallback);
    }

    public void SendChannelMsg(ulong channelId, string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        client.SendLobbyMessage(channelId, message, SendUserMessageCallback);
    }

    public Color GetUserhue(ulong id)
    {
        if (!userHueMemory.ContainsKey(id))
            userHueMemory.Add(id, GetColorFromId(id));

        return userHueMemory[id];
    }

    private static Color GetColorFromId(ulong id)
    {
        // Convert ulong to bytes
        byte[] bytes = BitConverter.GetBytes(id);

        // Mix the bytes to get more color variation
        byte r = (byte)(bytes[0] ^ bytes[4]);
        byte g = (byte)(bytes[1] ^ bytes[5]);
        byte b = (byte)(bytes[2] ^ bytes[6]);

        return new Color(r, g, b);
    }

    private static ushort GetHueFromId(ulong id)
    {
        // Fold the 64-bit ID into 32 bits for some mixing
        uint folded = (uint)(id ^ (id >> 32));

        // Further mix the bits
        folded ^= (folded >> 16);
        folded ^= (folded >> 8);

        // Return value in range 0–999
        return (ushort)(folded % 1000);
    }

    private void SendUserMessageCallback(ClientResult result, ulong messageId)
    {
        if (!result.Successful())
        {
            Log.Debug("Failed to send message...");
        }
    }

    private void OnLobbyCreatedCallback(ulong lobbyId)
    {
        OnLobbyCreated?.Invoke(client.GetLobbyHandle(lobbyId));
    }

    private void OnUserUpdatedCallback(ulong userId)
    {
        OnUserUpdated?.Invoke();
    }

    private void OnMessageCreated(ulong messageId)
    {
        var msg = client.GetMessageHandle(messageId);

        if (msg == null)
            return;

        AddMsgHistory(msg.ChannelId(), msg);

        OnMessageReceived?.Invoke(msg);

        var author = msg.Author();
        var channel = msg.Channel();

        if (author == null || channel == null)
            return;

        var id = channel.Id();

        if (channel.Type() == ChannelType.Dm)
        {
            id = author.Id();

            if (id == UserID)           //Message was sent by us
                id = msg.RecipientId(); //Put this into the dmg history for this user
        }

        AddMsgHistory(id, msg);

        MessageManager.HandleMessage
            (null, $"[Discord] {author.DisplayName()}: {msg.Content()}", string.Empty, GetHueFromId(author.Id()), MessageType.Regular, 255, TextType.SYSTEM);
    }

    private void AddMsgHistory(ulong id, MessageHandle msg)
    {
        if (!messageHistory.ContainsKey(id))
            messageHistory.Add(id, new List<MessageHandle>());

        var list = messageHistory[id];
        list.Add(msg);

        var excess = list.Count - MAX_MSG_HISTORY;

        if (excess > 0)
            list.RemoveRange(0, excess);
    }

    private static void OnLog(string message, LoggingSeverity severity)
    {
        Log.Debug($"Log: {severity} - {message}");
    }

    private void OnStatusChanged(DClient.Status status, DClient.Error error, int errorCode)
    {
        Log.Debug($"Status changed: {status}");
        SetStatusText(status.ToString());

        if (error != DClient.Error.None)
        {
            Log.Error($"Error: {error}, code: {errorCode}");
        }

        switch (status)
        {
            case DClient.Status.Disconnecting:
            case DClient.Status.Disconnected:
                connected = false;
                Log.Debug("Discord disconnected, reconnecting...");
                client.Connect();

                break;
        }

        if (status == DClient.Status.Ready)
        {
            ClientReady();
        }
    }

    private void OnUOConnected(object sender, EventArgs e)
    {
        client.CreateOrJoinLobbyWithMetadata(GenServerSecret(), GenServerMeta(), new Dictionary<string, string>(), (_, _) => { });
    }

    private void ClientReady()
    {
        UserID = client.GetCurrentUser().Id();

        connected = true;
        OnConnected?.Invoke();

        UpdateRichPresence();

        client.CreateOrJoinLobbyWithMetadata(TUOLOBBY, TUOMETA, new Dictionary<string, string>(), (_, _) => { });
    }

    private string GenServerSecret()
    {
        return World.ServerName + Settings.GlobalSettings.IP;
    }

    private Dictionary<string, string> GenServerMeta()
    {
        return new Dictionary<string, string>()
        {
            { "name", World.ServerName }
        };
    }

    private void UpdateRichPresence()
    {
        Activity activity = new Activity();
        activity.SetName("Ultima Online");
        activity.SetType(ActivityTypes.Playing);

        var sec = new ActivitySecrets();

        sec.SetJoin
        (
            JsonSerializer.Serialize
                (new ServerInfo(Settings.GlobalSettings.IP, Settings.GlobalSettings.Port.ToString(), World.ServerName), ServerInfoJsonContext.Default.ServerInfo)
        );

        activity.SetSecrets(sec);

        var ts = new ActivityTimestamps();
        ts.SetStart(0);
        activity.SetTimestamps(ts);

        client.UpdateRichPresence(activity, OnUpdateRichPresence);
    }

    private void OnUpdateRichPresence(ClientResult result)
    {
        //Do nothing
    }

    private void SetStatusText(string text)
    {
        statusText = text;
        OnStatusTextUpdated?.Invoke();
    }


    #region AuthFlow

    public void StartOAuthFlow()
    {
        Log.Debug("Starting Discord OAuth handshakes");
        SetStatusText("Attempting to connect...");
        var authorizationVerifier = client.CreateAuthorizationCodeVerifier();
        codeVerifier = authorizationVerifier.Verifier();

        var args = new AuthorizationArgs();
        args.SetClientId(clientId);
        args.SetScopes(DClient.GetDefaultCommunicationScopes());
        args.SetCodeChallenge(authorizationVerifier.Challenge());
        client.Authorize(args, OnAuthorizeResult);
        authBegan = true;
    }

    public void FromSavedToken()
    {
        var rpath = Path.Combine(CUOEnviroment.ExecutablePath, "Data", ".dratoken");

        if (!File.Exists(rpath))
            return;

        SetStatusText("Attempting to reconnect...");

        try
        {
            var rtoken = Crypter.Decrypt(File.ReadAllText(rpath));

            client.RefreshToken(clientId, rtoken, OnTokenExchangeCallback);
            authBegan = true;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void OnTokenExchangeCallback(ClientResult result, string token, string refreshToken, AuthorizationTokenType tokenType, int expiresIn, string scopes)
    {
        if (!result.Successful())
        {
            OnRetrieveTokenFailed();

            return;
        }

        OnReceivedToken(token, refreshToken);
    }

    private void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
    {
        Log.Debug($"Authorization result: [{result.Error()}] [{code}] [{redirectUri}]");
        SetStatusText("Handshake in progress...");

        if (!result.Successful())
        {
            OnRetrieveTokenFailed();

            return;
        }

        GetTokenFromCode(code, redirectUri);
    }

    private void GetTokenFromCode(string code, string redirectUri)
    {
        client.GetToken(clientId, code, codeVerifier, redirectUri, TokenExchangeCallback);
    }

    private void TokenExchangeCallback(ClientResult result, string token, string refreshToken, AuthorizationTokenType tokenType, int expiresIn, string scopes)
    {
        //TODO: Handle token expirations
        if (token != "")
        {
            OnReceivedToken(token, refreshToken);
        }
        else
        {
            OnRetrieveTokenFailed();
        }
    }

    private void OnReceivedToken(string token, string refresh)
    {
        Log.Debug("Token received");
        SetStatusText("Almost done...");

        try
        {
            File.WriteAllText(Path.Combine(CUOEnviroment.ExecutablePath, "Data", ".dratoken"), Crypter.Encrypt(refresh));
        }
        catch (Exception e)
        {
            Log.Error(e.ToString());
        }

        client.UpdateToken(AuthorizationTokenType.Bearer, token, (ClientResult result) => { client.Connect(); });
    }

    private void OnRetrieveTokenFailed()
    {
        SetStatusText("Failed to retrieve token");
    }

    #endregion
}

public delegate void SimpleEventHandler(object sender);
public delegate void EmptyEventHandler();

public struct ServerInfo(string ip, string port, string name)
{
    public string Ip { get; set; } = ip;
    public string Port { get; set; } = port;
    public string Name { get; set; } = name;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerInfo))]
public partial class ServerInfoJsonContext : JsonSerializerContext
{
}