using System.Collections.Generic;
using JollyCoop;
using JollyCoop.JollyMenu;
using Menu;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public sealed class CampaignSelectPage : PositionedMenuObject
{
    // Elements
    private FSprite pageTitle;
    private SlotSelector slotSelector;
    private SimpleButton newGameButton;
    private SimpleButton toggleJollyButton;
    private MenuLabel jollyMainLabel;
    private List<MenuLabel> jollyPlayerSummaries;

    // Vars
    private readonly Vector2 pageTitlePos;
    private bool lastDialogOpen;

    public CampaignSelectPage(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        Vector2 screenSize = menu.manager.rainWorld.screenSize;

        // Title
        pageTitlePos = new Vector2(screenSize.x / 2, screenSize.y - 80f);
        pageTitle = new FSprite("illustrations/randomizerpage");
        pageTitle.SetAnchor(0.5f, 0f);
        pageTitle.x = pageTitlePos.x;
        pageTitle.y = pageTitlePos.y;
        pageTitle.shader = menu.manager.rainWorld.Shaders["MenuText"];
        Container.AddChild(pageTitle);

        slotSelector = new SlotSelector(menu, this, default);
        slotSelector.pos = new Vector2(screenSize.x / 2 - slotSelector.size.x / 2, 50f);
        subObjects.Add(slotSelector);

        newGameButton = new SimpleButton(menu, this, menu.Translate("NEW GAME"), "NEW_GAME",
            new Vector2(slotSelector.pos.x + slotSelector.size.x + 20f, slotSelector.pos.y),
            new Vector2(100f, 30f));
        subObjects.Add(newGameButton);

        // Jolly Menu
        if (ModManager.JollyCoop)
        {
            toggleJollyButton = new SimpleButton(menu, this, menu.Translate("SHOW"), "JOLLY_CONFIG",
                new Vector2(screenSize.x - 160f, screenSize.y - 100f),
                new Vector2(110f, 30f));
            subObjects.Add(toggleJollyButton);
            jollyMainLabel = new MenuLabel(menu, this, menu.Translate("JOLLY COOP"),
                toggleJollyButton.pos + new Vector2(-75f, toggleJollyButton.size.y / 2f),
                Vector2.zero, true);
            jollyMainLabel.pos.x = toggleJollyButton.pos.x - jollyMainLabel.label.textRect.width / 2f - 20f;
            subObjects.Add(jollyMainLabel);
            RefreshJollySummary();
        }

        // Set this so it can be propagated to the slot start buttons
        slotSelector.nextSelectable[2] = newGameButton;
        slotSelector.SetNavigation();
        newGameButton.nextSelectable[0] = GetFirstSelectable();
        newGameButton.nextSelectable[1] = GetFirstSelectable();
        newGameButton.nextSelectable[2] = newGameButton;
        newGameButton.nextSelectable[3] = newGameButton;
    }

    public MenuObject GetFirstSelectable()
    {
        return ((SlotSelector.Slot)slotSelector.GetEntryAtIndex(0))?.startButton ?? (MenuObject)newGameButton;
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        if (message.Equals("JOLLY_CONFIG"))
        {
            JollySetupDialog dialog = new(SlugcatStats.Name.White, menu.manager, toggleJollyButton.pos);
            menu.manager.ShowDialog(dialog);
            menu.manager.rainWorld.options.playersBeforeEnterJollyMenu =
                menu.manager.rainWorld.options.JollyPlayerCount;
            menu.manager.rainWorld.options.ResetJollyProfileRequest();
            menu.PlaySound(SoundID.MENU_Switch_Page_In);
        }
    }

    public override void Update()
    {
        base.Update();

        // Refresh Jolly Co-op slugcats whenever a dialog closes (mainly the Jolly one)
        if (menu.manager.IsRunningAnyDialog)
        {
            lastDialogOpen = true;
        }
        else if (lastDialogOpen)
        {
            RefreshJollySummary();
            lastDialogOpen = false;
            Plugin.Log.LogDebug("refresh jolly");
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        pageTitle.x = Mathf.Lerp(owner.page.lastPos.x, owner.page.pos.x, timeStacker) + pageTitlePos.x;
        pageTitle.y = Mathf.Lerp(owner.page.lastPos.y, owner.page.pos.y, timeStacker) + pageTitlePos.y;
    }

    public override void RemoveSprites()
    {
        base.RemoveSprites();
        pageTitle.RemoveFromContainer();
    }

    private void RefreshJollySummary()
    {
        if (!ModManager.JollyCoop) return;

        bool hasReset = false;
        jollyPlayerSummaries ??= [];
        if (jollyPlayerSummaries is null)
        {
            jollyPlayerSummaries = [];
            hasReset = true;
        }
        else if (jollyPlayerSummaries.Count != Custom.rainWorld.options.JollyPlayerCount)
        {
            foreach (MenuLabel label in jollyPlayerSummaries)
            {
                label.RemoveSprites();
                RemoveSubObject(label);
            }

            jollyPlayerSummaries.Clear();
            hasReset = true;
        }

        for (int i = 0; i < Custom.rainWorld.options.JollyPlayerCount; i++)
        {
            SlugcatStats.Name slugcat = JollyCustom.SlugClassMenu(i, null);
            string text = slugcat is null
                ? menu.Translate("DEFAULT")
                : menu.Translate($"The {SlugcatStats.getSlugcatName(slugcat)}");
            if (hasReset)
            {
                MenuLabel label = new MenuLabel(menu, this, text,
                    toggleJollyButton.pos - new Vector2(0f, 30f + 25f * i),
                    new Vector2(110f, 30f), false);
                jollyPlayerSummaries.Add(label);
                subObjects.Add(label);
            }
            else
            {
                jollyPlayerSummaries[i].text = text;
            }
        }
    }
}