using System;
using System.Collections.Generic;
using Menu;
using RWCustom;
using UnityEngine;
using RWMenu = Menu.Menu;

namespace RainWorldRandomizer.Menu;

    /// <summary>
    /// Base class for various implementations of a pause screen menu. This is really just a big scroll box
    /// </summary>
    public abstract class ScrollingMenu : RectangularMenuObject, Slider.ISliderOwner, SelectOneButton.SelectOneButtonOwner
    {
        protected float entryWidth = 0.9f;
        protected float entryHeight = 0.05f;

        protected RoundedRect roundedRect;
        protected LevelSelector.ScrollButton scrollUpButton;
        protected LevelSelector.ScrollButton scrollDownButton;
        protected VerticalSlider scrollSlider;

        protected List<Entry> entries = [];
        protected List<Entry> filteredEntries = [];

        protected float floatScrollPos;
        private float floatScrollVel;
        private float sliderValue;
        private float sliderValueCap;
        private bool sliderPulled;

        protected int ScrollPos { get; set; }

        private int MaxVisibleItems
        {
            get
            {
                return (int)(size.y / (entryHeight + 12f));
            }
        }

        protected int LastPossibleScroll
        {
            get
            {
                return Math.Max(0, filteredEntries.Count - (MaxVisibleItems - 1));
            }
        }

        public ScrollingMenu(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size) :
            base(menu, owner, pos, size)
        {
            menu.manager.menuMic = new MenuMicrophone(menu.manager, menu.manager.soundLoader);
            entryWidth *= size.x;
            entryHeight *= size.y;

            myContainer = new FContainer();
            owner.Container.AddChild(myContainer);

            // Bounding box
            roundedRect = new RoundedRect(menu, this, default, size, true)
            { fillAlpha = 1f };

            // Entries
            floatScrollPos = ScrollPos;

            // Scroll Buttons
            scrollUpButton = new LevelSelector.ScrollButton(menu, this, "UP", new Vector2(size.x / 2f - 12f, size.y + 2f), 0);
            scrollDownButton = new LevelSelector.ScrollButton(menu, this, "DOWN", new Vector2(size.x / 2f - 12f, -26f), 2);
            
            // Slider
            scrollSlider = new VerticalSlider(menu, this, "Slider", new Vector2(-30f, 0f), new Vector2(30f, size.y - 20f), RandomizerEnums.SliderId.SpoilerMenu, true);
            
            subObjects.Add(roundedRect);
            subObjects.Add(scrollUpButton);
            subObjects.Add(scrollDownButton);
            subObjects.Add(scrollSlider);
        }

        /// <summary>
        /// Populate the <see cref="entries"/> list with every entry we will ever display. Called on object creation
        /// </summary>
        protected abstract void PopulateEntries();
        /// <summary>
        /// Filter the entries based on some criteria. Use an enum to create valid filters and apply them here
        /// </summary>
        protected virtual void FilterEntries(int filter) { }
        public abstract int GetCurrentlySelectedOfSeries(string series);
        public abstract void SetCurrentlySelectedOfSeries(string series, int to);

        public override void Update()
        {
            base.Update();
            if (MouseOver && menu.manager.menuesMouseMode && menu.mouseScrollWheelMovement != 0)
            {
                AddScroll(menu.mouseScrollWheelMovement);
            }
            for (int i = 0; i < filteredEntries.Count; i++)
            {
                filteredEntries[i].pos.y = IdealYPosForItem(i);
            }
            scrollDownButton.buttonBehav.greyedOut = ScrollPos == LastPossibleScroll;
            scrollUpButton.buttonBehav.greyedOut = ScrollPos == 0;

            floatScrollPos = Custom.LerpAndTick(floatScrollPos, ScrollPos, 0.01f, 0.01f); // Move position towards fade away position
            floatScrollVel *= Custom.LerpMap(Math.Abs(ScrollPos - floatScrollPos), 0.25f, 1.5f, 0.45f, 0.99f); // Black magic???
            floatScrollVel += Mathf.Clamp(ScrollPos - floatScrollPos, -2.5f, 2.5f) / 2.5f * 0.15f; // Add velocity based on difference from fadePos
            floatScrollVel = Mathf.Clamp(floatScrollVel, -1.2f, 1.2f); // Clamp velocity
            floatScrollPos += floatScrollVel; // Move by velocity
            sliderValueCap = Custom.LerpAndTick(sliderValueCap, LastPossibleScroll, 0.02f, entries.Count / 40f); // Move max slider downwards

            // If there's no scrolling, disable slider and return
            if (LastPossibleScroll == 0)
            {
                sliderValue = Custom.LerpAndTick(sliderValue, 0.5f, 0.02f, 0.05f);
                scrollSlider.buttonBehav.greyedOut = true;
                return;
            }
            scrollSlider.buttonBehav.greyedOut = false;

            // If the slider was used, move it and return
            if (sliderPulled)
            {
                floatScrollPos = Mathf.Lerp(0f, sliderValueCap, sliderValue);
                ScrollPos = Custom.IntClamp(Mathf.RoundToInt(floatScrollPos), 0, LastPossibleScroll);
                sliderPulled = false;
                return;
            }
            sliderValue = Custom.LerpAndTick(sliderValue, Mathf.InverseLerp(0f, sliderValueCap, floatScrollPos), 0.02f, 0.05f);
        }

        public float ValueOfSlider(Slider slider)
        {
            return 1f - sliderValue;
        }

        public void SliderSetValue(Slider slider, float value)
        {
            sliderValue = 1f - value;
            sliderPulled = true;
        }

        private float StepsDownOfItem(int index)
        {
            return Mathf.Min(index, filteredEntries.Count - 1) + 1;
        }

        protected float IdealYPosForItem(int index)
        {
            return size.y - ((entryHeight + 10f) * (StepsDownOfItem(index) - floatScrollPos)) - 7f;
        }

        private void AddScroll(int scrollDir)
        {
            ScrollPos += scrollDir;
            ConstrainScroll();
        }

        private void ConstrainScroll()
        {
            if (ScrollPos > LastPossibleScroll)
            {
                ScrollPos = LastPossibleScroll;
            }
            if (ScrollPos < 0)
            {
                ScrollPos = 0;
            }
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            switch (message)
            {
                case "UP":
                    AddScroll(-1);
                    return;
                case "DOWN":
                    AddScroll(1);
                    return;
            }
        }

        public abstract class Entry(RWMenu menu, MenuObject owner, Vector2 pos, Vector2 size) : RectangularMenuObject(menu, owner, pos, size)
        {
            protected RoundedRect roundedRect;

            private bool active;
            protected bool sleep;
            protected float fade;
            protected float lastFade;
            private float selectedBlink;
            public float lastSelectedBlink;
            private bool lastSelected;

            /// <summary>
            /// Call this at the end of override constructor. If called before it gets covered by other elements
            /// </summary>
            protected void CreateBoundingBox()
            {
                roundedRect = new RoundedRect(menu, this, default, size, false)
                {
                    borderColor = RWMenu.MenuColor(RWMenu.MenuColors.MediumGrey)
                };
                subObjects.Add(roundedRect);
            }

            public override void Update()
            {
                base.Update();
                ScrollingMenu statusMenu = owner as ScrollingMenu;
                lastFade = fade;
                lastSelectedBlink = selectedBlink;

                if (Selected)
                {
                    if (!lastSelected)
                    {
                        selectedBlink = 1f;
                    }
                    selectedBlink = Mathf.Max(0f, selectedBlink - 1f / Mathf.Lerp(10f, 40f, selectedBlink));
                }
                else
                {
                    selectedBlink = 0f;
                }
                lastSelected = Selected;

                int myindex = -1;
                for (int i = 0; i < statusMenu.filteredEntries.Count; i++)
                {
                    if (statusMenu.filteredEntries[i] == this)
                    {
                        myindex = i;
                        break;
                    }
                }

                active = myindex >= statusMenu.floatScrollPos - 1f
                    && myindex < statusMenu.floatScrollPos + statusMenu.MaxVisibleItems + 1f;
                
                if (sleep)
                {
                    if (!active)
                    {
                        return;
                    }
                    sleep = false;
                }

                float value = statusMenu.StepsDownOfItem(myindex) - 1f;
                float fadeTowards = 1f;
                float difference = 0f;
                if (myindex < statusMenu.floatScrollPos)
                {
                    fadeTowards = Mathf.InverseLerp(statusMenu.floatScrollPos - 1f, statusMenu.floatScrollPos, value);
                    difference = Mathf.Abs(myindex - statusMenu.floatScrollPos);
                    //Mathf.Clamp01(value - statusMenu.floatScrollPos - 1f);
                    //
                    // 0
                }
                else if (myindex > statusMenu.floatScrollPos + statusMenu.MaxVisibleItems - 1)
                {
                    float sum = statusMenu.floatScrollPos + statusMenu.MaxVisibleItems;
                    fadeTowards = Mathf.InverseLerp(sum, sum - 1, value);
                    difference = Mathf.Abs(myindex - sum - 1);
                    //Mathf.Clamp01(sum - value);
                    //
                }

                fade = Mathf.Lerp(fade, fadeTowards, difference > 0.5f ? 1f : 0.5f);
                // fade = Mathf.Lerp(fade, fadeTowards, Mathf.InverseLerp(0.5f, 0.45f, 0.5f));

                if (fade == 0f && lastFade == 0f)
                {
                    sleep = true;
                    if (roundedRect != null)
                        foreach (FSprite sprite in roundedRect.sprites)
                            sprite.isVisible = false;
                }
            }

            public override void GrafUpdate(float timeStacker)
            {
                if (sleep) return;

                base.GrafUpdate(timeStacker);
                float smoothedFade = fade;// Mathf.Lerp(lastFade, fade, timeStacker);

                if (smoothedFade > 0f && roundedRect != null)
                {
                    foreach (var sprite in roundedRect.sprites)
                    {
                        sprite.alpha = smoothedFade;
                        sprite.isVisible = true;
                    }
                }
            }
        }
    }