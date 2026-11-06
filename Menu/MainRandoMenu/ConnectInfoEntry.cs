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

    private SimpleButton connectButton;

    // Vars
    private const float LABEL_SEP = 40f;
    private const float BUTTON_SEP = 40f;

    public ConnectInfoEntry(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos,
        new Vector2(240f, 300f))
    {
        this.pos -= size / 2f;

        tabWrapper = new MenuTabWrapper(menu, this);
        float centerX = 120f;
        float runningY = 320f;

        // Host Name
        hostNameLabel = new MenuLabel(menu, this, menu.Translate("HOST NAME"), new Vector2(centerX, runningY), default,
                false)
            { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(hostNameLabel);
        runningY -= LABEL_SEP;

        hostNameField = new OpTextBox(HostNameConfig, new Vector2(centerX - 100f, runningY), 200f)
            { maxLength = int.MaxValue };
        hostNameWrapper = new UIelementWrapper(tabWrapper, hostNameField);
        hostNameWrapper.glow.Hide();
        runningY -= BUTTON_SEP;

        // Port
        portLabel = new MenuLabel(menu, this, menu.Translate("PORT"), new Vector2(centerX, runningY), default, false)
            { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(portLabel);
        runningY -= LABEL_SEP;

        portField = new OpTextBox(PortConfig, new Vector2(centerX - 27.5f, runningY), 55f);
        portWrapper = new UIelementWrapper(tabWrapper, portField);
        portWrapper.glow.Hide();
        runningY -= BUTTON_SEP;

        // Slot Name
        slotNameLabel = new MenuLabel(menu, this, menu.Translate("SLOT NAME"), new Vector2(centerX, runningY), default,
                false)
            { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(slotNameLabel);
        runningY -= LABEL_SEP;

        slotNameField = new OpTextBox(SlotNameConfig, new Vector2(centerX - 90f, runningY), 180f)
            { allowSpace = true, maxLength = 16 };
        slotNameWrapper = new UIelementWrapper(tabWrapper, slotNameField);
        slotNameWrapper.glow.Hide();
        runningY -= BUTTON_SEP;

        // Password
        passwordLabel = new MenuLabel(menu, this, menu.Translate("PASSWORD"), new Vector2(centerX, runningY), default,
                false)
            { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(passwordLabel);
        runningY -= LABEL_SEP;

        passwordField = new OpTextBox(PasswordConfig, new Vector2(centerX - 100f, runningY), 200f)
            { allowSpace = true, maxLength = int.MaxValue, password = true };
        passwordWrapper = new UIelementWrapper(tabWrapper, passwordField);
        passwordWrapper.glow.Hide();
        runningY -= BUTTON_SEP + 30f;

        subObjects.Add(tabWrapper);

        connectButton = new SimpleButton(menu, this, "CONNECT", "CONNECT",
            new Vector2(centerX - 50f, runningY), new Vector2(100f, 30f));
        subObjects.Add(connectButton);
    }

    public override void Update()
    {
        base.Update();
        Color newCol = RWMenu.MenuRGB(ArchipelagoConnection.SocketConnected
            ? RWMenu.MenuColors.DarkGrey
            : RWMenu.MenuColors.MediumGrey);
        hostNameLabel.label.color = newCol;
        portLabel.label.color = newCol;
        slotNameLabel.label.color = newCol;
        passwordLabel.label.color = newCol;

        connectButton.buttonBehav.greyedOut = ArchipelagoConnection.SocketConnected;
        ((CreateNewGamePage)owner.owner).modeButtons[0].buttonBehav.greyedOut = ArchipelagoConnection.SocketConnected;
        hostNameField.greyedOut = ArchipelagoConnection.SocketConnected;
        portField.greyedOut = ArchipelagoConnection.SocketConnected;
        slotNameField.greyedOut = ArchipelagoConnection.SocketConnected;
        passwordField.greyedOut = ArchipelagoConnection.SocketConnected;
    }
}