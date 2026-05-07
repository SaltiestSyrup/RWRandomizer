using Menu;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

/// <summary>
/// Indication of Archipelago connection status
/// </summary>
public class ConnectionStatusDisplay : PositionedMenuObject
{
    private const float BUTTON_DEFAULT_Y = -40f;

    public FSprite logoSprite;
    public MenuLabel statusLabel;
    public SimpleButton reconnectButton;

    private readonly Color connectedColor = Color.green;
    private readonly Color disconnectedColor = Color.red;

    private bool lastConnected;
    private bool IsConnected => ArchipelagoConnection.SocketConnected;

    public ConnectionStatusDisplay(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        lastConnected = IsConnected;

        myContainer = new FContainer();
        owner.Container.AddChild(myContainer);

        statusLabel = new MenuLabel(menu, this, IsConnected ? "Connected" : "Disconnected", new Vector2(0.01f, 0.01f), default, false);
        statusLabel.label.alignment = FLabelAlignment.Center;
        statusLabel.label.color = IsConnected ? connectedColor : disconnectedColor;
        subObjects.Add(statusLabel);

        reconnectButton = new SimpleButton(menu, this, "Reconnect", "RECONNECT", new Vector2(-40f, BUTTON_DEFAULT_Y), new Vector2(80f, 30f));
        reconnectButton.buttonBehav.greyedOut = IsConnected;
        if (IsConnected)
        {
            // Move button offscreen to hide it when already connected
            reconnectButton.pos = new Vector2(reconnectButton.pos.x, 1000);
            reconnectButton.lastPos = reconnectButton.pos;
        }
        subObjects.Add(reconnectButton);
    }

    public override void Update()
    {
        base.Update();
        if (IsConnected != lastConnected)
        {
            statusLabel.label.text = IsConnected ? "Connected" : "Disconnected";
            statusLabel.label.color = IsConnected ? connectedColor : disconnectedColor;
            reconnectButton.buttonBehav.greyedOut = IsConnected;
            reconnectButton.pos = new Vector2(reconnectButton.pos.x, IsConnected ? 1000 : BUTTON_DEFAULT_Y);
            reconnectButton.lastPos = reconnectButton.pos;
        }
        reconnectButton.buttonBehav.greyedOut = IsConnected || ArchipelagoConnection.CurrentlyConnecting;
        lastConnected = IsConnected;
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);

        if (message == "RECONNECT")
        {
            ArchipelagoConnection.ReconnectAsync();
        }
    }
}