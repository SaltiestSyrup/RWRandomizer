using System.Collections.Generic;
using System.Linq;
using Menu;
using RWMenu = Menu.Menu;
using UnityEngine;

namespace RainWorldRandomizer.Menu;

/// <summary>
/// Simple list display of all found gates
/// </summary>
public class GatesDisplay : RectangularMenuObject
{
    public RoundedRect roundedRect;
    public MenuLabel[] menuLabels;

    public GatesDisplay(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
    {
        List<string> openedGates = [.. Plugin.RandoManager.GetGatesStatus().Where(g => g.Value).Select(g => g.Key)];
        menuLabels = new MenuLabel[openedGates.Count + 1];
        size = new Vector2(300f, (menuLabels.Length * 15f) + 20f);

        roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
        {
            fillAlpha = 1f
        };
        subObjects.Add(roundedRect);

        menuLabels[0] = new MenuLabel(menu, this, "Currently Unlocked Gates:", new Vector2(10.01f, -13.01f), default, false, null);
        menuLabels[0].label.color = RWMenu.MenuRGB(RWMenu.MenuColors.White);
        menuLabels[0].label.alignment = FLabelAlignment.Left;
        subObjects.Add(menuLabels[0]);

        for (int i = 1; i < menuLabels.Length; i++)
        {
            menuLabels[i] = new MenuLabel(menu, this,
                Plugin.GateToString(openedGates[i - 1], Plugin.RandoManager.currentSlugcat),
                new Vector2(10.01f, -15.01f - (15f * i)), default, false, null);
            menuLabels[i].label.color = RWMenu.MenuRGB(RWMenu.MenuColors.MediumGrey);
            menuLabels[i].label.alignment = FLabelAlignment.Left;
            subObjects.Add(menuLabels[i]);
        }
    }
}