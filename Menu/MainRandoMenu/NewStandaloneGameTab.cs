using System;
using System.Linq;
using Menu;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class NewStandaloneGameTab : PositionedMenuObject
{
    // Determines the order and profile illustrations of each selectable slugcat
    private readonly (string, string, string)[] slugcatInfos =
    [
        ("Yellow", "illustrations", MenuHelpers.GetSlugcatPortrait(SlugcatStats.Name.Yellow)),
        ("White", "illustrations", MenuHelpers.GetSlugcatPortrait(SlugcatStats.Name.White)),
        ("Red", "illustrations", MenuHelpers.GetSlugcatPortrait(SlugcatStats.Name.Red)),
        ("Gourmand", "illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Gourmand"))),
        ("Artificer", "illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Artificer"))),
        ("Spear", "illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Spear"))),
        ("Rivulet", "illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Rivulet"))),
        ("Saint", "illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Saint"))),
        ("Watcher", "illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Watcher"))),
        ("Inv", "illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Inv"))),
    ];
    
    // Elements
    private PortraitButton[] slugcatButtons;
    
    // Vars
    private int currentSelection;
    
    public NewStandaloneGameTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
    {
        // grid of slugcat portrait buttons
        // pressing one opens their options page
        // can quit out or press a hold button to start the game with chosen options
        slugcatButtons = new PortraitButton[slugcatInfos.Length];
        for (int i = 0; i < slugcatInfos.Length; i++)
        {
            // The most magical numbers you've ever seen
            // (getting quite sick of programmatic UI)
            Vector2 buttonPos = i switch
            {
                // X = (Portrait & Margin Width * Index Within Row) - ((Portrait & Margin Width / 2) * (Num in row - 1)) - (Portrait Width / 2)
                // Y = Static Offset - (Portrait & Margin Width * Row)
                < 3 => new Vector2(110f * i - 55f * 2 - 47f, 120f),
                < 8 => new Vector2(110f * (i - 3) - 55f * 4 - 47f, 120f - 115f),
                _ => new Vector2(110f * (i - 8) - 55f - 47f, 120f - 115f * 2f)
            };
            slugcatButtons[i] = new PortraitButton(menu, this, $"SLUG-{slugcatInfos[i].Item1}", buttonPos, 
                slugcatInfos[i].Item2, slugcatInfos[i].Item3);
            subObjects.Add(slugcatButtons[i]);

            // Hardcoded selectables!!! Yay!!
            switch (i)
            {
                case 0:
                    slugcatButtons[i].nextSelectable[0] = slugcatButtons[i];
                    slugcatButtons[i].nextSelectable[1] = ((CreateNewGamePage)owner).modeButtons[0];
                    break;
                case 1 or 2:
                    slugcatButtons[i].nextSelectable[1] = ((CreateNewGamePage)owner).modeButtons[0];
                    break;
                case 3:
                    slugcatButtons[i].nextSelectable[1] = slugcatButtons[0];
                    break;
                case 4 or 5 or 6:
                    slugcatButtons[i].nextSelectable[1] = slugcatButtons[i - 4];
                    slugcatButtons[i - 4].nextSelectable[3] = slugcatButtons[i];
                    break;
                case 7:
                    slugcatButtons[i].nextSelectable[1] = slugcatButtons[2];
                    break;
                case 8:
                    slugcatButtons[i].nextSelectable[1] = slugcatButtons[4];
                    slugcatButtons[i].nextSelectable[3] = slugcatButtons[i];
                    slugcatButtons[3].nextSelectable[3] = slugcatButtons[i];
                    slugcatButtons[4].nextSelectable[3] = slugcatButtons[i];
                    slugcatButtons[5].nextSelectable[3] = slugcatButtons[i];
                    break;
                case 9:
                    slugcatButtons[i].nextSelectable[1] = slugcatButtons[5];
                    slugcatButtons[i].nextSelectable[2] = slugcatButtons[i];
                    slugcatButtons[i].nextSelectable[3] = slugcatButtons[i];
                    slugcatButtons[6].nextSelectable[3] = slugcatButtons[i];
                    slugcatButtons[7].nextSelectable[3] = slugcatButtons[i];
                    break;
            }

            if (i > 0)
            {
                slugcatButtons[i].nextSelectable[0] = slugcatButtons[i - 1];
                slugcatButtons[i - 1].nextSelectable[2] = slugcatButtons[i];
            }
        }

        // Watcher disabled until implemented
        slugcatButtons[8].GetButtonBehavior.greyedOut = true;
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);

        if (message.StartsWith("SLUG-")
            && ExtEnumBase.TryParse(typeof(SlugcatStats.Name), message.Substring(5), false, out ExtEnumBase slugcat))
        {
            ((CreateNewGamePage)owner).chosenSlugcat = (SlugcatStats.Name)slugcat;
            SaveFile file = new SaveFile
            {
                slugcat = slugcat.value,
                isDownpourDLC = ModManager.MSC,
                isWatcherDLC = ModManager.Watcher
            };
            ((RandomizerMenu)menu).optionsDialog = new OptionsDialog(menu.manager,
                OptionsDialog.Mode.StandaloneNew, file,
                () =>
                {
                    ((RandomizerMenu)menu).optionsDialog.OutputToSaveFile(ref file);
                    RandoOptions.LoadedOptions = file.options;
                    try { Singal(this, "START_NEW_GAME"); }
                    catch (Exception e) { Plugin.Log.LogError(e); }
                });
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            menu.manager.ShowDialog(((RandomizerMenu)menu).optionsDialog);
        }
    }

    private class PortraitButton : SimpleButton
    {
        private MenuIllustration portrait;
        
        public PortraitButton(RWMenu menu, MenuObject owner, string signalText, Vector2 pos, 
            string folderName, string fileName) 
            : base(menu, owner, "", signalText, pos, new Vector2(94f, 94f))
        {
            portrait = new MenuIllustration(menu, this, folderName, fileName, new Vector2(5f, 5f), true, false);
            subObjects.Add(portrait);
        }

        public override void Update()
        {
            base.Update();

            portrait.color = buttonBehav.greyedOut ? new Color(0.2f, 0.2f, 0.2f) : Color.white;
        }
    }
}