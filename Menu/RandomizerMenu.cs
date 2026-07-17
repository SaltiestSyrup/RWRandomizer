using System.Collections.Generic;
using Menu;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class RandomizerMenu : RWMenu
{
    private const float TOP_MARGIN = 50f;
    
    /// <summary>
    /// Anchors on left and right side of screen
    /// </summary>
    private Vector2 anchors;
    
    private SimpleButton exitButton;
    
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
        ];

        scene = new InteractiveMenuScene(this, null, MenuScene.SceneID.Landscape_SS)
        {
            blurMax = 250f,
            blurMin = 150f,
        };
        pages[0].subObjects.Add(scene);

        exitButton = new SimpleButton(this, pages[1], Translate("BACK"), "EXIT",
            new Vector2(anchors.x + 50f, manager.rainWorld.options.ScreenSize.y - TOP_MARGIN),
            new Vector2(100f, 30f));
        pages[1].subObjects.Add(exitButton);
        backObject = exitButton;

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
}