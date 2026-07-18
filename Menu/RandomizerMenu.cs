using System.Collections.Generic;
using Menu;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class RandomizerMenu : RWMenu
{
    private const float TOP_MARGIN = 50f;
    private readonly Vector2 buttonSize = new Vector2(100f, 30f);
    
    /// <summary>
    /// Anchors on left and right side of screen
    /// </summary>
    private Vector2 anchors;
    
    private SimpleButton exitButton;
    private SimpleButton startButton; // Temp for testing
    
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

        SaveData.SaveTracker.OrigSaveSlot = manager.rainWorld.options.saveSlot;
        SaveData.SaveTracker.CustomSlotActive = true;
        manager.rainWorld.options.saveSlot = 100; //temp
        SaveData.SaveTracker.AddNewSaveSlot(100, SlugcatStats.Name.White);
        manager.rainWorld.progression.Destroy(SaveData.SaveTracker.OrigSaveSlot);
        manager.rainWorld.progression = new PlayerProgression(manager.rainWorld, true, false);

        pages = [
            new Page(this, null, "SCENE", 0),
            new Page(this, null, "SELECT", 1),
        ];

        scene = new InteractiveMenuScene(this, null, MenuScene.SceneID.Landscape_SS)
        {
            blurMax = 250f,
            blurMin = 150f,
        };
        pages[0].subObjects.Add(scene);

        exitButton = new SimpleButton(this, pages[1], Translate("BACK"), "EXIT",
            new Vector2(anchors.x + 50f, manager.rainWorld.options.ScreenSize.y - TOP_MARGIN),
            buttonSize);
        pages[1].subObjects.Add(exitButton);
        backObject = exitButton;
        
        startButton = new SimpleButton(this, pages[1], Translate("START"), "START",
            exitButton.pos + new Vector2(buttonSize.x + 30f, 0),
            buttonSize);
        pages[1].subObjects.Add(startButton);
        
        currentPage = 1;
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "EXIT":
                PlaySound(SoundID.MENU_Switch_Page_Out);
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
                break;
            case "START":
                StartGame();
                break;
        }
    }

    public override void Update()
    {
        base.Update();
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
    }

    private void StartGame()
    {
        SlugcatStats.Name slugcat = SlugcatStats.Name.White;

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
        
        
        // Save slot switching - ExpeditionMenu.Ctor
        
        
    }
}