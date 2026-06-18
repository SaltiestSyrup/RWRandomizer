using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

/// <summary>
/// Button used on the <see cref="SleepAndDeathScreen"/> to teleport the player to the start,
/// both for convenience and preventing softlocks.
/// Do not use outside of <see cref="SleepAndDeathScreen"/> (why would you) (it explodes)
/// </summary>
public class PassageHomeButton : PositionedMenuObject
{
    private MenuTabWrapper tabWrapper;
    public OpHoldButton button;
    private UIelementWrapper elementWrapper;
    
    public PassageHomeButton(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        tabWrapper = new MenuTabWrapper(menu, this);
        subObjects.Add(tabWrapper);
        
        button = new OpHoldButton(
            Vector2.zero, new Vector2(110f, 30f), "RETURN HOME", 40f)
        {
            description = "Fast travel to the shelter you started the campaign in",
        };
        button.OnPressDone += (_) => Singal(this, "RETURN_HOME");
        
        elementWrapper = new UIelementWrapper(tabWrapper, button);
    }

    public override void Update()
    {
        base.Update();
        button.greyedOut = ((SleepAndDeathScreen)menu).ButtonsGreyedOut || ((SleepAndDeathScreen)menu).goalMalnourished;
    }
}