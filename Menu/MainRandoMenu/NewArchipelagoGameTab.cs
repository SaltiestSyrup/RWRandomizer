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
    
    private DialogBoxAsyncWait establishConnectionDialog;
    private DialogNotify connectResultDialog;
    
    // Vars
    private Task<string> connectTask;
    
    public NewArchipelagoGameTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        // Connect info fields in center
        // Slot data display on right
        // Start Game button on bottom

        
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "CONNECT":
                StartAsyncConnection();
                break;
        }
    }

    public void Enable()
    {
        connectInfoEntry = new ConnectInfoEntry(menu, this, new Vector2());
        subObjects.Add(connectInfoEntry);
    }

    public void Disable()
    {
        if (connectInfoEntry is null) return;
        
        connectInfoEntry.RemoveSprites();
        RemoveSubObject(connectInfoEntry);
        connectInfoEntry = null;
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
            if (ArchipelagoConnection.SocketConnected)
            {
                Disable(); // Remove connect info entry so that it can be used in options
                ((CreateNewGamePage)owner).chosenSlugcat = ArchipelagoConnection.Slugcat;
                // After options is closed enter the game
                ((RandomizerMenu)menu).optionsDialog = new OptionsDialog(menu.manager,
                    OptionsDialog.Mode.ArchipelagoNew, new SaveFile
                    {
                        options = ArchipelagoConnection.ConnectedOptions,
                        connectionInfo = new SaveFile.ConnectionInfo
                        {
                            hostName = ArchipelagoConnection.ConnectedHostName,
                            port = ArchipelagoConnection.ConnectedPort,
                            slotName = ArchipelagoConnection.ConnectedSlotName,
                            password = ArchipelagoConnection.ConnectedPassword,
                        },
                        startingDen = ArchipelagoConnection.desiredStartDen,
                        slugcat = ArchipelagoConnection.Slugcat.value,
                        isDownpourDLC = ModManager.MSC,
                        isWatcherDLC = ModManager.Watcher
                    },
                    () =>
                    {
                        try { Singal(this, "START_NEW_GAME"); }
                        catch (Exception e) { Plugin.Log.LogError(e); }
                    });
                menu.manager.ShowDialog(((RandomizerMenu)menu).optionsDialog);
            }
            else
            {
                connectResultDialog = new DialogNotify(connectTask.Result, menu.manager, () => { });
                menu.manager.ShowDialog(connectResultDialog);
            }
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