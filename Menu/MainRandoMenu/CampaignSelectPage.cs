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

    // Vars
    private readonly Vector2 pageTitlePos;

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
}