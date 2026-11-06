using Menu;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class CreateNewGamePage : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    // Elements
    private MenuLabel modeLabel;
    public SelectOneButton[] modeButtons;
    private FSprite modeSeparatorLeft;
    private FSprite modeSeparatorRight;
    private NewStandaloneGameTab stTab;
    private NewArchipelagoGameTab apTab;

    // Vars
    private readonly Vector2 screenCenter;
    private int currentMode;
    public SlugcatStats.Name chosenSlugcat;

    public CreateNewGamePage(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        screenCenter = new Vector2(menu.manager.rainWorld.screenSize.x / 2f, menu.manager.rainWorld.screenSize.y / 2);

        modeLabel = new MenuLabel(menu, this, menu.Translate("CHOOSE A MODE"),
            new Vector2(screenCenter.x, menu.manager.rainWorld.screenSize.y - 60f),
            default, false)
        {
            label = { alignment = FLabelAlignment.Center, color = Color.gray }
        };
        subObjects.Add(modeLabel);

        modeButtons = new SelectOneButton[2];
        modeButtons[0] = new SelectOneButton(menu, this, "ARCHIPELAGO", "MODE-ARCHIPELAGO",
            new Vector2(screenCenter.x - 130f, menu.manager.rainWorld.screenSize.y - 120f),
            new Vector2(100f, 30f), modeButtons, 0);
        subObjects.Add(modeButtons[0]);
        modeButtons[1] = new SelectOneButton(menu, this, "STANDALONE", "MODE-STANDALONE",
            new Vector2(screenCenter.x + 30f, menu.manager.rainWorld.screenSize.y - 120f),
            new Vector2(100f, 30f), modeButtons, 1);
        subObjects.Add(modeButtons[1]);

        modeSeparatorLeft = new FSprite("LinearGradient200")
        {
            x = screenCenter.x,
            y = modeButtons[0].pos.y - 40f,
            rotation = 270,
            scaleY = 1.5f
        };
        modeSeparatorLeft.SetAnchor(0.5f, 0f);
        Container.AddChild(modeSeparatorLeft);
        modeSeparatorRight = new FSprite("LinearGradient200")
        {
            x = screenCenter.x,
            y = modeButtons[0].pos.y - 40f,
            rotation = 90,
            scaleY = 1.5f
        };
        modeSeparatorRight.SetAnchor(0.5f, 0f);
        Container.AddChild(modeSeparatorRight);

        apTab = new NewArchipelagoGameTab(menu, this, screenCenter);
        subObjects.Add(apTab);

        stTab = new NewStandaloneGameTab(menu, this, screenCenter - new Vector2(0f, 1500f));
        subObjects.Add(stTab);

        modeButtons[0].nextSelectable[1] = modeButtons[0];
        modeButtons[1].nextSelectable[1] = modeButtons[1];
    }

    public int GetCurrentlySelectedOfSeries(string series)
    {
        return series.StartsWith("MODE-") ? currentMode : 0;
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        if (series.StartsWith("MODE-") && currentMode != to) SwitchMode(to);
    }

    public void Enable()
    {
        apTab?.Enable();
    }

    public void Disable()
    {
        apTab?.Disable();
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        modeSeparatorLeft.x = Mathf.Lerp(owner.page.lastPos.x, owner.page.lastPos.x, timeStacker) +
                              menu.manager.rainWorld.screenSize.x / 2;
        modeSeparatorLeft.y = Mathf.Lerp(owner.page.lastPos.y, owner.page.lastPos.y, timeStacker) +
            modeButtons[0].pos.y - 40f;
        modeSeparatorRight.x = Mathf.Lerp(owner.page.lastPos.x, owner.page.lastPos.x, timeStacker) +
                               menu.manager.rainWorld.screenSize.x / 2;
        modeSeparatorRight.y = Mathf.Lerp(owner.page.lastPos.y, owner.page.lastPos.y, timeStacker) +
            modeButtons[0].pos.y - 40f;
    }

    private void SwitchMode(int newMode)
    {
        currentMode = newMode;

        switch (newMode)
        {
            case 0:
                stTab.pos.y = screenCenter.y - 1500f;
                apTab.pos.y = screenCenter.y;
                apTab.Enable();
                break;
            case 1:
                stTab.pos.y = screenCenter.y;
                apTab.pos.y = screenCenter.y - 1500f;
                apTab.Disable();
                break;
        }
    }
}