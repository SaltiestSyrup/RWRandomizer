using Menu;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LocationKind = RainWorldRandomizer.LocationInfo.LocationKind;

namespace RainWorldRandomizer
{
    public class GateMapDisplay : RoundedRect
    {
        public Dictionary<string, Node> nodes = [];
        public Dictionary<string, Connector> connectors = [];
        public IEnumerable<LocationInfo> locationInfos;
        public Node highlightedNode;
        public static string Scug => Plugin.RandoManager.currentSlugcat?.value ?? "White";
        public static string CurrentRegion => (Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.world.name;
        public static Color COLOR_ACCESSIBLE = Color.white;
        public static Color COLOR_INACCESSIBLE = new(0.2f, 0.2f, 0.2f);
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
            locationInfos = Plugin.RandoManager.locations;
            foreach (KeyValuePair<string, Node> pair in nodes)
            {
                IEnumerable<LocationInfo> nodeInfos = locationInfos.Where(x => GetNodeName(x.region) == pair.Key);
                pair.Value.completion = nodeInfos.Count(x => x.Collected) / (float)nodeInfos.Count();
            }
        }

        //public readonly struct LocationInfo
        //{
        //    public readonly LocationKind kind;
        //    public readonly string name;
        //    public readonly string region;
        //    public readonly string node;
        //    public readonly bool collected;

        //    public LocationInfo(string location, bool collected)
        //    {
        //        kind = KindOfLocation(location);
        //        region = RegionOfLocation(kind, location);
        //        node = GetNodeName(region);
        //        name = location;
        //        this.collected = collected;
        //    }

        //    public LocationInfo(KeyValuePair<string, bool> pair) : this(pair.Key, pair.Value) { }

        //    public static string RegionOfLocation(LocationKind kind, string location)
        //    {
        //        switch (kind)
        //        {
        //            case LocationKind.BlueToken:
        //            case LocationKind.RedToken:
        //            case LocationKind.GreenToken:
        //            case LocationKind.Broadcast:
        //            case LocationKind.Pearl:
        //                return location.Split('-')[2];
        //            case LocationKind.Echo:
        //                return location.Split('-')[1];
        //            case LocationKind.GoldToken:
        //                string third = location.Split('-')[2];
        //                return third switch
        //                {
        //                    "GWold" => "GW",
        //                    "gutter" => "SB",
        //                    _ => third,
        //                };
        //            case LocationKind.Shelter:
        //                return location.Substring(8, 2);
        //            case LocationKind.FoodQuest:
        //                return "<FQ>";
        //            case LocationKind.Passage:
        //                return "<P>";
        //            default:
        //                return location switch
        //                {
        //                    "Eat_Neuron" => "<P>",
        //                    "Meet_LttM_Spear" => "DM",
        //                    "Kill_FP" => "RM",
        //                    "Gift_Neuron" or "Meet_LttM" or "Save_LttM" or "Ascend_LttM" => "SL",
        //                    "Meet_FP" or "Ascend_FP" => "SS",
        //                    _ => null,
        //                };
        //        }
        //    }

        //    public static LocationKind KindOfLocation(string location)
        //    {
        //        if (location.StartsWith("Pearl-")) return LocationKind.Pearl;
        //        if (location.StartsWith("Shelter-")) return LocationKind.Shelter;
        //        if (location.StartsWith("Broadcast-")) return LocationKind.Broadcast;
        //        if (location.StartsWith("Echo-")) return LocationKind.Echo;
        //        if (location.StartsWith("Token-L-")) return LocationKind.GoldToken;
        //        if (location.StartsWith("Token-S-")) return LocationKind.RedToken;
        //        if (location.StartsWith("Token-")) return LocationKind.BlueToken;
        //        if (location.StartsWith("Passage-")) return LocationKind.Passage;
        //        if (location.StartsWith("Wanderer-")) return LocationKind.WandererPip;
        //        if (location.StartsWith("FoodQuest-")) return LocationKind.FoodQuest;
        //        return LocationKind.Other;
        //    }
        //}

