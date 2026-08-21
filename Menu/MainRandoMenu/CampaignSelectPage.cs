using Menu;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public sealed class CampaignSelectPage : PositionedMenuObject
{
    // Elements
    private FSprite pageTitle;
    private SlotSelector slotSelector;
    private SimpleButton startButton;
    
    // Vars
    private readonly Vector2 pageTitlePos;
    
    public CampaignSelectPage(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        // Title
        pageTitlePos = new Vector2(menu.manager.rainWorld.screenSize.x / 2, menu.manager.rainWorld.screenSize.y - 80f);
        pageTitle = new FSprite("illustrations/randomizerpage");
        pageTitle.SetAnchor(0.5f, 0f);
        pageTitle.x = pageTitlePos.x;
        pageTitle.y = pageTitlePos.y;
        pageTitle.shader = menu.manager.rainWorld.Shaders["MenuText"];
        Container.AddChild(pageTitle);
        
        slotSelector = new SlotSelector(menu, this, 
            new Vector2(menu.manager.rainWorld.options.ScreenSize.x / 4f, 50f));
        subObjects.Add(slotSelector);

        startButton = new SimpleButton(menu, this, menu.Translate("NEW GAME"), "NEW_GAME",
            new Vector2(slotSelector.pos.x + slotSelector.size.x + 20f, slotSelector.pos.y),
            new Vector2(100f, 30f));
        subObjects.Add(startButton);
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