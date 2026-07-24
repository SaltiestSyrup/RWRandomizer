using Menu;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class CreateNewGamePage : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    // Elements
    public SelectOneButton[] modeButtons;
    private NewArchipelagoGameTab apTab;
    
    // Vars
    private readonly Vector2 screenCenter;
    private int currentMode = 0;
    
    public CreateNewGamePage(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        screenCenter = new Vector2(menu.manager.rainWorld.screenSize.x / 2f, menu.manager.rainWorld.screenSize.y / 2);
        
        modeButtons = new SelectOneButton[2];
        modeButtons[0] = new SelectOneButton(menu, this, "STANDALONE", "MODE-STANDALONE",
            new Vector2(screenCenter.x - 150f, menu.manager.rainWorld.screenSize.y - 80f),
            new Vector2(100f, 30f), modeButtons, 0);
        subObjects.Add(modeButtons[0]);
        modeButtons[1] = new SelectOneButton(menu, this, "ARCHIPELAGO", "MODE-ARCHIPELAGO",
            new Vector2(screenCenter.x + 50f, menu.manager.rainWorld.screenSize.y - 80f),
            new Vector2(100f, 30f), modeButtons, 1);
        subObjects.Add(modeButtons[1]);

        apTab = new NewArchipelagoGameTab(menu, this, screenCenter - new Vector2(0f, 1500f));
        subObjects.Add(apTab);
    }

    public int GetCurrentlySelectedOfSeries(string series)
    {
        return series.StartsWith("MODE-") ? currentMode : 0;
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        if (series.StartsWith("MODE-") && currentMode != to) SwitchMode(to);
    }

    private void SwitchMode(int newMode)
    {
        currentMode = newMode;

        switch (newMode)
        {
            case 0:
                apTab.pos.y = screenCenter.y - 1500f;
                break;
            case 1:
                apTab.pos.y = screenCenter.y;
                break;
        }
    }
}