        public void CreateNodes()
        {
            // Nodes which are always present, regardless of gamestate.
            nodes["SB"] = new Node(menu, this, new Vector2(40f, 40f), "SB");
            nodes["DS"] = new Node(menu, this, new Vector2(100f, 40f), Scug is "Saint" ? "UG" : "DS");
            nodes["LF"] = new Node(menu, this, new Vector2(40f, 80f), "LF");
            nodes["SU"] = new Node(menu, this, new Vector2(100f, 80f), "SU");
            nodes["GW"] = new Node(menu, this, new Vector2(160f, 80f), "GW");
            nodes["HI"] = new Node(menu, this, new Vector2(100f, 120f), "HI");
            nodes["SH"] = new Node(menu, this, new Vector2(160f, 120f), Scug is "Saint" ? "CL" : "SH");
            nodes["SL"] = new Node(menu, this, new Vector2(220f, 120f), Scug is "Artificer" or "Spear" ? "LM" : "SL");
            nodes["SI"] = new Node(menu, this, new Vector2(40f, 160f), "SI");
            nodes["CC"] = new Node(menu, this, new Vector2(100f, 160f), "CC");
            nodes["<P>"] = new Node(menu, this, new Vector2(280f, 40f), "P");

            if (ModManager.MSC)
            {
                nodes["VS"] = new Node(menu, this, new Vector2(40f, 120f), "VS");
                if (Scug is "White" or "Yellow" or "Gourmand") nodes["OE"] = new Node(menu, this, new Vector2(70f, 60f), "OE");
                if (Scug is "Artificer") nodes["LC"] = new Node(menu, this, new Vector2(160f, 200f), "LC");
                if (Scug is not "Artificer") nodes["MS"] = new Node(menu, this, new Vector2(280f, 160f), Scug is "Spear" ? "DM" : "MS");
                nodes["<FQ>"] = new Node(menu, this, new Vector2(280f, 80f), "FQ");
            }

            if (Scug is not "Saint")
            {
                nodes["UW"] = new Node(menu, this, new Vector2(160f, 160f), "UW");
                nodes["SS"] = new Node(menu, this, new Vector2(220f, 160f), Scug is "Rivulet" ? "RM" : "SS");
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


            if (Scug is not "Saint")
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

                if (Scug is "Artificer") connectors["GATE_UW_LC"] = new Connector(nodes["UW"].Top, nodes["LC"].Bottom);
                if (Scug is "Spear")
                {
                    connectors["GATE_DM_SL"] = new Connector(nodes["SL"].TopRight, nodes["MS"].Bottom);
                    connectors["GATE_SL_DM"] = new Connector(nodes["SL"].Top, nodes["MS"].BottomLeft);
                }
                if (Scug is not "Artificer" and not "Spear")
                {
                    connectors["GATE_MS_SL"] = new Connector(nodes["SL"].TopRight, nodes["MS"].Bottom);
                    connectors["GATE_SL_MS"] = new Connector(nodes["SL"].Top, nodes["MS"].BottomLeft);
                }
                if (Scug is not "Saint")
                {
                    connectors["GATE_UW_SL"] = new Connector(nodes["SL"].TopLeft, nodes["UW"].BottomRight);
                }
                if (Scug is "White" or "Yellow" or "Gourmand")
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
                return Plugin.RandoManager.customStartDen.Split('_')[0];
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
            return key switch
            {
                "GATE_LF_SB" => Scug is "Saint" ? [true, true] : [true, false],
                "GATE_SL_MS" => [false, true],
                "GATE_OE_SU" => [true, false],
                "GATE_UW_SL" => Scug is "Artificer" or "Spear" ? [true, true] : [false, false],
                "GATE_SL_VS" => Scug is "Artificer" ? [false, false] : [true, true],
                _ => [true, true],
            };
        }

        /// <summary>
        /// Given a list of currently held gate keys, determinine which region nodes are accessible.
        /// </summary>
        public static IEnumerable<string> GetAccessibleNodes(IEnumerable<string> keys)
        {
            List<string> ret = [GetNodeName(ActualStartRegion), "<FQ>", "<P>"];
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
            return code switch
            {
                "LM" => "SL",
                "RM" => "SS",
                "UG" => "DS",
                "DM" => "MS",
                "CL" => "SH",
                _ => code,
            };
        }

        /// <summary>Get the code of the region actually represented by a given node.  null if the region does not exist in the current gamestate.</summary>
        public static string GetActualRegion(string node)
        {
            string scug = Scug;
            return node switch
            {
                "SL" => scug is "Artificer" or "Spear" ? "LM" : "SL",
                "SS" => scug is "Rivulet" ? "RM" : (scug is "Saint" ? null : "SS"),
                "DS" => scug is "Saint" ? "UG" : "DS",
                "MS" => scug is "Spear" ? "DM" : "MS",
                "SH" => scug is "Saint" ? "CL" : "SH",
                "UW" => scug is "Saint" ? null : "UW",
                _ => node,
            };
        }

        public class Node : RoundedRect
        {
            public MenuLabel label;
            public RoundedRect outer;
            public static Vector2 SIZE = new(30f, 20f);
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
                    return label.label.text switch
                    {
                        "<FQ>" => "Food Quest",
                        "<P>" => "Passages",
                        _ => "Unknown region",
                    };
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
                List<Segment> segments = [];
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
                List<Segment> segments = [];
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
            //if (Plugin.RandoManager is not ManagerArchipelago) return;
            if (!nodes.TryGetValue(GetNodeName(region), out Node node)) return;

            if (highlightedNode != null) highlightedNode.current = false;
            highlightedNode = node;
            highlightedNode.current = true;
            displayedRegion = region;

            checkIconContainer.RemoveAllChildren();
            foreach (LocationInfo info in locationInfos.Where(x => x.region == region))
                checkIconContainer.AddIcon(info.kind, info.internalName, info.Collected);
            checkIconContainer.Refresh();
            //Plugin.Log.LogDebug($"Updating for region {region}: {string.Join(", ", locs)}");
            //Plugin.Log.LogDebug($"All locations: {string.Join(", ", Plugin.RandoManager.GetLocations())}");
        }

        //public enum LocationKind { BlueToken, RedToken, GoldToken, GreenToken, Broadcast, Pearl, Echo, Shelter, Passage, WandererPip, FoodQuest, Other }

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
                Vector2 pos = new(20f, 280f);
                foreach (CheckIcon sprite in _childNodes.OfType<CheckIcon>())
                {
                    sprite.SetPosition(pos + sprite.Adjustment);
                    pos += new Vector2(sprite.width + 5f, 0f);
                    if (pos.x > 300f) pos = new Vector2(20f, pos.y - 35f);
                }
            }

            public class CheckIcon(string element, LocationKind kind, string name) : FSprite(element)
            {
                public Vector2 Adjustment
                {
                    get
                    {
                        return kind == LocationKind.Pearl ? new Vector2(-6f, 0f) : default;
                    }
                }
                public LocationKind kind = kind;
                public string name = name;

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
                            DataPearl.AbstractDataPearl.DataPearlType pearl = new(name);
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
                            if (name is "Eat_Neuron" or "Gift_Neuron")
                            {
                                iconData = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.SSOracleSwarmer, 0);
                                element = ItemSymbol.SpriteNameForItem(iconData.itemType, iconData.intData);
                                color = ItemSymbol.ColorForItem(iconData.itemType, iconData.intData);
                            }
                            else if (ModManager.MSC && name is "Kill_FP") { element = "GuidanceEnergyCell"; }
                            else if (ModManager.MSC) { element = "GuidancePebbles"; }
                            break;
                        default:
                            element = "EndGameCircle";
                            scale = 0.5f;
                            break;
                    }

                    CheckIcon ret = new(element, kind, name)
                    {
                        color = is_checked ? color : new Color(0.2f, 0.2f, 0.2f),
                        scale = scale
                    };
                    return ret;
                }
            }
        }
    }

}
