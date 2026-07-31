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
    // Elements
    private ConnectInfoEntry connectInfoEntry;
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

        connectInfoEntry = new ConnectInfoEntry(menu, this, new Vector2());
        subObjects.Add(connectInfoEntry);
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
                // TODO: Allow browsing options before jumping into game
                if (ArchipelagoConnection.SocketConnected)
                {
                    ((CreateNewGamePage)owner).chosenSlugcat = ArchipelagoConnection.Slugcat;
                    base.Singal(this, "START_NEW_GAME");
                }
                break;
        }
    }

    public override void Update()
    {
        base.Update();

        if (connectTask?.IsCompleted ?? false)
        {
            page.subObjects.Remove(establishConnectionDialog);
            establishConnectionDialog.RemoveSprites();
            establishConnectionDialog = null;

            RandoOptions.LoadedOptions = ArchipelagoConnection.ConnectedOptions;
            
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
            new Vector2(menu.manager.rainWorld.screenSize.x / 2 - 125f, menu.manager.rainWorld.screenSize.y / 2 - 75f), 
            new Vector2(250f, 150f))
        { loadingSpinner = { animSpeed = 0.5f } };
        page.subObjects.Add(establishConnectionDialog);

        connectTask = Task.Run<string>(() =>
        {
            try
            {
                return ArchipelagoConnection.Connect(
                    connectInfoEntry.hostNameField.value, 
                    connectInfoEntry.portField.valueInt,
                    connectInfoEntry.slotNameField.value,
                    connectInfoEntry.passwordField.value);
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