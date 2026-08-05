using System;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

public class OptionsDialog : Dialog, SelectOneButton.SelectOneButtonOwner
{
    // Tab radio buttons
    // Main Rect
    // Body for each tab
    // Elements
    // private RoundedRect roundedRect;
    private SelectOneButton[] tabButtons;
    private PositionedMenuObject[] tabs;
    private SimpleButton exitButton;
    
    // Vars
    public bool editMode;
    private int currentTab;
    public OptionStruct options;
    
    public OptionsDialog(ProcessManager manager) : base(manager)
    {
        Vector2 centerScreen = manager.rainWorld.screenSize / 2f;
        size = new Vector2(800f, 500f);

        darkSprite.alpha = 0.8f;
        
        
        tabButtons = new SelectOneButton[3];
        
        tabButtons[0] = new SelectOneButton(this, pages[0], "CHECKS", "OPTAB-CHECKS",
            centerScreen + new Vector2(size.x * -0.25f - 50f, size.y / 2f - 6f), new Vector2(100f, 30f),
            tabButtons, 0)
        {
            fadeAlpha = 4f
        };
        tabButtons[1] = new SelectOneButton(this, pages[0], "ITEMS", "OPTAB-ITEMS",
            centerScreen + new Vector2(-50f, size.y / 2f - 6f), new Vector2(100f, 30f),
            tabButtons, 1)
        {
            fadeAlpha = 4f
        };
        tabButtons[2] = new SelectOneButton(this, pages[0], "BEHAVIORS", "OPTAB-BEHAVIORS",
            centerScreen + new Vector2(size.x * 0.25f - 50f, size.y / 2f - 6f), new Vector2(100f, 30f),
            tabButtons, 2)
        {
            fadeAlpha = 4f
        };
        // Insert to start of subobjects to be drawn in the back
        pages[0].subObjects.InsertRange(0, tabButtons);
        
        roundedRect = new RoundedRect(this, pages[0], centerScreen - size / 2f, size, true)
        {
            fillAlpha = 1f
        };
        pages[0].subObjects.Insert(3, roundedRect);
        

        exitButton = new SimpleButton(this, pages[0], "DONE", "CLOSE_OPTIONS",
            roundedRect.pos + new Vector2(size.x - 105f, 5f),
            new Vector2(100f, 30f));
        pages[0].subObjects.Add(exitButton);
        
        tabs = new PositionedMenuObject[3];

        // pages.Add(new Page(this, null, "CHECKS", 1));
        tabs[0] = new ChecksTab(this, pages[0], roundedRect.pos);
        pages[0].subObjects.Add(tabs[0]);
        
        // pages.Add(new Page(this, null, "ITEMS", 2));
        tabs[1] = new ItemsTab(this, pages[0], roundedRect.pos - new Vector2(0f, 2000f));
        pages[0].subObjects.Add(tabs[1]);
        
        tabs[2] = new BehaviorsTab(this, pages[0], roundedRect.pos - new Vector2(0f, 2000f));
        pages[0].subObjects.Add(tabs[2]);
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "CLOSE_OPTIONS":
                manager.StopSideProcess(this);
                break;
        }
    }

    private void UpdatePage(int newPage)
    {
        // InitPagelessObjects(newPage);
        currentTab = newPage;
        for (int i = 0; i < tabs.Length; i++)
        {
            tabs[i].pos = roundedRect.pos + (i == newPage ? 0f : -1f) * new Vector2(0f, 2000f);
            tabs[i].lastPos = tabs[i].pos;
        }
    }
    
    public int GetCurrentlySelectedOfSeries(string series)
    {
        return series.StartsWith("OPTAB-") ? currentTab : 0;
    }

    public void SetCurrentlySelectedOfSeries(string series, int to)
    {
        if (series.StartsWith("OPTAB-") && currentTab != to)
        {
            UpdatePage(to);
        }
    }

    private class ChecksTab : PositionedMenuObject
    {
        public ChecksTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            Vector2 topLeft = new Vector2(0f, ((Dialog)menu).size.y);
            float runningY = ((Dialog)menu).size.y - 40f;

            CheckBoxOption checkBoxOption = new CheckBoxOption(menu, this, new Vector2(20f, runningY),
                RandoOptions.useSandboxTokenChecks);
            subObjects.Add(checkBoxOption);
            // MenuLabel tokenChecksLabel = new MenuLabel(menu, this, "Sandbox Tokens",
            //     new Vector2(20f, runningY -= 20f), default, false);
            // subObjects.Add(tokenChecksLabel);
        }
    }

    private class ItemsTab : PositionedMenuObject
    {
        public ItemsTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            float runningY = ((Dialog)menu).size.y - 40f;
            
            CheckBoxOption checkBoxOption = new CheckBoxOption(menu, this, new Vector2(20f, runningY),
                RandoOptions.givePassageUnlocks);
            subObjects.Add(checkBoxOption);
        }
    }

    private class BehaviorsTab : PositionedMenuObject
    {
        public BehaviorsTab(RWMenu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos)
        {
            float runningY = ((Dialog)menu).size.y - 40f;
            
            CheckBoxOption checkBoxOption = new CheckBoxOption(menu, this, new Vector2(20f, runningY),
                RandoOptions.randomizeSpawnLocation);
            subObjects.Add(checkBoxOption);
        }
    }

    private class CheckBoxOption : PositionedMenuObject
    {
        // Wrappers
        private MenuTabWrapper tabWrapper;
        private UIelementWrapper labelWrapper;
        private UIelementWrapper checkBoxWrapper;
        
        // Elements
        private OpLabel label;
        private OpCheckBox checkBox;
        
        public CheckBoxOption(RWMenu menu, MenuObject owner, Vector2 pos, Configurable<bool> config) : base(menu, owner, pos)
        {
            tabWrapper = new MenuTabWrapper(menu, this);
            
            checkBox = new OpCheckBox(config, new Vector2(300f, -3f))
            {
                description = config.info.description
            };
            checkBoxWrapper = new UIelementWrapper(tabWrapper, checkBox);
            
            label = new OpLabel(default, default, config.info.Tags[0] as string, FLabelAlignment.Left, true)
            {
                bumpBehav = checkBox.bumpBehav,
                description = checkBox.description
            };
            labelWrapper = new UIelementWrapper(tabWrapper, label);
            
            subObjects.Add(tabWrapper);
            
        }
    }
}