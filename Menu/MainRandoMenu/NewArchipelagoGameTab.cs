using System;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class NewArchipelagoGameTab(RWMenu menu, MenuObject owner, Vector2 pos) : PositionedMenuObject(menu, owner, pos)
{
    // Elements
    private ConnectInfoEntry connectInfoEntry;
    
    private DialogBoxAsyncWait establishConnectionDialog;
    private DialogNotify connectResultDialog;
    
    // Vars
    private Task<string> connectTask;

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
        if (connectInfoEntry is not null) return;
        
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
            
            InitiateStartGameDialogs();
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

    private void InitiateStartGameDialogs()
    {
        RandoOptions.LoadedOptions = ArchipelagoConnection.ConnectedOptions;
        ((CreateNewGamePage)owner).chosenSlugcat = ArchipelagoConnection.Slugcat;
        
        // Show error dialog if connection failed
        if (!ArchipelagoConnection.SocketConnected)
        {
            connectResultDialog = new DialogNotify(connectTask.Result, menu.manager, () => { });
            menu.manager.ShowDialog(connectResultDialog);
            return;
        }

        if (SaveManager.HasLegacySave(ArchipelagoConnection.generationSeed, ArchipelagoConnection.ConnectedSlotName))
        {
            menu.manager.ShowDialog(new DialogConfirm(
                "Successfully connected to the Multiworld.\n" +
                "A legacy save file for this slot name and multiworld was found,\n" +
                "would you like to import the campaign data from the currently selected Rain World slot?\n" +
                "If not, a new game will be created instead and the old data will be discarded.",
                new Vector2(600f, 200f), 
                menu.manager,
                () => // On yes, load legacy file. Else go to options dialog as normal
                {
                    ArchipelagoConnection.lastItemIndex =
                        SaveManager.GetLastIndexFromLegacy(ArchipelagoConnection.generationSeed,
                            ArchipelagoConnection.ConnectedSlotName);
                    Singal(this, "CONTINUE_FROM_LEGACY");
                }, 
                () =>
                {
                    // Destroy old save if we aren't using it, so manager doesn't get confused
                    SaveManager.DestroyLegacySave(ArchipelagoConnection.generationSeed, 
                        ArchipelagoConnection.ConnectedSlotName, ArchipelagoConnection.Slugcat.value, 
                        menu.manager.rainWorld.options.saveSlot);
                    // Add directly to the stack, because calling ShowDialog here freezes the game
                    menu.manager.dialogStack.Add(CreateOptionsDialog());
                }));
            return;
        }
        
        menu.manager.ShowDialog(CreateOptionsDialog());
    }

    private OptionsDialog CreateOptionsDialog()
    {
        Disable(); // Remove connect info entry so that it can be used in options
        // After options is closed enter the game
        return ((RandomizerMenu)menu).optionsDialog = new OptionsDialog(menu.manager,
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
    }
}