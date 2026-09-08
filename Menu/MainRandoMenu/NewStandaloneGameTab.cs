using System.Linq;
using Menu;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class NewStandaloneGameTab : PositionedMenuObject, SelectOneButton.SelectOneButtonOwner
{
    // Determines the order and profile illustrations of each selectable slugcat
    private readonly (string, string)[] slugcatInfos =
    [
        ("illustrations", MenuHelpers.GetSlugcatPortrait(SlugcatStats.Name.Yellow)),
        ("illustrations", MenuHelpers.GetSlugcatPortrait(SlugcatStats.Name.White)),
        ("illustrations", MenuHelpers.GetSlugcatPortrait(SlugcatStats.Name.Red)),
        ("illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Gourmand"))),
        ("illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Artificer"))),
        ("illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Spear"))),
        ("illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Rivulet"))),
        ("illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Saint"))),
        ("illustrations", MenuHelpers.GetSlugcatPortrait(new SlugcatStats.Name("Watcher"))),
        ("content", ModManager.MSC ? "sm1" : "multiplayerportrait02"),
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
                < 3 => new Vector2(110f * i - 55f * 2 - 47f, 150f),
                < 8 => new Vector2(110f * (i - 3) - 55f * 4 - 47f, 150f - 115f),
                _ => new Vector2(110f * (i - 8) - 55f - 47f, 150f - 115f * 2f)
            };
            slugcatButtons[i] = new PortraitButton(menu, this, $"SLUG-{i}", buttonPos, 
                slugcatButtons.ToArray<SelectOneButton>(), i, slugcatInfos[i].Item1, slugcatInfos[i].Item2);
            subObjects.Add(slugcatButtons[i]);
        }
    }

    public int GetCurrentlySelectedOfSeries(string series)
    {
        return series.StartsWith("SLUG-") ? currentSelection : 0;
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        if (series.StartsWith("SLUG-") && currentSelection != to)
        {
            currentSelection = to;
            for (int i = 0; i < slugcatButtons.Length; i++)
            {
                slugcatButtons[i].UpdateSelected(to == i);
            }
        }
    }
    
    private class PortraitButton : SelectOneButton
    {
        private MenuIllustration portrait;
        
        public PortraitButton(RWMenu menu, MenuObject owner, string signalText, Vector2 pos, 
            SelectOneButton[] buttonArray, int buttonArrayIndex, string folderName, string fileName) 
            : base(menu, owner, "", signalText, pos, new Vector2(94f, 94f), buttonArray, buttonArrayIndex)
        {
            portrait = new MenuIllustration(menu, this, folderName, fileName, new Vector2(5f, 5f), true, false)
            {
                sprite = { scale = folderName == "content" ? 0.2f : 1f }
            };
            subObjects.Add(portrait);
        }

        public void UpdateSelected(bool selected)
        {
            bool greyedOut = buttonBehav.greyedOut;
            if (selected)
            {
                portrait.color = Color.white;
            }
            else
            {
                portrait.color = greyedOut ? new Color(0.1f, 0.1f, 0.1f) : new Color(0.25f, 0.25f, 0.25f);
            }

            portrait.alpha = greyedOut ? 0.7f : 1f;
        }
    }
}