using System;
using System.Linq;
using ClassicUO.Assets;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Input;
using Discord.Sdk;
using DiscordSocialSDK.Wrapper;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Gumps;

public class DiscordGump : Gump
{
    public ulong ActiveChannel => _selectedChannel;

    private const int WIDTH = 800, LEFT_WIDTH = 200;
    private const int HEIGHT = 700;

    private NiceButton _connect;
    private EmbeddedGumpPic _discordLogo;
    private TextBox _statusText;
    private DataBox _chatDataBox;
    private ModernOptionsGump.InputField _chatInput;
    private ScrollArea _chatScroll;
    private DiscordFriendListControl _discordFriendList;
    private DiscordChannelListControl _discordChannelList;
    private ulong _selectedChannel;
    private bool isDM;

    public DiscordGump() : base(0, 0)
    {
        Width = WIDTH;
        Height = HEIGHT;
        CanMove = true;

        BuildLeftArea();
        BuildChatArea();

        DiscordManager.Instance.OnStatusTextUpdated += OnStatusTextUpdated;
        DiscordManager.Instance.OnUserUpdated += OnUserUpdated;
        DiscordManager.Instance.OnMessageReceived += OnMessageReceived;

        CenterXInViewPort();
        CenterYInViewPort();
    }

    private void OnMessageReceived(object sender)
    {
        var msg = (MessageHandle)sender;

        if (msg == null)
            return;
        
        
        if (msg.Channel()?.Type() == ChannelType.Dm)
        {
            OnDMReceived(msg);

            return;
        }
        
        _discordChannelList.OnChannelActivity(msg.ChannelId());

        if (_selectedChannel != msg.ChannelId())
            return;

        AddMessageToChatBox(msg);
    }

    private void OnDMReceived(MessageHandle msg)
    {
        var id = msg.AuthorId();

        if (id == DiscordManager.Instance.UserID) //Message was sent by us
            id = msg.RecipientId();               //Put this into the dmg history for this user
        
        _discordChannelList.OnChannelActivity(id);

        if (_selectedChannel != id)
            return;

        //TODO: Use real timestamp from discord
        AddMessageToChatBox(msg);
    }

    private void OnUserUpdated()
    {
        _discordFriendList.BuildFriendsList();
    }

    private void BuildLeftArea()
    {
        AcceptMouseInput = true;

        _discordLogo = new(LEFT_WIDTH / 2 - 66, HEIGHT / 2 - 50, PNGLoader.Instance.EmbeddedArt["Discord-Symbol-Blurple-SM.png"]);
        Add(_discordLogo);

        AlphaBlendControl c;

        Add
        (
            c = new(0.75f)
            {
                Width = LEFT_WIDTH,
                Height = HEIGHT,
            }
        );

        c.BaseColor = new Color(21, 21, 21);

        if (!DiscordManager.Instance.Connected)
        {
            _connect = new(LEFT_WIDTH - 100, 0, 100, 30, ButtonAction.Activate, "Login");
            _connect.MouseUp += OnConnectClicked;
            Add(_connect);
        }

        var splitH = (HEIGHT - 20) / 2;

        _statusText = TextBox.GetOne(DiscordManager.Instance.StatusText, TrueTypeLoader.EMBEDDED_FONT, 16f, Color.DarkOrange, TextBox.RTLOptions.Default());
        Add(_statusText);

        Add
        (
            _discordChannelList = new DiscordChannelListControl(LEFT_WIDTH, splitH, this)
            {
                Y = 20
            }
        );
        
        Add(new Line(0, _discordChannelList.Height + _discordChannelList.Y, LEFT_WIDTH, 1, 0xFF383838));

        _discordChannelList.BuildChannelList();

        Add
        (
            _discordFriendList = new DiscordFriendListControl(LEFT_WIDTH, splitH - 1, this)
            {
                Y = _discordChannelList.Height + _discordChannelList.Y + 1 //1 is for the line
            }
        );

        _discordFriendList.BuildFriendsList();
    }

    private void BuildChatArea()
    {
        AlphaBlendControl c;

        Add
        (
            c = new AlphaBlendControl(0.75f)
            {
                X = LEFT_WIDTH + 5,
                Width = WIDTH - LEFT_WIDTH - 5,
                Height = HEIGHT,
            }
        );

        c.BaseColor = new(31, 31, 31);

        _chatScroll = new(c.X, 0, c.Width, HEIGHT - 20, true)
        {
            ScrollbarBehaviour = ScrollbarBehaviour.ShowAlways
        };

        Add(_chatScroll);

        _chatScroll.Add(_chatDataBox = new DataBox(0, 0, _chatScroll.Width, 0));

        _chatInput = new(c.Width, 20)
        {
            X = c.X,
            Y = c.Height - 20
        };
        
        _chatInput.EnterPressed += ChatInputOnEnterPressed;
        Add(_chatInput);
    }

    private void AddMessageToChatBox(MessageHandle msg, bool rearrange = true)
    {
        _chatDataBox.Add(new DiscordMessageControl(msg, _chatScroll.Width));

        if (rearrange)
            _chatDataBox.ReArrangeChildren();
    }

    private void AddMessageToChatBox(string text, Color hue, bool rearrange = true)
    {
        _chatDataBox.Add(TextBox.GetOne(text, TrueTypeLoader.EMBEDDED_FONT, 20f, hue, TextBox.RTLOptions.Default(_chatScroll.Width - 20)));

        if (rearrange)
            _chatDataBox.ReArrangeChildren();
    }

    public void SetActiveChatChannel(ulong channelId, bool isDM)
    {
        _selectedChannel = channelId;
        this.isDM = isDM;

        _chatDataBox.Clear();

        if (DiscordManager.Instance.MessageHistory.ContainsKey(channelId))
            foreach (var m in DiscordManager.Instance.MessageHistory[channelId])
                AddMessageToChatBox(m, false);
        else
            AddMessageToChatBox("No messages yet..", Color.Gray, false);

        _chatDataBox.ReArrangeChildren();

        _discordFriendList.UpdateSelectedFriend();
        _discordChannelList.UpdateSelectedChannel();
    }

    private void ChatInputOnEnterPressed(object sender, EventArgs e)
    {
        if (_selectedChannel == 0)
            return;

        string txt = _chatInput.Text;

        if (string.IsNullOrEmpty(txt))
            return;

        _chatInput.SetText(string.Empty);

        if (isDM)
            DiscordManager.Instance.SendDM(_selectedChannel, txt);
        else
            DiscordManager.Instance.SendChannelMsg(_selectedChannel, txt);
    }

    private void OnStatusTextUpdated()
    {
        _statusText.Text = DiscordManager.Instance.StatusText;
    }

    private void OnConnectClicked(object sender, MouseEventArgs e)
    {
        DiscordManager.Instance.OnConnected += DiscordOnConnected;
        _connect.Dispose();
        DiscordManager.Instance.StartOAuthFlow();
    }

    private void DiscordOnConnected()
    {
        _discordFriendList.BuildFriendsList();
        DiscordManager.Instance.OnConnected -= DiscordOnConnected;
    }

    public override void Dispose()
    {
        base.Dispose();
        DiscordManager.Instance.OnStatusTextUpdated -= OnStatusTextUpdated;
        DiscordManager.Instance.OnUserUpdated -= OnUserUpdated;
    }
}