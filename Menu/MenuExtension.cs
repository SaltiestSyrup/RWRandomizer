using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RainWorldRandomizer
{
    public class MenuExtension
    {
        public bool hasSeenSpoilers = false;

        public bool displaySpoilerMenu = false;
        public SimpleButton spoilerButton;
        public SpoilerMenu spoilerMenu;
        public PendingItemsDisplay pendingItemsDisplay;

        public void ApplyHooks()
        {
            On.Menu.PauseMenu.ctor += OnMenuCtor;
            On.Menu.PauseMenu.Singal += OnMenuSignal;
            On.Menu.PauseMenu.ShutDownProcess += OnMenuShutdownProcess;
            On.Menu.PauseMenu.SpawnExitContinueButtons += OnSpawnExitContinueButtons;
            On.Menu.PauseMenu.SpawnConfirmButtons += OnSpawnConfirmButtons;
        }

        public void RemoveHooks()
        {
            On.Menu.PauseMenu.ctor -= OnMenuCtor;
            On.Menu.PauseMenu.Singal -= OnMenuSignal;
            On.Menu.PauseMenu.ShutDownProcess -= OnMenuShutdownProcess;
            On.Menu.PauseMenu.SpawnExitContinueButtons -= OnSpawnExitContinueButtons;
            On.Menu.PauseMenu.SpawnConfirmButtons -= OnSpawnConfirmButtons;
        }

        public void OnMenuCtor(On.Menu.PauseMenu.orig_ctor orig, PauseMenu self, ProcessManager manager, RainWorldGame game)
        {
            //menu = self;
            orig(self, manager, game);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            // Extra offset if using Warp Menu
            float xOffset = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("LeeMoriya.Warp") ? 190f : 20f;

            RectangularMenuObject gateDisplay;

            if (RandoOptions.useGateMap.Value && (Plugin.RandoManager is ManagerArchipelago || !Plugin.AnyThirdPartyRegions))
            {
                gateDisplay = new GateMapDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - 320f));
            }
            else
            {
                gateDisplay = new GatesDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - 20f));
            }

            self.pages[0].subObjects.Add(gateDisplay);

            if (RandoOptions.GiveObjectItems && Plugin.Singleton.itemDeliveryQueue.Count > 0)
            {
                pendingItemsDisplay = new PendingItemsDisplay(self, self.pages[0],
                    new Vector2((1366f - manager.rainWorld.screenSize.x) / 2f + xOffset, manager.rainWorld.screenSize.y - gateDisplay.size.y - 20f));
                self.pages[0].subObjects.Add(pendingItemsDisplay);
            }
        }

        public void OnMenuShutdownProcess(On.Menu.PauseMenu.orig_ShutDownProcess orig, PauseMenu self)
        {
            displaySpoilerMenu = false;
            orig(self);
        }

        public void OnSpawnExitContinueButtons(On.Menu.PauseMenu.orig_SpawnExitContinueButtons orig, PauseMenu self)
        {
            orig(self);
            if (!Plugin.RandoManager.isRandomizerActive || Plugin.RandoManager is ManagerArchipelago) return;

            spoilerButton = new SimpleButton(self, self.pages[0], self.Translate("SHOW SPOILERS"), "SHOW_SPOILERS",
                new Vector2(self.ContinueAndExitButtonsXPos - 460.2f - self.moveLeft, 15f),
                new Vector2(110f, 30f));

            self.pages[0].subObjects.Add(spoilerButton);
            spoilerButton.nextSelectable[1] = spoilerButton;
            spoilerButton.nextSelectable[3] = spoilerButton;
        }

        public void OnSpawnConfirmButtons(On.Menu.PauseMenu.orig_SpawnConfirmButtons orig, PauseMenu self)
        {
            orig(self);
            if (spoilerButton != null)
            {
                spoilerButton.RemoveSprites();
                self.pages[0].RemoveSubObject(spoilerButton);
            }
            spoilerButton = null;
        }

        public void SpawnConfirmButtonsForSpoilers(PauseMenu self)
        {
            if (self.continueButton != null)
            {
                self.continueButton.RemoveSprites();
                self.pages[0].RemoveSubObject(self.continueButton);
            }
            self.continueButton = null;
            if (self.exitButton != null)
            {
                self.exitButton.RemoveSprites();
                self.pages[0].RemoveSubObject(self.exitButton);
            }
            if (spoilerButton != null)
            {
                spoilerButton.RemoveSprites();
                self.pages[0].RemoveSubObject(spoilerButton);
            }
            spoilerButton = null;

            self.confirmYesButton = new SimpleButton(self, self.pages[0], self.Translate("YES"), "YES_SPOILERS",
                new Vector2(self.ContinueAndExitButtonsXPos - 180.2f - self.moveLeft, 15f),
                new Vector2(110f, 30f));
            self.confirmNoButton = new SimpleButton(self, self.pages[0], self.Translate("NO"), "NO_SPOILERS",
                new Vector2(self.ContinueAndExitButtonsXPos - 320.2f - self.moveLeft, 15f),
                new Vector2(110f, 30f));
            self.confirmMessage = new MenuLabel(self, self.pages[0], self.Translate("Are you sure you want to spoil the seed for this run?"),
                self.confirmNoButton.pos, new Vector2(10f, 30f), false, null);
            self.confirmMessage.label.alignment = FLabelAlignment.Left;
            self.confirmMessage.pos = new Vector2(self.confirmMessage.pos.x - self.confirmMessage.label.textRect.width - 40f, self.confirmMessage.pos.y);

            self.pages[0].subObjects.Add(self.confirmYesButton);
            self.pages[0].subObjects.Add(self.confirmNoButton);
            self.pages[0].subObjects.Add(self.confirmMessage);

            self.confirmYesButton.nextSelectable[1] = self.confirmYesButton;
            self.confirmYesButton.nextSelectable[3] = self.confirmYesButton;
            self.confirmNoButton.nextSelectable[1] = self.confirmNoButton;
            self.confirmNoButton.nextSelectable[3] = self.confirmNoButton;
        }

        public void OnMenuSignal(On.Menu.PauseMenu.orig_Singal orig, PauseMenu self, MenuObject sender, string message)
        {
            orig(self, sender, message);
            if (!Plugin.RandoManager.isRandomizerActive) return;

            if (message != null)
            {
                if (message == "SHOW_SPOILERS")
                {
                    if (hasSeenSpoilers)
                    {
                        ToggleSpoilerMenu(self);
                    }
                    else
                    {
                        SpawnConfirmButtonsForSpoilers(self);
                    }
                    self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }

                if (message == "YES_SPOILERS")
                {
                    ToggleSpoilerMenu(self);
                    hasSeenSpoilers = true;
                    self.SpawnExitContinueButtons();
                    self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }

                if (message == "NO_SPOILERS")
                {
                    self.SpawnExitContinueButtons();
                    self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
            }
        }

        public void ToggleSpoilerMenu(PauseMenu self)
        {
            displaySpoilerMenu = !displaySpoilerMenu;
            if (displaySpoilerMenu)
            {
                spoilerMenu = new SpoilerMenu(self, self.pages[0]);
                self.pages[0].subObjects.Add(spoilerMenu);
            }
            else
            {
                if (spoilerMenu != null)
                {
                    spoilerMenu.RemoveSprites();
                    self.pages[0].RemoveSubObject(spoilerMenu);
                }
                spoilerMenu = null;
            }
        }

        public class GatesDisplay : RectangularMenuObject
        {
            public RoundedRect roundedRect;
            public MenuLabel[] menuLabels;

            public GatesDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
            {
                List<string> openedGates = Plugin.RandoManager.GetGatesStatus().Where(g => g.Value == true).ToList().ConvertAll(g => g.Key);
                menuLabels = new MenuLabel[openedGates.Count + 1];
                size = new Vector2(300f, (menuLabels.Length * 15f) + 20f);

                roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
                {
                    fillAlpha = 1f
                };
                subObjects.Add(roundedRect);

                menuLabels[0] = new MenuLabel(menu, this, "Currently Unlocked Gates:", new Vector2(10f, -13f), default, false, null);
                menuLabels[0].label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White);
                menuLabels[0].label.alignment = FLabelAlignment.Left;
                subObjects.Add(menuLabels[0]);

                for (int i = 1; i < menuLabels.Length; i++)
                {
                    menuLabels[i] = new MenuLabel(menu, this,
                        Plugin.GateToString(openedGates[i - 1], Plugin.RandoManager.currentSlugcat),
                        new Vector2(10f, -15f - (15f * i)), default, false, null);
                    menuLabels[i].label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
                    menuLabels[i].label.alignment = FLabelAlignment.Left;
                    subObjects.Add(menuLabels[i]);
                }
            }
        }

        public class GateMapDisplay : RoundedRect
        {
            public Dictionary<string, Node> nodes = new Dictionary<string, Node>();
            public Dictionary<string, Connector> connectors = new Dictionary<string, Connector>();
            public IEnumerable<LocationInfo> locationInfos;
            public Node highlightedNode;
            public static string Scug => Plugin.RandoManager.currentSlugcat?.value ?? "White";
            public static string CurrentRegion => (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.world.name;
            public static Color COLOR_ACCESSIBLE = Color.white;
            public static Color COLOR_INACCESSIBLE = new Color(0.2f, 0.2f, 0.2f);
            public static Dictionary<string, string> regionCodeLookup = Plugin.RegionNamesMap.ToDictionary(x => x.Value, x => x.Key);

            public GateMapDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default, true)
            {
                size = new Vector2(320f, 310f);
                fillAlpha = 1f;

                // Nodes have to exist before the connectors, but we want the connectors to be behind the nodes.
                CreateNodes();
                CreateConnectors();
                ParseLocationStatus();
                foreach (Connector connector in connectors.Values) Container.AddChild(connector);
                subObjects.AddRange(nodes.Values);

                Dictionary<string, bool> gates = Plugin.RandoManager.GetGatesStatus();

                foreach (var pair in gates)
                {
                    if (connectors.TryGetValue(pair.Key, out Connector connector))
                    {
                        connector.Color = pair.Value ? COLOR_ACCESSIBLE : COLOR_INACCESSIBLE;
                    }
                }

                foreach (string nodeName in GetAccessibleNodes(gates.Where(x => x.Value).Select(x => x.Key)))
                {
                    if (nodes.TryGetValue(nodeName, out Node node)) node.Accessible = true;
                }

                checkIconContainer = new CheckIconContainer();
                Container.AddChild(checkIconContainer);
                checkIconContainer.SetPosition(pos);

                UpdateTrackerForRegion(CurrentRegion);
            }

            public void ParseLocationStatus()
            {
                if (Plugin.RandoManager is ManagerArchipelago manager)
                {
                    locationInfos = manager.locationsStatus.Select(x => new LocationInfo(x));
                    foreach (KeyValuePair<string, Node> pair in nodes)
                    {
                        IEnumerable<LocationInfo> nodeInfos = locationInfos.Where(x => x.node == pair.Key);
                        pair.Value.completion = nodeInfos.Count(x => x.collected) / (float)nodeInfos.Count();
                    }
                }
            }

            public readonly struct LocationInfo
            {
                public readonly LocationKind kind;
                public readonly string name;
                public readonly string region;
                public readonly string node;
                public readonly bool collected;

                public LocationInfo(string location, bool collected)
                {
                    kind = KindOfLocation(location);
                    region = RegionOfLocation(kind, location);
                    node = GetNodeName(region);
                    name = location;
                    this.collected = collected;
                }

                public LocationInfo(KeyValuePair<string, bool> pair) : this(pair.Key, pair.Value) { }

                public static string RegionOfLocation(LocationKind kind, string location)
                {
                    switch (kind)
                    {
                        case LocationKind.BlueToken:
                        case LocationKind.RedToken:
                        case LocationKind.GreenToken:
                        case LocationKind.Broadcast:
                        case LocationKind.Pearl:
                            return location.Split('-')[2];

                        case LocationKind.Echo:
                            return location.Split('-')[1];

                        case LocationKind.GoldToken:
                            string third = location.Split('-')[2];
                            switch (third)
                            {
                                case "GWold": return "GW";
                                case "gutter": return "SB";
                                default: return third;
                            }

                        case LocationKind.Shelter:
                            return location.Substring(8, 2);

                        case LocationKind.FoodQuest:
                            return "<FQ>";
                        case LocationKind.Passage:
                            return "<P>";

                        default:
                            switch (location)
                            {
                                case "Eat_Neuron": return "<P>";
                                case "Meet_LttM_Spear": return "DM";
                                case "Kill_FP": return "RM";
                                case "Gift_Neuron":
                                case "Meet_LttM":
                                case "Save_LttM":
                                case "Ascend_LttM":
                                    return "SL";
                                case "Meet_FP":
                                case "Ascend_FP":
                                    return "SS";
                                default: return null;
                            }
                    }
                }

                public static LocationKind KindOfLocation(string location)
                {
                    if (location.StartsWith("Pearl-")) return LocationKind.Pearl;
                    if (location.StartsWith("Shelter-")) return LocationKind.Shelter;
                    if (location.StartsWith("Broadcast-")) return LocationKind.Broadcast;
                    if (location.StartsWith("Echo-")) return LocationKind.Echo;
                    if (location.StartsWith("Token-L-")) return LocationKind.GoldToken;
                    if (location.StartsWith("Token-S-")) return LocationKind.RedToken;
                    if (location.StartsWith("Token-")) return LocationKind.BlueToken;
                    if (location.StartsWith("Passage-")) return LocationKind.Passage;
                    if (location.StartsWith("Wanderer-")) return LocationKind.WandererPip;
                    if (location.StartsWith("FoodQuest-")) return LocationKind.FoodQuest;
                    return LocationKind.Other;
                }
            }

            public void CreateNodes()
            {
                // Nodes which are always present, regardless of gamestate.
                nodes["SB"] = new Node(menu, this, new Vector2(40f, 40f), "SB");
                nodes["DS"] = new Node(menu, this, new Vector2(100f, 40f), Scug == "Saint" ? "UG" : "DS");
                nodes["LF"] = new Node(menu, this, new Vector2(40f, 80f), "LF");
                nodes["SU"] = new Node(menu, this, new Vector2(100f, 80f), "SU");
                nodes["GW"] = new Node(menu, this, new Vector2(160f, 80f), "GW");
                nodes["HI"] = new Node(menu, this, new Vector2(100f, 120f), "HI");
                nodes["SH"] = new Node(menu, this, new Vector2(160f, 120f), Scug == "Saint" ? "CL" : "SH");
                nodes["SL"] = new Node(menu, this, new Vector2(220f, 120f), Scug == "Artificer" || Scug == "Spear" ? "LM" : "SL");
                nodes["SI"] = new Node(menu, this, new Vector2(40f, 160f), "SI");
                nodes["CC"] = new Node(menu, this, new Vector2(100f, 160f), "CC");
                nodes["<P>"] = new Node(menu, this, new Vector2(280f, 40f), "P");

                if (ModManager.MSC)
                {
                    nodes["VS"] = new Node(menu, this, new Vector2(40f, 120f), "VS");
                    if (Scug == "White" || Scug == "Yellow" || Scug == "Gourmand") nodes["OE"] = new Node(menu, this, new Vector2(70f, 60f), "OE");
                    if (Scug == "Artificer") nodes["LC"] = new Node(menu, this, new Vector2(160f, 200f), "LC");
                    if (Scug != "Artificer") nodes["MS"] = new Node(menu, this, new Vector2(280f, 160f), Scug == "Spear" ? "DM" : "MS");
                    nodes["<FQ>"] = new Node(menu, this, new Vector2(280f, 80f), "FQ");
                }

                if (Scug != "Saint")
                {
                    nodes["UW"] = new Node(menu, this, new Vector2(160f, 160f), "UW");
                    nodes["SS"] = new Node(menu, this, new Vector2(220f, 160f), Scug == "Rivulet" ? "RM" : "SS");
                }
            }

            public void CreateConnectors()
            {
                // Vanilla connections that always exist (except SI_LF).
                connectors["GATE_SU_DS"] = new Connector(nodes["SU"].Bottom, nodes["DS"].Top);
                connectors["GATE_SU_HI"] = new Connector(nodes["HI"].Bottom, nodes["SU"].Top);
                connectors["GATE_LF_SU"] = new Connector(nodes["LF"].Right, nodes["SU"].Left);
                connectors["GATE_DS_GW"] = new Connector(false, nodes["DS"].Right, 45, 30);
                connectors["GATE_DS_SB"] = new Connector(nodes["SB"].Right, nodes["DS"].Left);
                connectors["GATE_GW_SL"] = new Connector(nodes["GW"].TopRight, nodes["SL"].BottomLeft);
                connectors["GATE_HI_CC"] = new Connector(nodes["CC"].Bottom, nodes["HI"].Top);
                connectors["GATE_HI_GW"] = new Connector(nodes["HI"].BottomRight, nodes["GW"].TopLeft);
                connectors["GATE_HI_SH"] = new Connector(nodes["HI"].Right, nodes["SH"].Left);
                connectors["GATE_LF_SB"] = new Connector(nodes["LF"].Bottom, nodes["SB"].Top);
                connectors["GATE_SB_SL"] = new Connector(true, nodes["SB"].Bottom, -10, 180, 90);
                connectors["GATE_SH_SL"] = new Connector(nodes["SH"].Right, nodes["SL"].Left);
                connectors["GATE_SI_CC"] = new Connector(nodes["SI"].Right, nodes["CC"].Left);


                if (Scug != "Saint")
                {
                    connectors["GATE_CC_UW"] = new Connector(nodes["CC"].Right, nodes["UW"].Left);
                    connectors["GATE_SH_UW"] = new Connector(nodes["UW"].Bottom, nodes["SH"].Top);
                    connectors["GATE_SS_UW"] = new Connector(nodes["UW"].TopRight, nodes["SS"].TopLeft);
                    connectors["GATE_UW_SS"] = new Connector(nodes["UW"].BottomRight, nodes["SS"].BottomLeft);
                }

                if (ModManager.MSC)
                {
                    connectors["GATE_HI_VS"] = new Connector(nodes["VS"].Right, nodes["HI"].Left);
                    connectors["GATE_SI_VS"] = new Connector(nodes["SI"].Bottom, nodes["VS"].Top);
                    connectors["GATE_GW_SH"] = new Connector(nodes["SH"].Bottom, nodes["GW"].Top);
                    connectors["GATE_SI_LF"] = new Connector(false, nodes["SI"].Left, -10, -57, 0, float.NaN, -5, 0, -18, 10);
                    connectors["GATE_SB_VS"] = new Connector(true, nodes["VS"].Bottom, -10, -25, -60, 10);

                    connectors["GATE_SI_LF"] = Connector.Broken(
                        nodes["SI"].BottomRight,
                        new Vector2(20f, 0f), new Vector2(0f, -31f), default, new Vector2(0f, -5f), new Vector2(0f, -32f), new Vector2(-20f, 0f));

                    connectors["GATE_DS_CC"] = Connector.Wrapping(nodes["DS"].Bottom, new Vector2(0f, -30f), nodes["CC"].Top, new Vector2(0f, 30f));
                    connectors["GATE_SL_VS"] = Connector.Wrapping(nodes["VS"].Left, new Vector2(-30f, 0f), nodes["SL"].Right, new Vector2(30f, 0f));

                    if (Scug == "Artificer") connectors["GATE_UW_LC"] = new Connector(nodes["UW"].Top, nodes["LC"].Bottom);
                    if (Scug == "Spear")
                    {
                        connectors["GATE_DM_SL"] = new Connector(nodes["SL"].TopRight, nodes["MS"].Bottom);
                        connectors["GATE_SL_DM"] = new Connector(nodes["SL"].Top, nodes["MS"].BottomLeft);
                    }
                    if (Scug != "Artificer" && Scug != "Spear")
                    {
                        connectors["GATE_MS_SL"] = new Connector(nodes["SL"].TopRight, nodes["MS"].Bottom);
                        connectors["GATE_SL_MS"] = new Connector(nodes["SL"].Top, nodes["MS"].BottomLeft);
                    }
                    if (Scug != "Saint")
                    {
                        connectors["GATE_UW_SL"] = new Connector(nodes["SL"].TopLeft, nodes["UW"].BottomRight);
                    }
                    if (Scug == "White" || Scug == "Yellow" || Scug == "Gourmand")
                    {
                        connectors["GATE_OE_SU"] = new Connector(true, nodes["SU"].Bottom + new Vector2(-5f, 0f), -10, -10);
                        connectors["GATE_SB_OE"] = new Connector(true, nodes["SB"].Top + new Vector2(5f, 0f), 10, 10);
                    }
                }
                else
                {
                    // Without MSC, VS is not in the way of the SI_LF connection.
                    connectors["GATE_SI_LF"] = new Connector(nodes["SI"].Bottom, nodes["LF"].Top);
                }
            }

            public static string ActualStartRegion
            {
                get
                {
                    // `customStartDen` currently does not reliably reflect an AP start; grab AP directly
                    string s = Plugin.RandoManager is ManagerArchipelago ? ArchipelagoConnection.desiredStartDen : Plugin.RandoManager.customStartDen;
                    // Special case for Spearmaster
                    return s == "GATE_OE_SU" ? "SU" : s.Split('_')[0];
                }
            }

            /// <summary>
            /// Determine whether a particular gate is usable in either direction.
            /// </summary>
            /// <param name="key">The gate name ("GATE_A_B").</param>
            /// <returns>Two <see cref="bool"/>s - one for whether going from region A to region B is possible, and one for B to A.
            /// Note that which region is A and which is B is dependent only on the gate name, not on the physical position of the regions.</returns>
            public static bool[] CanUseGate(string key)
            {
                switch (key)
                {
                    case "GATE_LF_SB": return Scug == "Saint" ? new bool[] { true, true } : new bool[] { true, false };
                    case "GATE_SL_MS": return new bool[] { false, true };
                    case "GATE_OE_SU": return new bool[] { true, false };
                    case "GATE_UW_SL": return Scug == "Artificer" || Scug == "Spear" ? new bool[] { true, true } : new bool[] { false, false };
                    case "GATE_SL_VS": return Scug == "Artificer" ? new bool[] { false, false } : new bool[] { true, true };
                }
                return new bool[] { true, true };
            }

            /// <summary>
            /// Given a list of currently held gate keys, determinine which region nodes are accessible.
            /// </summary>
            public static IEnumerable<string> GetAccessibleNodes(IEnumerable<string> keys)
            {
                List<string> ret = new List<string>() { GetNodeName(ActualStartRegion), "<FQ>", "<P>" };
                Dictionary<string, bool[]> keyDict = keys.ToDictionary(x => x, CanUseGate);
                bool updated = true;
                while (updated)
                {
                    updated = false;
                    foreach (var pair in keyDict)
                    {
                        string[] split = pair.Key.Split('_');
                        string left = GetNodeName(split[1]);
                        string right = GetNodeName(split[2]);
                        bool[] usable = pair.Value;
                        if (usable[0] && ret.Contains(left) && !ret.Contains(right)) { ret.Add(right); updated = true; }
                        else if (usable[1] && !ret.Contains(left) && ret.Contains(right)) { ret.Add(left); updated = true; }
                    }
                }
                return ret;
            }

            /// <summary>
            /// Get the name of the node associated with a given region code.
            /// </summary>
            public static string GetNodeName(string code)
            {
                switch (code)
                {
                    case "LM": return "SL";
                    case "RM": return "SS";
                    case "UG": return "DS";
                    case "DM": return "MS";
                    case "CL": return "SH";
                    default: return code;
                }
            }

            /// <summary>Get the code of the region actually represented by a given node.  null if the region does not exist in the current gamestate.</summary>
            public static string GetActualRegion(string node)
            {
                string scug = Scug;
                switch (node)
                {
                    case "SL": return scug == "Artificer" || scug == "Spear" ? "LM" : "SL";
                    case "SS": return scug == "Rivulet" ? "RM" : (scug == "Saint" ? null : "SS");
                    case "DS": return scug == "Saint" ? "UG" : "DS";
                    case "MS": return scug == "Spear" ? "DM" : "MS";
                    case "SH": return scug == "Saint" ? "CL" : "SH";
                    case "UW": return scug == "Saint" ? null : "UW";
                    default: return node;
                }
            }

            public class Node : RoundedRect
            {
                public MenuLabel label;
                public RoundedRect outer;
                public static Vector2 SIZE = new Vector2(30f, 20f);
                public float completion;
                public bool current;

                public Node(Menu.Menu menu, MenuObject owner, Vector2 pos, string text, float completion = 0f)
                    : base(menu, owner, pos, SIZE, true)
                {
                    fillAlpha = 1f;
                    label = new MenuLabel(menu, owner, text, pos + (SIZE / 2) + new Vector2(0.01f, 0.01f), default, false);
                    label.label.alignment = FLabelAlignment.Center;
                    subObjects.Add(label);
                    outer = new RoundedRect(menu, owner, pos - new Vector2(4f, 4f), size + new Vector2(8f, 8f), false);
                    subObjects.Add(outer);
                    this.completion = completion;

                    label.label.color = COLOR_INACCESSIBLE;
                    Vector3 c = Custom.RGB2HSL(COLOR_INACCESSIBLE);
                    borderColor = new HSLColor(c.x, c.y, c.z);
                }

                public Vector2 Bottom => pos + new Vector2(size.x / 2, 1f) + (owner as GateMapDisplay).pos;
                public Vector2 Top => pos + new Vector2(size.x / 2, size.y + 1f) + (owner as GateMapDisplay).pos;
                public Vector2 Left => pos + new Vector2(1f, size.y / 2) + (owner as GateMapDisplay).pos;
                public Vector2 Right => pos + new Vector2(size.x, size.y / 2) + (owner as GateMapDisplay).pos;
                public Vector2 BottomLeft => pos + new Vector2(4f, 4f) + (owner as GateMapDisplay).pos;
                public Vector2 BottomRight => pos + new Vector2(size.x - 4f, 4f) + (owner as GateMapDisplay).pos;
                public Vector2 TopRight => pos + new Vector2(size.x - 4f, size.y - 4f) + (owner as GateMapDisplay).pos;
                public Vector2 TopLeft => pos + new Vector2(4f, size.y - 4f) + (owner as GateMapDisplay).pos;

                public bool Accessible
                {
                    set
                    {
                        label.label.color = value ? COLOR_ACCESSIBLE : COLOR_INACCESSIBLE;
                    }
                }

                public string ReadableName
                {
                    get
                    {
                        string text = label.label.text;
                        if (Plugin.RegionNamesMap.TryGetValue(text, out string s)) return s;
                        switch (label.label.text)
                        {
                            case "<FQ>": return "Food Quest";
                            case "<P>": return "Passages";
                            default: return "Unknown region";
                        }
                    }
                }

                public override void GrafUpdate(float timeStacker)
                {
                    base.GrafUpdate(timeStacker);
                    for (int i = 0; i < 8; i++)
                    {
                        if (completion > i / 7f || completion >= 1f)
                        {
                            sprites[i % 2 == 0 ? CornerSprite(i / 2) : SideSprite(i / 2)].color = Color.green;
                        }
                    }
                    foreach (FSprite sprite in outer.sprites) sprite.isVisible = current;
                }
            }

            public class Connector : FContainer
            {
                /// <summary>
                /// Create a Connector from a list of vector vertices.
                /// </summary>
                public Connector(params Vector2[] vertices)
                {
                    for (int i = 1; i < vertices.Length; i++)
                    {
                        AddChild(new Segment(vertices[i - 1], vertices[i]));
                    }
                }

                /// <summary>
                /// Create an connector from a start point and a list of vector offsets, optionally with gaps.
                /// </summary>
                /// <param name="start">The point to start at.</param>
                /// <param name="offsets">The list of vectors used to define the remaining points, each acting as an offset from the last point.
                /// If <see cref="Vector2.zero"/> is given, the next step is not actually drawn, resulting in a gap.</param>
                /// <returns>The constructed <see cref="Connector"/>.</returns>
                public static Connector Broken(Vector2 start, params Vector2[] offsets)
                {
                    Vector2 point = start;
                    bool discardNext = false;
                    List<Segment> segments = new List<Segment>();
                    foreach (Vector2 offset in offsets)
                    {
                        if (offset == Vector2.zero) { discardNext = true; continue; }
                        Vector2 nextPoint = point + offset;
                        if (!discardNext) segments.Add(new Segment(point, nextPoint));
                        point = nextPoint;
                        discardNext = false;
                    }
                    return new Connector(segments);
                }

                /// <summary>
                /// Create an orthogonal angled connector from a start point and a list of steps, optionally with gaps.
                /// </summary>
                /// <param name="verticalFirst">Whether the first step is vertical instead of horizontal.</param>
                /// <param name="start">The start point.</param>
                /// <param name="offsets">The list of steps to take.  The orientation of each step is orthogonal to the last step.
                /// If <see cref="float.NaN"/> is given, the next step is not drawn, resulting in a gap (and the orientation does not change).
                /// If 0 (zero) is given, the orientation changes without drawing a new segment.</param>
                public Connector(bool verticalFirst, Vector2 start, params float[] offsets)
                {
                    Vector2 point = start;
                    bool verticalNext = verticalFirst;
                    bool discardNext = false;
                    foreach (float offset in offsets)
                    {
                        if (float.IsNaN(offset)) { discardNext = true; continue; }
                        if (offset == 0) { verticalNext = !verticalNext; continue; }
                        Vector2 nextPoint = point + (verticalNext ? new Vector2(0f, offset) : new Vector2(offset, 0f));
                        if (!discardNext) AddChild(new Segment(point, nextPoint));
                        point = nextPoint;
                        verticalNext = !verticalNext;
                        discardNext = false;
                    }
                }

                public Connector(List<Segment> segments) { foreach (Segment segment in segments) AddChild(segment); }

                public static Connector Wrapping(Vector2 startA, Vector2 offsetA, Vector2 startB, Vector2 offsetB, float dashLength = 7f, float dashSpace = 5f)
                {
                    List<Segment> segments = new List<Segment>();
                    for (int i = 0; i < 3; i++)
                    {
                        segments.Add(new Segment(
                            startA + offsetA.normalized * (dashLength + dashSpace) * i,
                            startA + offsetA.normalized * ((dashLength + dashSpace) * i + dashLength)
                            ));
                        segments.Add(new Segment(
                            startB + offsetB.normalized * (dashLength + dashSpace) * i,
                            startB + offsetB.normalized * ((dashLength + dashSpace) * i + dashLength)
                            ));
                    }
                    return new Connector(segments);
                }

                public Color Color
                {
                    set { foreach (Segment segment in _childNodes.OfType<Segment>()) segment.color = value; }
                    get => _childNodes.OfType<Segment>().FirstOrDefault()?.color ?? COLOR_INACCESSIBLE;
                }

                public class Segment : FSprite
                {
                    public Segment(Vector2 start, Vector2 end) : base("pixel")
                    {
                        Vector2 midpoint = (start + end) / 2;
                        SetPosition(midpoint);
                        var displacement = end - start;
                        color = COLOR_INACCESSIBLE;
                        if (displacement.x == 0) { width = 1; height = displacement.y; }
                        else if (displacement.y == 0) { width = displacement.x; height = 1; }
                        else
                        {
                            rotation = Custom.AimFromOneVectorToAnother(start, end);
                            width = 1; height = displacement.magnitude;
                        }
                    }
                }
            }

            public override void Update()
            {
                base.Update();
                if (MouseOver
                    && nodes.FirstOrDefault(x => x.Value.MouseOver) is KeyValuePair<string, Node> pair
                    && pair.Key is string region && pair.Value is Node node
                    && region != displayedRegion)
                {
                    UpdateTrackerForRegion(GetActualRegion(region));
                    checkIconContainer.regionLabel.text = node.ReadableName;
                }
            }

            public CheckIconContainer checkIconContainer;
            public string displayedRegion;

            public void UpdateTrackerForRegion(string region)
            {
                if (!(Plugin.RandoManager is ManagerArchipelago)) return;
                if (!nodes.TryGetValue(GetNodeName(region), out Node node)) return;

                if (highlightedNode != null) highlightedNode.current = false;
                highlightedNode = node;
                highlightedNode.current = true;
                displayedRegion = region;

                checkIconContainer.RemoveAllChildren();
                foreach (LocationInfo info in locationInfos.Where(x => x.region == region))
                    checkIconContainer.AddIcon(info.kind, info.name, info.collected);
                checkIconContainer.Refresh();
                //Plugin.Log.LogDebug($"Updating for region {region}: {string.Join(", ", locs)}");
                //Plugin.Log.LogDebug($"All locations: {string.Join(", ", Plugin.RandoManager.GetLocations())}");
            }

            public enum LocationKind { BlueToken, RedToken, GoldToken, GreenToken, Broadcast, Pearl, Echo, Shelter, Passage, WandererPip, FoodQuest, Other }

            public class CheckIconContainer : FContainer
            {
                public FLabel regionLabel;

                public CheckIconContainer()
                {
                    regionLabel = new FLabel(Custom.GetFont(), "") { x = 150.01f, y = 30.01f, alignment = FLabelAlignment.Center };
                    AddChild(regionLabel);
                }

                public void AddIcon(LocationKind kind, string name, bool is_checked)
                {
                    AddChild(CheckIcon.New(kind, name, is_checked));
                }

                public void Refresh()
                {
                    Vector2 pos = new Vector2(20f, 280f);
                    foreach (CheckIcon sprite in _childNodes.OfType<CheckIcon>())
                    {
                        sprite.SetPosition(pos + sprite.Adjustment);
                        pos += new Vector2(sprite.width + 5f, 0f);
                        if (pos.x > 300f) pos = new Vector2(20f, pos.y - 35f);
                    }
                }

                public class CheckIcon : FSprite
                {
                    public Vector2 Adjustment
                    {
                        get
                        {
                            return kind == LocationKind.Pearl ? new Vector2(-6f, 0f) : default;
                        }
                    }
                    public LocationKind kind;
                    public string name;

                    public CheckIcon(string element, LocationKind kind, string name) : base(element)
                    {
                        this.kind = kind;
                        this.name = name;
                    }
                    public static CheckIcon New(LocationKind kind, string name, bool is_checked)
                    {
                        string element = "Futile_White";
                        float scale = 1f;
                        Color color = Futile.white;

                        IconSymbol.IconSymbolData iconData;
                        switch (kind)
                        {
                            case LocationKind.Passage:
                                element = name.Substring(8) + "A";
                                if (name == "Gourmand")
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                                    element = CreatureSymbol.SpriteNameOfCreature(iconData);
                                    color = PlayerGraphics.DefaultSlugcatColor(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
                                }
                                break;
                            case LocationKind.Echo:
                                element = "smallKarma9-9";
                                scale = 0.5f;
                                color = RainWorld.SaturatedGold;
                                break;
                            case LocationKind.Pearl:
                                element = "Symbol_Pearl";
                                DataPearl.AbstractDataPearl.DataPearlType pearl = new DataPearl.AbstractDataPearl.DataPearlType(name);
                                color = DataPearl.UniquePearlMainColor(pearl);
                                Color? highlight = DataPearl.UniquePearlHighLightColor(pearl);
                                if (highlight != null)
                                {
                                    color = Custom.Screen(color, highlight.Value * Custom.QuickSaturation(highlight.Value) * 0.5f);
                                }
                                break;
                            case LocationKind.BlueToken:
                                element = "ctOn";
                                scale = 2f;
                                color = RainWorld.AntiGold.rgb;
                                break;
                            case LocationKind.GoldToken:
                                element = "ctOn";
                                scale = 2f;
                                color = new Color(1f, 0.6f, 0.05f);
                                break;
                            case LocationKind.RedToken:
                                element = "ctOn";
                                scale = 2f;
                                color = CollectToken.RedColor.rgb;
                                break;
                            case LocationKind.Broadcast:
                                element = "ctOn";
                                scale = 2f;
                                color = CollectToken.WhiteColor.rgb;
                                break;
                            case LocationKind.FoodQuest:
                                string objtype = name.Substring(10);
                                if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(objtype))
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(objtype), 0);
                                    element = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                                    color = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                                }
                                else if (ExtEnumBase.GetNames(typeof(CreatureTemplate.Type)).Contains(objtype))
                                {
                                    iconData = new IconSymbol.IconSymbolData(new CreatureTemplate.Type(objtype), AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                                    element = CreatureSymbol.SpriteNameOfCreature(iconData);
                                    color = CreatureSymbol.ColorOfCreature(iconData);
                                }
                                break;
                            case LocationKind.Shelter:
                                element = "ShelterMarker";
                                break;
                            case LocationKind.Other:
                                if (name == "Eat_Neuron" || name == "Gift_Neuron")
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer, 0);
                                    element = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                                    color = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                                }
                                else if (ModManager.MSC && name == "Kill_FP") { element = "GuidanceEnergyCell"; }
                                else if (ModManager.MSC) { element = "GuidancePebbles"; }
                                break;
                            default:
                                element = "EndGameCircle";
                                scale = 0.5f;
                                break;
                        }

                        CheckIcon ret = new CheckIcon(element, kind, name)
                        {
                            color = is_checked ? color : new Color(0.2f, 0.2f, 0.2f),
                            scale = scale
                        };
                        return ret;
                    }
                }
            }
        }

        public class PendingItemsDisplay : RectangularMenuObject
        {
            public RoundedRect roundedRect;
            public MenuLabel label;
            public FSprite[] sprites;

            public PendingItemsDisplay(Menu.Menu menu, MenuObject owner, Vector2 pos) : base(menu, owner, pos, default)
            {
                Unlock.Item[] pendingItems = Plugin.Singleton.itemDeliveryQueue.ToArray();
                sprites = new FSprite[pendingItems.Length];
                size = new Vector2(250f, ((pendingItems.Length - 1) / 8 * 30f) + 57f);

                myContainer = new FContainer();
                owner.Container.AddChild(myContainer);

                roundedRect = new RoundedRect(menu, this, new Vector2(0f, -size.y), size, true)
                {
                    fillAlpha = 1f
                };
                subObjects.Add(roundedRect);

                label = new MenuLabel(menu, this, "Pending items:", new Vector2(10f, -13f), default, false, null);
                label.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.White);
                label.label.alignment = FLabelAlignment.Left;
                subObjects.Add(label);

                for (int i = 0; i < pendingItems.Length; i++)
                {
                    sprites[i] = ItemToFSprite(pendingItems[i]);
                    Container.AddChild(sprites[i]);
                }
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);

                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i].isVisible = true;
                    sprites[i].x = DrawX(timeStacker) + (30f * (i % 8)) + 20f;
                    sprites[i].y = DrawY(timeStacker) - (30f * Mathf.FloorToInt(i / 8)) - 35f;
                    sprites[i].alpha = 1f;
                }
            }

            public FSprite ItemToFSprite(Unlock.Item item)
            {
                string spriteName = "Futile_White";
                float spriteScale = 1f;
                Color spriteColor = Futile.white;

                IconSymbol.IconSymbolData iconData;

                if (item.id == "KarmaFlower")
                {
                    spriteName = "FlowerMarker";
                    spriteColor = RainWorld.GoldRGB;
                }
                else
                {
                    if (item.id == "FireSpear" || item.id == "ExplosiveSpear")
                    {
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 1);
                    }
                    else if (item.id == "ElectricSpear")
                    {
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 2);
                    }
                    else if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(item.type.value))
                    {
                        iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(item.type.value), 0);
                    }
                    else
                    {
                        iconData = new IconSymbol.IconSymbolData();
                    }

                    spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                    spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                }

                try
                {
                    return new FSprite(spriteName, true)
                    {
                        scale = spriteScale,
                        color = spriteColor,
                    };
                }
                catch
                {
                    Plugin.Log.LogError($"Failed to load sprite '{spriteName}'");
                    return new FSprite("Futile_White", true);
                }
            }
        }

        public class SpoilerMenu : RectangularMenuObject, Slider.ISliderOwner, SelectOneButton.SelectOneButtonOwner
        {
            private readonly float entryWidth = 0.9f;
            private readonly float entryHeight = 0.05f;

            public RoundedRect roundedRect;
            public LevelSelector.ScrollButton scrollUpButton;
            public LevelSelector.ScrollButton scrollDownButton;
            public VerticalSlider scrollSlider;

            public RoundedRect filterSelectRect;
            public SelectOneButton[] filterSelectOptions;

            public List<SpoilerEntry> entries = new List<SpoilerEntry>();
            public List<SpoilerEntry> filteredEntries = new List<SpoilerEntry>();
            public EntryFilterType currentFilter = EntryFilterType.Given;

            public float floatScrollPos;
            public float floatScrollVel;
            private float sliderValue;
            private float sliderValueCap;
            private bool sliderPulled;

            public enum EntryFilterType
            {
                None,
                Given,
                NotGiven,
            }

            public int ScrollPos { get; set; }
            public int MaxVisibleItems
            {
                get
                {
                    return (int)(size.y / (entryHeight + 12f));
                }
            }
            public int LastPossibleScroll
            {
                get
                {
                    return Math.Max(0, filteredEntries.Count - (MaxVisibleItems - 1));
                }
            }

            public SpoilerMenu(Menu.Menu menu, MenuObject owner) : base(menu, owner, new Vector2(menu.manager.rainWorld.screenSize.x * 0.35f, menu.manager.rainWorld.screenSize.y * 0.125f + 60f), default)
            {
                menu.manager.menuMic = new MenuMicrophone(menu.manager, menu.manager.soundLoader);

                size = menu.manager.rainWorld.screenSize * new Vector2(0.3f, 0.75f);
                entryWidth *= size.x;
                entryHeight *= size.y;

                myContainer = new FContainer();
                owner.Container.AddChild(myContainer);

                // Bounding box
                roundedRect = new RoundedRect(menu, this, default, size, true)
                {
                    fillAlpha = 0.9f
                };
                subObjects.Add(roundedRect);

                // Entries
                floatScrollPos = ScrollPos;
                int i = 1;
                foreach (string loc in Plugin.RandoManager.GetLocations())
                {
                    entries.Add(new SpoilerEntry(menu, this,
                        new Vector2((size.x - entryWidth) / 2f, IdealYPosForItem(i - 1)),
                        new Vector2(entryWidth, entryHeight),
                        loc));
                    subObjects.Add(entries[i - 1]);

                    i += 1;
                }

                // Scroll Buttons
                scrollUpButton = new LevelSelector.ScrollButton(menu, this, "UP", new Vector2(size.x / 2f - 12f, size.y + 2f), 0);
                scrollDownButton = new LevelSelector.ScrollButton(menu, this, "DOWN", new Vector2(size.x / 2f - 12f, -26f), 2);
                subObjects.Add(scrollUpButton);
                subObjects.Add(scrollDownButton);

                // Slider
                scrollSlider = new VerticalSlider(menu, this, "Slider", new Vector2(-30f, 0f), new Vector2(30f, size.y - 20f), RandomizerEnums.SliderId.SpoilerMenu, true);
                subObjects.Add(scrollSlider);

                // Filter Menu
                filterSelectRect = new RoundedRect(menu, this, new Vector2(0f, -78f), new Vector2(size.x, 50f), true);

                filterSelectOptions = new SelectOneButton[3];
                filterSelectOptions[0] = new SelectOneButton(menu, this, menu.Translate("SHOW ALL"), "FILTER",
                    new Vector2(size.x / 28, filterSelectRect.pos.y + 10f),
                    new Vector2(2f * size.x / 7, filterSelectRect.size.y - 20f),
                    filterSelectOptions, 0);
                filterSelectOptions[1] = new SelectOneButton(menu, this, menu.Translate("SHOW COMPLETE"), "FILTER",
                    new Vector2(10 * size.x / 28, filterSelectRect.pos.y + 10f),
                    new Vector2(2f * size.x / 7, filterSelectRect.size.y - 20f),
                    filterSelectOptions, 1);
                filterSelectOptions[2] = new SelectOneButton(menu, this, menu.Translate("SHOW INCOMPLETE"), "FILTER",
                    new Vector2(19 * size.x / 28, filterSelectRect.pos.y + 10f),
                    new Vector2(2f * size.x / 7, filterSelectRect.size.y - 20f),
                    filterSelectOptions, 2);
                subObjects.AddRange(filterSelectOptions);
                FilterEntries(EntryFilterType.Given);
            }

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

            public void FilterEntries(EntryFilterType filter)
            {
                Func<SpoilerEntry, bool> predicate;

                switch (filter)
                {
                    case EntryFilterType.Given:
                        predicate = (e) =>
                        {
                            return (bool)Plugin.RandoManager.IsLocationGiven(e.entryKey);
                        };
                        break;
                    case EntryFilterType.NotGiven:
                        predicate = (e) =>
                        {
                            return !(bool)Plugin.RandoManager.IsLocationGiven(e.entryKey);
                        };
                        break;
                    default:
                        predicate = (e) =>
                        {
                            return true;
                        };
                        break;
                }
                filteredEntries = entries.Where(predicate).ToList();
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

            public float StepsDownOfItem(int index)
            {
                float val = Mathf.Min(index, filteredEntries.Count - 1) + 1;
                for (int i = 0; i <= Mathf.Min(index, filteredEntries.Count - 1); i++)
                {
                    val += 1f;
                }
                return Mathf.Min(index, filteredEntries.Count - 1) + 1;
            }

            public float IdealYPosForItem(int index)
            {
                return size.y - ((entryHeight + 10f) * (StepsDownOfItem(index) - floatScrollPos)) - 7f;
            }

            public void AddScroll(int scrollDir)
            {
                ScrollPos += scrollDir;
                ConstrainScroll();
            }

            public void ConstrainScroll()
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
                if (message != null)
                {
                    if (message == "UP")
                    {
                        AddScroll(-1);
                        return;
                    }
                    if (message == "DOWN")
                    {
                        AddScroll(1);
                        return;
                    }
                }
            }

            public int GetCurrentlySelectedOfSeries(string series)
            {
                if (series == null || series != "FILTER")
                {
                    return 0;
                }
                return (int)currentFilter;
            }

            public void SetCurrentlySelectedOfSeries(string series, int to)
            {
                if (series != null && series == "FILTER")
                {
                    currentFilter = (EntryFilterType)to;
                    FilterEntries(currentFilter);
                }
            }

            public class SpoilerEntry : RectangularMenuObject
            {
                public readonly string entryKey;
                public readonly string checkType;
                public readonly string checkName;

                public RoundedRect roundedRect;
                public FSprite arrow;
                public FSprite checkSprite;
                public FSprite unlockSprite;
                public MenuLabel checkLabel;
                public MenuLabel unlockLabel;

                public OpHoldButton holdButton;
                public MenuTabWrapper tabWrapper;
                public UIelementWrapper holdButtonWrapper;

                // Render variables
                public bool active;
                public bool sleep;
                public float fade;
                public float lastFade;
                public float selectedBlink;
                public float lastSelectedBlink;
                public bool lastSelected;

                public SpoilerEntry(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, string entryKey) : base(menu, owner, pos, size)
                {
                    this.entryKey = entryKey;
                    string[] split = Regex.Split(entryKey, "-");
                    if (split.Length > 1)
                    {
                        checkType = split.Length == 3 ? split[0] + "-" + split[1] : split[0];
                        checkName = split.Length == 3 ? split[2] : split[1];
                    }
                    else
                    {
                        checkType = "Misc";
                        checkName = entryKey;
                    }

                    // Button
                    tabWrapper = new MenuTabWrapper(menu, this);
                    subObjects.Add(tabWrapper);

                    holdButton = new OpHoldButton(default, size, " ", 40f)
                    {
                        description = entryKey
                    };
                    holdButton.OnPressDone += OnPressDone;

                    holdButtonWrapper = new UIelementWrapper(tabWrapper, holdButton);

                    // Bounding box
                    roundedRect = new RoundedRect(menu, this, default, size, true)
                    {
                        fillAlpha = 0.0f,
                        borderColor = (bool)Plugin.RandoManager.IsLocationGiven(entryKey) ? CollectToken.GreenColor : Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey)
                    };
                    subObjects.Add(roundedRect);

                    // Sprites
                    arrow = new FSprite("Big_Menu_Arrow", true)
                    {
                        scale = 0.5f,
                        rotation = 90f
                    };
                    Container.AddChild(arrow);

                    checkSprite = CheckToFSprite(checkType, checkName);
                    Container.AddChild(checkSprite);

                    unlockSprite = UnlockToFSprite(Plugin.RandoManager.GetUnlockAtLocation(entryKey));
                    Container.AddChild(unlockSprite);

                    // Labels
                    if (checkType != "FreeCheck")
                    {
                        checkLabel = new MenuLabel(menu, this, checkName,
                        new Vector2(0f, 5f),
                        new Vector2(size.x / 2, 20f), false, null);
                        subObjects.Add(checkLabel);
                    }

                    unlockLabel = new MenuLabel(menu, this, Plugin.RandoManager.GetUnlockAtLocation(entryKey).ToString(),
                        new Vector2(size.x / 2, 5f),
                        new Vector2(size.x / 2, 20f), false, null);

                    subObjects.Add(unlockLabel);
                }

                public override void Update()
                {
                    base.Update();
                    lastFade = fade;
                    lastSelectedBlink = selectedBlink;

                    roundedRect.borderColor = (bool)Plugin.RandoManager.IsLocationGiven(entryKey) ? CollectToken.GreenColor : Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey);
                    holdButton.greyedOut = (bool)Plugin.RandoManager.IsLocationGiven(entryKey);

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
                    for (int i = 0; i < (owner as SpoilerMenu).filteredEntries.Count; i++)
                    {
                        if ((owner as SpoilerMenu).filteredEntries[i] == this)
                        {
                            myindex = i;
                            break;
                        }
                    }

                    active = myindex >= (owner as SpoilerMenu).ScrollPos
                        && myindex < (owner as SpoilerMenu).ScrollPos + (owner as SpoilerMenu).MaxVisibleItems;

                    if (sleep)
                    {
                        if (!active)
                        {
                            return;
                        }
                        sleep = false;
                    }

                    float value = ((owner as SpoilerMenu).StepsDownOfItem(myindex) - 1f);
                    float fadeTowards = 1f;
                    if (myindex < (owner as SpoilerMenu).floatScrollPos)
                    {
                        fadeTowards = Mathf.InverseLerp((owner as SpoilerMenu).floatScrollPos - 1f, (owner as SpoilerMenu).floatScrollPos, value);
                    }
                    else if (myindex > (owner as SpoilerMenu).floatScrollPos + (owner as SpoilerMenu).MaxVisibleItems - 1)
                    {
                        float sum = (owner as SpoilerMenu).floatScrollPos + (owner as SpoilerMenu).MaxVisibleItems;
                        fadeTowards = Mathf.InverseLerp(sum, sum - 1, value);
                    }

                    fade = Custom.LerpAndTick(fade, fadeTowards, 0.08f, 0.1f);
                    fade = Mathf.Lerp(fade, fadeTowards, Mathf.InverseLerp(0.5f, 0.45f, 0.5f));

                    if (fade == 0f && lastFade == 0f)
                    {
                        sleep = true;
                        // Disable sprites
                        holdButton.Hide();
                        for (int i = 0; i < 17; i++)
                        {
                            roundedRect.sprites[i].isVisible = false;
                        }
                    }
                }

                public override void GrafUpdate(float timeStacker)
                {
                    if (sleep) return;

                    checkSprite.isVisible = true;
                    unlockSprite.isVisible = true;
                    base.GrafUpdate(timeStacker);
                    float smoothedFade = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.3f);

                    arrow.x = DrawX(timeStacker) + DrawSize(timeStacker).x / 2f;
                    arrow.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;
                    checkSprite.x = DrawX(timeStacker) + 20f;
                    checkSprite.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;
                    unlockSprite.x = DrawX(timeStacker) + DrawSize(timeStacker).x - 20f;
                    unlockSprite.y = DrawY(timeStacker) + DrawSize(timeStacker).y / 2f;

                    float alpha = Mathf.Pow(smoothedFade, 2f);
                    arrow.alpha = alpha;
                    checkLabel.label.alpha = alpha;
                    unlockLabel.label.alpha = alpha;
                    checkSprite.alpha = alpha;
                    unlockSprite.alpha = alpha;

                    for (int j = 0; j < 8; j++)
                    {
                        holdButton._rectH.sprites[j].alpha = alpha;
                    }

                    if (smoothedFade > 0f)
                    {
                        holdButton.Show();
                        for (int i = 0; i < 9; i++)
                        {
                            roundedRect.sprites[i].alpha = smoothedFade * 0.5f;
                            roundedRect.sprites[i].isVisible = true;
                        }
                        for (int i = 9; i < 17; i++)
                        {
                            roundedRect.sprites[i].alpha = smoothedFade;
                            roundedRect.sprites[i].isVisible = true;
                        }
                    }
                }

                public void OnPressDone(UIfocusable trigger)
                {
                    Plugin.RandoManager.GiveLocation(entryKey);
                }

                public static FSprite CheckToFSprite(string type, string name)
                {
                    string spriteName = "Futile_White";
                    float spriteScale = 1f;
                    Color spriteColor = Futile.white;

                    IconSymbol.IconSymbolData iconData;
                    switch (type)
                    {
                        case "Passage":
                            spriteName = name + "A";
                            if (name == "Gourmand")
                            {
                                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                                spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                                spriteColor = PlayerGraphics.DefaultSlugcatColor(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand);
                            }
                            break;
                        case "Echo":
                            spriteName = "smallKarma9-9";
                            spriteScale = 0.5f;
                            spriteColor = RainWorld.SaturatedGold;
                            break;
                        case "Pearl":
                            spriteName = "Symbol_Pearl";
                            DataPearl.AbstractDataPearl.DataPearlType pearl = new DataPearl.AbstractDataPearl.DataPearlType(name);
                            spriteColor = DataPearl.UniquePearlMainColor(pearl);
                            Color? highlight = DataPearl.UniquePearlHighLightColor(pearl);
                            if (highlight != null)
                            {
                                spriteColor = Custom.Screen(spriteColor, highlight.Value * Custom.QuickSaturation(highlight.Value) * 0.5f);
                            }
                            break;
                        case "Token":
                            spriteName = "ctOn";
                            spriteScale = 2f;
                            spriteColor = RainWorld.AntiGold.rgb;
                            break;
                        case "Token-L":
                            spriteName = "ctOn";
                            spriteScale = 2f;
                            spriteColor = new Color(1f, 0.6f, 0.05f);
                            break;
                        case "Token-S":
                            spriteName = "ctOn";
                            spriteScale = 2f;
                            spriteColor = CollectToken.RedColor.rgb;
                            break;
                        case "Broadcast":
                            spriteName = "ctOn";
                            spriteScale = 2f;
                            spriteColor = CollectToken.WhiteColor.rgb;
                            break;
                        case "FoodQuest":
                            if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(name))
                            {
                                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(name), 0);
                                spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                                spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                            }
                            else if (ExtEnumBase.GetNames(typeof(CreatureTemplate.Type)).Contains(name))
                            {
                                iconData = new IconSymbol.IconSymbolData(new CreatureTemplate.Type(name), AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                                spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                                spriteColor = CreatureSymbol.ColorOfCreature(iconData);
                            }
                            break;
                        default:
                            spriteName = "EndGameCircle";
                            spriteScale = 0.5f;
                            break;
                    }

                    try
                    {
                        return new FSprite(spriteName, true)
                        {
                            scale = spriteScale,
                            color = spriteColor,
                        };
                    }
                    catch
                    {
                        Plugin.Log.LogError($"Failed to load sprite '{spriteName}'");
                        return new FSprite("Futile_White", true);
                    }
                }

                public static FSprite UnlockToFSprite(Unlock unlock)
                {
                    string spriteName = "Futile_White";
                    float spriteScale = 1f;
                    Color spriteColor = Futile.white;

                    IconSymbol.IconSymbolData iconData;
                    switch (unlock.Type.value)
                    {
                        case "Gate":
                            spriteName = "smallKarmaNoRingD";
                            spriteScale = 0.75f;
                            break;
                        case "Token":
                            spriteName = unlock.ID + "A";
                            break;
                        case "Karma":
                            spriteName = "smallKarma9-9";
                            spriteScale = 0.5f;
                            break;
                        case "Item":
                            if (unlock.item.Value.id == "KarmaFlower")
                            {
                                spriteName = "FlowerMarker";
                                spriteColor = RainWorld.GoldRGB;
                            }
                            else
                            {
                                if (ExtEnumBase.GetNames(typeof(AbstractPhysicalObject.AbstractObjectType)).Contains(unlock.ID))
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, new AbstractPhysicalObject.AbstractObjectType(unlock.ID), 0);
                                }
                                else if (unlock.item.Value.id == "FireSpear")
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 1);
                                }
                                else if (unlock.item.Value.id == "ElectricSpear")
                                {
                                    iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.Spear, 2);
                                }
                                else
                                {
                                    iconData = new IconSymbol.IconSymbolData();
                                }

                                spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                                spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                            }
                            break;
                        case "ItemPearl":
                            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.DataPearl, 0);
                            spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                            spriteColor = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                            break;
                        case "HunterCycles":
                            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.Slugcat, AbstractPhysicalObject.AbstractObjectType.Creature, 0);
                            spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                            spriteColor = PlayerGraphics.DefaultSlugcatColor(SlugcatStats.Name.Red);
                            break;
                        case "The_Mark":
                        case "Neuron_Glow":
                        case "IdDrone":
                        case "DisconnectFP":
                        case "RewriteSpearPearl":
                            iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.NSHSwarmer, 0);
                            spriteName = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                            break;
                        default:
                            spriteName = "EndGameCircle";
                            spriteScale = 0.5f;
                            break;
                    }

                    try
                    {
                        return new FSprite(spriteName, true)
                        {
                            scale = spriteScale,
                            color = spriteColor,
                        };
                    }
                    catch
                    {
                        Plugin.Log.LogError($"Failed to load sprite '{spriteName}'");
                        return new FSprite("Futile_White", true);
                    }
                }
            }
        }
    }
}
