using System;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class NewArchipelagoGameTab : PositionedMenuObject
{
    // Wrappers
    private MenuTabWrapper tabWrapper;
    private UIelementWrapper hostNameWrapper;
    private UIelementWrapper portWrapper;
    private UIelementWrapper slotNameWrapper;
    private UIelementWrapper passwordWrapper;
    public static Configurable<string> HostNameConfig;
    public static  Configurable<int> PortConfig;
    public static Configurable<string> SlotNameConfig;
    public static Configurable<string> PasswordConfig;
    
    // Elements
    private MenuLabel hostNameLabel;
    private MenuLabel portLabel;
    private MenuLabel slotNameLabel;
    private MenuLabel passwordLabel;
    private OpTextBox hostNameField;
    private OpTextBox portField;
    private OpTextBox slotNameField;
    private OpTextBox passwordField;
    
    private SimpleButton connectButton;
    private HoldButton startButton;
    
    private DialogBoxAsyncWait establishConnectionDialog;
    private DialogBoxNotify connectResultDialog;
    
    // Vars
    private Task<string> connectTask;
    
    public NewArchipelagoGameTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        // Connect info fields in center
        // Slot data display on right
        // Start Game button on bottom

        tabWrapper = new MenuTabWrapper(menu, this);
        float runningY = 200f;

        // Host Name
        hostNameLabel = new MenuLabel(menu, this, "Host Name", new Vector2(0f, runningY), default, true)
        { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(hostNameLabel);
        runningY -= 40f;
        
        hostNameField = new OpTextBox(HostNameConfig, new Vector2(-100f, runningY), 200f)
        { maxLength = int.MaxValue };
        hostNameWrapper = new UIelementWrapper(tabWrapper, hostNameField);
        runningY -= 20f;
        
        // Port
        portLabel = new MenuLabel(menu, this, "Port", new Vector2(0f, runningY), default, true)
        { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(portLabel);
        runningY -= 40f;
        
        portField = new OpTextBox(PortConfig, new Vector2(-27.5f, runningY), 55f);
        portWrapper = new UIelementWrapper(tabWrapper, portField);
        runningY -= 20f;
        
        // Slot Name
        slotNameLabel = new MenuLabel(menu, this, "Slot Name", new Vector2(0f, runningY), default, true)
        { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(slotNameLabel);
        runningY -= 40f;
        
        slotNameField = new OpTextBox(SlotNameConfig, new Vector2(-90f, runningY), 180f)
        { allowSpace = true, maxLength = 16 };
        slotNameWrapper = new UIelementWrapper(tabWrapper, slotNameField);
        runningY -= 20f;
        
        // Password
        passwordLabel = new MenuLabel(menu, this, "Password", new Vector2(0f, runningY), default, true)
        { label = { alignment = FLabelAlignment.Center } };
        subObjects.Add(passwordLabel);
        runningY -= 40f;
        
        passwordField = new OpTextBox(PasswordConfig, new Vector2(-100f, runningY), 200f)
        { allowSpace = true, maxLength = int.MaxValue };
        passwordWrapper = new UIelementWrapper(tabWrapper, passwordField);
        runningY -= 50f;
        
        subObjects.Add(tabWrapper);

        connectButton = new SimpleButton(menu, this, "CONNECT", "CONNECT",
            new Vector2(-50f, runningY), new Vector2(100f, 30f));
        subObjects.Add(connectButton);
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "CONNECT":
                StartAsyncConnection();
                break;
            case "CONFIRM_CONNECT_RESULT":
                subObjects.Remove(connectResultDialog);
                connectResultDialog.RemoveSprites();
                connectResultDialog = null;
                break;
        }
    }

    public override void Update()
    {
        base.Update();
        connectButton.buttonBehav.greyedOut = ArchipelagoConnection.SocketConnected;
        ((CreateNewGamePage)owner).modeButtons[0].buttonBehav.greyedOut = ArchipelagoConnection.SocketConnected;
        hostNameField.greyedOut = ArchipelagoConnection.SocketConnected;
        portField.greyedOut = ArchipelagoConnection.SocketConnected;
        slotNameField.greyedOut = ArchipelagoConnection.SocketConnected;
        passwordField.greyedOut = ArchipelagoConnection.SocketConnected;

        if (connectTask?.IsCompleted ?? false)
        {
            page.subObjects.Remove(establishConnectionDialog);
            establishConnectionDialog.RemoveSprites();
            establishConnectionDialog = null;
            
            // If success, populate options UI. Else show error dialog
            connectResultDialog = new DialogBoxNotify(menu, this, connectTask.Result, "CONFIRM_CONNECT_RESULT", 
                new Vector2(-240f, -160f), new Vector2(480f, 320f));
            subObjects.Add(connectResultDialog);

            connectTask = null;
        }
    }

    private void StartAsyncConnection()
    {
        establishConnectionDialog = new DialogBoxAsyncWait(menu, page, "Connecting to server...", 
            new Vector2(menu.manager.rainWorld.screenSize.x / 2 -125f, menu.manager.rainWorld.screenSize.y / 2 -75f), 
            new Vector2(250f, 150f))
        { loadingSpinner = { animSpeed = 0.5f } };
        page.subObjects.Add(establishConnectionDialog);

        connectTask = Task.Run<string>(() =>
        {
            try
            {
                return ArchipelagoConnection.Connect(hostNameField.value, portField.valueInt,
                    slotNameField.value,
                    passwordField.value);
            }
            catch (Exception e)
            {
                string err = $"Encountered an exception while attempting to connect to server: \n{e}";
                Plugin.Log.LogError(err);
                return err;
            }
        });
    }
}