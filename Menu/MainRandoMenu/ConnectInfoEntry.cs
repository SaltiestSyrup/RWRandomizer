using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class ConnectInfoEntry : RectangularMenuObject
{
    // Wrappers
    private MenuTabWrapper tabWrapper;
    private UIelementWrapper hostNameWrapper;
    private UIelementWrapper portWrapper;
    private UIelementWrapper slotNameWrapper;
    private UIelementWrapper passwordWrapper;
    public static Configurable<string> HostNameConfig;
    public static Configurable<int> PortConfig;
    public static Configurable<string> SlotNameConfig;
    public static Configurable<string> PasswordConfig;
    
    // Elements
    private MenuLabel hostNameLabel;
    private MenuLabel portLabel;
    private MenuLabel slotNameLabel;
    private MenuLabel passwordLabel;
    public OpTextBox hostNameField;
    public OpTextBox portField;
    public OpTextBox slotNameField;
    public OpTextBox passwordField;

    private RoundedRect rectBorder;
    private SimpleButton connectButton;
    
    // TODO: Allow tab to select next text field
    public ConnectInfoEntry(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, new Vector2(240f, 300f))
    {
        this.pos -= size / 2f;
        
        rectBorder = new RoundedRect(menu, this, 
            new Vector2(), new Vector2(240f, 330f), true)
        {
            filled = true,
            fillAlpha = 0.6f
        };
        subObjects.Add(rectBorder);
        
        tabWrapper = new MenuTabWrapper(menu, this);
        float centerX = 120f;
        float runningY = 300f;

        // Host Name
        hostNameLabel = new MenuLabel(menu, this, "Host Name", new Vector2(centerX, runningY), default, true)
        { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(hostNameLabel);
        runningY -= 40f;
        
        hostNameField = new OpTextBox(HostNameConfig, new Vector2(centerX - 100f, runningY), 200f)
        { maxLength = int.MaxValue };
        hostNameWrapper = new UIelementWrapper(tabWrapper, hostNameField);
        runningY -= 20f;
        
        // Port
        portLabel = new MenuLabel(menu, this, "Port", new Vector2(centerX, runningY), default, true)
        { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(portLabel);
        runningY -= 40f;
        
        portField = new OpTextBox(PortConfig, new Vector2(centerX - 27.5f, runningY), 55f);
        portWrapper = new UIelementWrapper(tabWrapper, portField);
        runningY -= 20f;
        
        // Slot Name
        slotNameLabel = new MenuLabel(menu, this, "Slot Name", new Vector2(centerX, runningY), default, true)
        { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(slotNameLabel);
        runningY -= 40f;
        
        slotNameField = new OpTextBox(SlotNameConfig, new Vector2(centerX - 90f, runningY), 180f)
        { allowSpace = true, maxLength = 16 };
        slotNameWrapper = new UIelementWrapper(tabWrapper, slotNameField);
        runningY -= 20f;
        
        // Password
        passwordLabel = new MenuLabel(menu, this, "Password", new Vector2(centerX, runningY), default, true)
        { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(passwordLabel);
        runningY -= 40f;
        
        passwordField = new OpTextBox(PasswordConfig, new Vector2(centerX - 100f, runningY), 200f)
        { allowSpace = true, maxLength = int.MaxValue };
        passwordWrapper = new UIelementWrapper(tabWrapper, passwordField);
        runningY -= 50f;
        
        subObjects.Add(tabWrapper);

        connectButton = new SimpleButton(menu, this, "CONNECT", "CONNECT",
            new Vector2(centerX - 50f, runningY), new Vector2(100f, 30f));
        subObjects.Add(connectButton);
    }
    
    public override void Update()
    {
        base.Update();
        hostNameLabel.inactive = ArchipelagoConnection.SocketConnected;
        portLabel.inactive = ArchipelagoConnection.SocketConnected;
        slotNameLabel.inactive = ArchipelagoConnection.SocketConnected;
        passwordLabel.inactive = ArchipelagoConnection.SocketConnected;
        
        connectButton.buttonBehav.greyedOut = ArchipelagoConnection.SocketConnected;
        ((CreateNewGamePage)owner.owner).modeButtons[0].buttonBehav.greyedOut = ArchipelagoConnection.SocketConnected;
        hostNameField.greyedOut = ArchipelagoConnection.SocketConnected;
        portField.greyedOut = ArchipelagoConnection.SocketConnected;
        slotNameField.greyedOut = ArchipelagoConnection.SocketConnected;
        passwordField.greyedOut = ArchipelagoConnection.SocketConnected;
    }
}