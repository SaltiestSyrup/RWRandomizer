using Menu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class CurrentBuffsDisplay : RectangularMenuObject
    {
        public static bool AnyBuffsToDisplay
        {
            get
            {
                if (Plugin.RandoManager is not ManagerBase manager) return false;

                return manager.NumDamageUpgrades > 0 || manager.HasAnyExpeditionPerks();
            }
        }

        public RoundedRect roundedRect;
        public MenuLabel[] menuLabels;

        public CurrentBuffsDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
        {
            List<string> obtainedBuffs = [];
            if (Plugin.RandoManager.NumDamageUpgrades > 0)
            {
                obtainedBuffs.Add($"Spear Damage +{(Plugin.RandoManager.SpearDamageMultiplier - 1) * 100}%");
            }
            foreach (int p in Enum.GetValues(typeof(ManagerBase.ExpeditionPerks)))
            {
                ManagerBase.ExpeditionPerks perk = (ManagerBase.ExpeditionPerks)p;
                if (Plugin.RandoManager.HasExpeditionPerk(perk))
                    obtainedBuffs.Add(Unlock.readableItemNames.TryGetValue(perk.ToString(), out string val) ? val : perk.ToString());
            }
            menuLabels = new MenuLabel[obtainedBuffs.Count + 1];
            size = new Vector2(150f, (menuLabels.Length * 15f) + 20f);

            roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
            {
                fillAlpha = 1f
            };
            subObjects.Add(roundedRect);

            menuLabels[0] = new MenuLabel(menu, this, "Current Buffs:", new Vector2(10.01f, -13.01f), default, false, null);
            menuLabels[0].label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White);
            menuLabels[0].label.alignment = FLabelAlignment.Left;
            subObjects.Add(menuLabels[0]);

            for (int i = 1; i < menuLabels.Length; i++)
            {
                menuLabels[i] = new MenuLabel(menu, this, obtainedBuffs[i - 1],
                    new Vector2(10.01f, -15.01f - (15f * i)), default, false, null);
                menuLabels[i].label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                menuLabels[i].label.alignment = FLabelAlignment.Left;
                subObjects.Add(menuLabels[i]);
            }
        }
    }
}
