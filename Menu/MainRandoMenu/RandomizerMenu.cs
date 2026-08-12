using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Newtonsoft.Json;
using RainWorldRandomizer.SaveData;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class RandomizerMenu : RWMenu
{
    public const float TOP_MARGIN = 50f;
    public readonly Vector2 buttonSize = new Vector2(100f, 30f);
    private readonly Vector2 exitButtonPos;
    
    /// <summary>
    /// Anchors on left and right side of screen
    /// </summary>
    private Vector2 anchors;
    private bool pagesMoving;
    private Vector2 newPagePos;
    private Vector2[] oldPagePos;
    private float movementCounter;

    // Pages
    private CreateNewGamePage createNewGamePage;
    private CampaignSelectPage campaignSelectPage;

    // Elements
    public OptionsDialog optionsDialog;
    private SimpleButton exitButton;
    // private DialogBoxNotify failedStartGameDialog;
    // public DialogNotify connectResultDialog;

    // Vars
    public SaveTracker saveTracker = new();
    private TaskCompletionSource<bool> progressionIsLoading = null;

    internal bool _freezeMenuFunctions;
    public override bool FreezeMenuFunctions
    {
        get { return base.FreezeMenuFunctions || _freezeMenuFunctions; }
    }

    public RandomizerMenu(ProcessManager manager) : base(manager, RandomizerEnums.ProcessID.RandomizerMenu)
    {
        
        anchors = new Vector2(Custom.GetScreenOffsets()[0], Custom.GetScreenOffsets()[1]);
        
        /*
            Central saved games tab
                Save game container
                AP and Standalone version
            Connection info pop-up
            Chosen options viewer (takes from AP or S)
            Standalone options editor
            
        */

        pages = [
            new Page(this, null, "SCENE", 0),
            new Page(this, null, "SELECT", 1),
            new Page(this, null, "CREATE", 2),
        ];

        // TODO: Change landscape scene
        scene = new InteractiveMenuScene(this, null, MenuScene.SceneID.Landscape_SS)
        {
            blurMax = 250f,
            blurMin = 150f,
        };
        pages[0].subObjects.Add(scene);

        // Exit button
        exitButtonPos = new Vector2(anchors.x + 50f, manager.rainWorld.options.ScreenSize.y - TOP_MARGIN);
        exitButton = new SimpleButton(this, pages[1], Translate("BACK"), "EXIT",
            exitButtonPos, buttonSize);
        pages[1].subObjects.Add(exitButton);
        backObject = exitButton;

        // Load existing game page
        campaignSelectPage = new CampaignSelectPage(this, pages[1], default);
        pages[1].subObjects.Add(campaignSelectPage);

        // Create new game page
        createNewGamePage = new CreateNewGamePage(this, pages[2], default);
        pages[2].subObjects.Add(createNewGamePage);
        pages[2].pos.x += 1500f;
        pages[2].lastPos = pages[2].pos;
        
        currentPage = 1;
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "EXIT":
                switch (currentPage)
                {
                    case 0 or 1:
                        PlaySound(SoundID.MENU_Switch_Page_Out);
                        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                        break;
                    case 2 or _:
                        MovePage(true);
                        UpdatePage(1);
                        break;
                }
                break;
            case "CONTINUE_GAME":
                // TODO: Make this lead to a validation step which checks AP connections / DLC enabled
                if (sender is SlotSelector.Slot slot)
                {
                    ContinueGame(slot.saveSlot, new SlugcatStats.Name(slot.saveFile.slugcat));
                }
                else
                {
                    Plugin.Log.LogError("Failed to determine slot to begin game with");
                }
                break;
            case "NEW_GAME":
                MovePage(false);
                UpdatePage(2);
                break;
            case "START_NEW_GAME":
                CreateNewGame(((CreateNewGamePage)sender.owner).chosenSlugcat);
                break;
        }
    }

    public override void Update()
    {
        base.Update();
        if (progressionIsLoading is not null
            && !manager.rainWorld.progression.requestLoad
            && !manager.rainWorld.progression.loadInProgress)
        {
            progressionIsLoading.TrySetResult(manager.rainWorld.progression.progressionLoaded);
        }
        
        // Page switching animation
        if (!pagesMoving) return;

        // Code taken from ExpeditionMenu.Update
        movementCounter += 0.195f;
        float scurveVal = Mathf.Lerp(8f, 125f, Custom.SCurve(movementCounter, 0.85f));
        for (int i = 1; i < pages.Count; i++)
        {
            Vector2 target = oldPagePos[i] + newPagePos;
            float totalDist = Vector2.Distance(oldPagePos[i], target);
            float remainingDist = Vector2.Distance(pages[i].pos, target);
            float speed = Mathf.Lerp(1f, 0.01f, Mathf.InverseLerp(totalDist, 0.1f, remainingDist));
            pages[i].pos = Custom.MoveTowards(pages[i].pos, target, scurveVal * speed);
            if (pages[i].pos == target) pagesMoving = false;
        }
        if (!pagesMoving) PlaySound(SoundID.MENU_Checkbox_Check);
        exitButton.pos = exitButtonPos - exitButton.page.pos;
        exitButton.lastPos = exitButtonPos - exitButton.page.lastPos;
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
    }

    public void MovePage(bool moveLeft)
    {
        if (pagesMoving) return;
        pagesMoving = true;
        movementCounter = 0f;
        newPagePos = new Vector2(1500f * (moveLeft ? 1f : -1f), 0f);
        oldPagePos = new Vector2[pages.Count];
        for (int i = 0; i < oldPagePos.Length; i++)
            oldPagePos[i] = pages[i].pos;
        PlaySound(SoundID.MENU_Next_Slugcat);
    }

    public void UpdatePage(int newPage)
    {
        // TODO: Auto default cursor selection
        
        // Menu objects that persist across pages
        exitButton.RemoveSprites();
        pages[currentPage].RemoveSubObject(exitButton);
        exitButton = new SimpleButton(this, pages[newPage], Translate("BACK"), "EXIT",
            exitButtonPos, buttonSize);
        pages[newPage].subObjects.Add(exitButton);
        backObject = exitButton;

        if (newPage == 2) // New game page
        {
            createNewGamePage.Enable();
        }
        else
        {
            createNewGamePage.Disable();
        }
        
        currentPage = newPage;
    }

    private void MoveToNewGameScreen()
    {
        
    }

    private void CreateNewGame(SlugcatStats.Name slugcat)
    {
        SaveTracker.OrigSaveSlot = manager.rainWorld.options.saveSlot;
        SaveTracker.CustomSlotActive = true;
        if (!saveTracker.TryGetNextSaveSlot(manager.rainWorld.options.saveSlot, out int newSlot))
        {
            Plugin.Log.LogError("Failed to find new valid save slot number");
            return;
        }
        manager.rainWorld.options.saveSlot = newSlot;
        manager.rainWorld.progression.Destroy(SaveTracker.OrigSaveSlot);
        manager.rainWorld.progression = new PlayerProgression(manager.rainWorld, true, false);
        
        StartGame(slugcat);
    }

    private void ContinueGame(int slot, SlugcatStats.Name slugcat)
    {
        SaveTracker.OrigSaveSlot = manager.rainWorld.options.saveSlot;
        SaveTracker.CustomSlotActive = true;
        manager.rainWorld.options.saveSlot = slot;
        manager.rainWorld.progression.Destroy(SaveTracker.OrigSaveSlot);
        manager.rainWorld.progression = new PlayerProgression(manager.rainWorld, true, false);
        
        StartGame(slugcat);
    }
    
    private async void StartGame(SlugcatStats.Name slugcat)
    {
        try
        {
            // New progression object was just made, needs to wait until the game finishes loading it (which happens asynchronously)
            progressionIsLoading = new TaskCompletionSource<bool>();
            await progressionIsLoading.Task;

            if (!progressionIsLoading.Task.Result) throw new LoadDataException();

            progressionIsLoading = null;
        
            if (ModManager.CoopAvailable)
            {
                Custom.Log("JollyCoop Player Count is:", manager.rainWorld.options.JollyPlayerCount.ToString());
                for (int i = 1; i < manager.rainWorld.options.JollyPlayerCount; i++)
                {
                    manager.rainWorld.ActivatePlayer(i);
                }
                for (int j = manager.rainWorld.options.JollyPlayerCount; j < 4; j++)
                {
                    manager.rainWorld.DeactivatePlayer(j);
                }
            }
        
            manager.rainWorld.inGameSlugCat = slugcat;
            manager.arenaSitting = null;
            manager.rainWorld.progression.currentSaveState = null;
            manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = slugcat;

            if (manager.rainWorld.progression.IsThereASavedGame(slugcat))
            {
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                PlaySound(SoundID.MENU_Continue_Game);
            }
            else
            {
                manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
                PlaySound(SoundID.MENU_Start_New_Game);
            }
        
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
        }
        catch (Exception e)
        {
            manager.ShowDialog(new DialogNotify($"Encountered exception while attempting to start game:\n{e}", manager, () => { }));
            Plugin.Log.LogError($"Encountered exception while attempting to start game:\n{e}");
        }
    }
}