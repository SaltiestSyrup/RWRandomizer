using Menu;
using RainWorldRandomizer.WatcherIntegration;
using RWCustom;
using System;
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
            size = Scug is "Watcher" ? new(540f, 430f) : new(320f, 310f);
            fillAlpha = 1f;

            // Nodes have to exist before the connectors, but we want the connectors to be behind the nodes.
            CreateNodes();
            CreateConnectors();
            ParseLocationStatus();
            foreach (Connector connector in connectors.Values) Container.AddChild(connector);
            subObjects.AddRange(nodes.Values);

            IEnumerable<string> gates =
                Plugin.RandoManager.GetGatesStatus().Where(x => x.Value).Select(x => x.Key)
                .Union(Items.GetAllOpenWarps().Select(x => $"Warp-{x}"));

            foreach (string gate in gates)
            {
                if (connectors.TryGetValue(gate, out Connector connector)) connector.Color = COLOR_ACCESSIBLE;
            }

            foreach (string nodeName in GetAccessibleNodes(gates))
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
            locationInfos = Plugin.RandoManager.GetLocations();
            foreach (KeyValuePair<string, Node> pair in nodes)
            {
                IEnumerable<LocationInfo> nodeInfos = locationInfos.Where(x => GetNodeName(x.region) == pair.Key);
                pair.Value.completion = nodeInfos.Count(x => x.Collected) / (float)nodeInfos.Count();
            }
        }

        internal static List<string> watcherNodeOrder =
        [
             null,  "WARB", "WARC", "WSSR", "WORA", "WRSA", "WARA", "WARB*",
             null,  "WARE", "WVWB",  null,  "WBLA", "WAUA", "WPTA", "WARC*",
            "WRFB", "WTDA", "WSKC", "WARD", "WMPA", "WTDA*","WSKC*","WVWA*",
            "WVWA", "WTDB", "WARG", "WSKD",  null,   null,   null,   null,
            "WRRA", "WRFA", "WPGA", "WSKA",  null,  "WHIR", "WGWR",  "<P>",
             null,  "WSKB",  null,   null,  "WARF", "WSUR", "WDSR",  "<FQ>",
        ];

        public void CreateNodes()
        {
            if (Scug is "Watcher")
            {
                Vector2 pos = new(40f, 40f);

                foreach (string nodeName in watcherNodeOrder)
                {
                    if (nodeName is not null)
                    {
                        string displayName = nodeName switch
                        {
                            "<FQ>" => "FQ",
                            "<P>" => "P",
                            _ => nodeName.Substring(1, 3)
                        };
                        nodes[nodeName] = new(menu, this, pos, displayName);
                    }
                    pos += pos.x < 460f ? new Vector2(60f, 0f) : new Vector2(-420f, 40f);
                }

                return;
            }

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
            if (Scug is "Watcher")
            {
                // List every node that is connected to neighboring nodes in a specific way.
                // The first item is how many places to jump in the array to get from A to B.
                // The second and third items are which side of node A and which side of node B to connect.
                List<ValueTuple<int, int, int, List<int>>> directives =
                [
                    new(1, 0, 4, [1, 3, 4, 5, 6, 18, 19, 25, 26, 32, 33, 34]),  // to right
                    new(8, 2, 6, [1, 2, 6, 9, 12, 14, 15, 16, 19]),  // to above
                    new(9, 1, 5, [6, 9, 10, 12, 14, 16, 17, 32, 35]),  // to upper right
                    new(7, 3, 7, [6, 9, 19, 25, 27]),  // to upper left
                    new(2, 0, 4, [10]), // 2 to the right
                    new(16, 2, 6, [3]), // 2 to above
                    new(3, 0, 4, [41]), // 3 to the right
                    new(24, 2, 6, [20]), // 3 to above
                    new(11, 1, 5, [33]), // 3 right, 1 above
                    new(17, 1, 5, [27]), // 1 right, 2 above
                    new(23, 3, 7, [21]) // 1 left, 3 above

                ];

                foreach ((int, int, int, List<int>) directive in directives)
                {
                    foreach (int num in directive.Item4)
                    {
                        string leftName = watcherNodeOrder[num];
                        string rightName = watcherNodeOrder[num + directive.Item1];
                        string keyName = string.Join("-", (new string[] { leftName.Substring(0, 4), rightName.Substring(0, 4) }).OrderBy(x => x));

                        // One-ways
                        if (CanUseGate(keyName)[0] ^ CanUseGate(keyName)[1])
                        {
                            Vector2[] vertices = 
                            [
                                nodes[leftName].FetchDirection(directive.Item2),
                                nodes[rightName].FetchDirection(directive.Item3)
                            ];
                            // Invert direction if ST is on the other side of the warp string XOR the nodes were swapped when making the warp string
                            if (!CanUseGate(keyName)[0] ^ leftName.CompareTo(rightName) > 0) vertices = [vertices[1], vertices[0]];

                            connectors[$"Warp-{keyName}"] = Connector.OneWay(vertices);
                            continue;
                        }

                        connectors[$"Warp-{keyName}"] = new(
                            nodes[leftName].FetchDirection(directive.Item2),
                            nodes[rightName].FetchDirection(directive.Item3)
                            );
                    }
                }

                return;
            }

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

        public static string ActualStartRegion => Plugin.RandoManager.customStartDen.Split('_')[0];

        /// <summary>
        /// Determine whether a particular gate is usable in either direction.
        /// </summary>
        /// <param name="key">The gate name ("GATE_A_B").</param>
        /// <returns>Two <see cref="bool"/>s - one for whether going from region A to region B is possible, and one for B to A.
        /// Note that which region is A and which is B is dependent only on the gate name, not on the physical position of the regions.</returns>
        public static bool[] CanUseGate(string key)
        {
            if (key.StartsWith("GATE_") || key.StartsWith("Warp-")) key = key.Substring(5);

            // (Most) Daemon warps are one way entering Daemon
            if (key.Contains("WRSA") && !key.Contains("WORA") && !key.Contains("WARA"))
            {
                string[] split = key.Split('-');
                if (split[1] == "WRSA") return [false, true];
                else return [true, false];
            }

            return key switch
            {
                "LF_SB" => Scug is "Saint" ? [true, true] : [true, false],
                "SL_MS" => [false, true],
                "OE_SU" => [true, false],
                "UW_SL" => Scug is "Artificer" or "Spear" ? [true, true] : [false, false],
                "SL_VS" => Scug is "Artificer" ? [false, false] : [true, true],

                "WRFB-WTDB" => [false, true],
                "WARE-WRFB" => [false, true],
                "WARE-WSKC" => [true, false],
                "WARD-WVWB" => [false, true],
                "WBLA-WVWB" => [true, false],
                "WBLA-WTDA" => [false, true],
                "WARF-WTDA" => [true, false],
                "WPTA-WSKC" => [false, true],
                "WARA-WPTA" => [false, true],
                "WARC-WVWA" => [false, true],
                "WARA-WARC" => [false, true],
                "WARA-WARB" => [false, true],
                "WARA-WAUA" => [true, false],

                //"WBLA-WSSR" => [true, false],
                "WARD-WSSR" => [true, false],
                "WORA-WSSR" => [false, true],
                //"WORA-WSUR" => [false, true],
                //"WGWR-WORA" => [true, false],
                //"WHIR-WORA" => [true, false],
                //"WDSR-WORA" => [true, false],
                "WARA-WRSA" => [false, true],

                _ => [true, true],
            };
        }

        /// <summary>
        /// Given a list of currently held keys, determinine which region nodes are accessible.
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
                    List<string> names = [.. pair.Key.Replace("_", "-").Split('-').Skip(1).Select(GetNodeName)];
                    bool[] usable = pair.Value;

                    for (int i = 0; i < 2; i++)
                    {
                        string here = names[i]; string there = names[1 - i];
                        if (usable[i] && ((ret.Contains(here) && !ret.Contains(there)) || (ret.Contains($"{here}*") && !ret.Contains($"{there}*"))))
                        {
                            ret.Add(there); ret.Add($"{there}*"); updated = true;
                        }
                    }
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

            public Vector2 FetchDirection(int dir) => dir switch
            {
                0 => Right,
                1 => TopRight,
                2 => Top,
                3 => TopLeft,
                4 => Left,
                5 => BottomLeft,
                6 => Bottom,
                7 => BottomRight,
                _ => throw new ArgumentOutOfRangeException()
            };

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

            /// <summary>
            /// Create a one-way Connector from a list of vector vertices. The first given vertex will be considered the "start" of the connection.
            /// </summary>
            public static Connector OneWay(params Vector2[] vertices)
            {
                Connector c = new Connector(vertices);
                Vector2 dotPos = Vector2.MoveTowards(vertices[0], vertices[1], 4);
                c.AddChild(new Dot(dotPos));
                return c;
            }

            public Color Color
            {
                set 
                {
                    foreach (Segment segment in _childNodes.OfType<Segment>()) segment.color = value;
                    foreach (Dot dot in _childNodes.OfType<Dot>()) dot.color = value;
                }
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

            public class Dot : FSprite
            {
                public Dot(Vector2 pos) : base("Circle20")
                {
                    SetPosition(pos);
                    color = COLOR_INACCESSIBLE;
                    width = 7f;
                    height = 7f;
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
            if (Scug is "Watcher" && region.EndsWith("*")) region = region.Substring(0, 4);

            if (highlightedNode != null) highlightedNode.current = false;
            highlightedNode = node;
            highlightedNode.current = true;
            displayedRegion = region;

            checkIconContainer.RemoveAllChildren();
            foreach (LocationInfo info in locationInfos.Where(x => x.region == region))
                checkIconContainer.AddIcon(info);
            checkIconContainer.Refresh();
            //Plugin.Log.LogDebug($"Updating for region {region}: {string.Join(", ", locs)}");
            //Plugin.Log.LogDebug($"All locations: {string.Join(", ", Plugin.RandoManager.GetLocations())}");
        }

        public class CheckIconContainer : FContainer
        {
            public FLabel regionLabel;

            public CheckIconContainer()
            {
                regionLabel = new FLabel(Custom.GetFont(), "") { x = 150.01f, y = 30.01f, alignment = FLabelAlignment.Center };
                AddChild(regionLabel);
            }

            public void AddIcon(LocationInfo info)
            {
                AddChild(CheckIcon.New(info));
            }

            public void Refresh()
            {
                Vector2 pos = new(20f, Scug is "Watcher" ? 400f : 280f);
                foreach (CheckIcon sprite in _childNodes.OfType<CheckIcon>())
                {
                    sprite.SetPosition(pos + sprite.Adjustment);
                    pos += new Vector2(sprite.width + 5f + sprite.Padding, 0f);
                    if (pos.x > (Scug is "Watcher" ? 420f : 300f)) pos = new Vector2(20f, pos.y - 35f);
                }
            }

            public class CheckIcon(string element, LocationKind kind) : FSprite(element)
            {
                public Vector2 Adjustment => kind switch
                {
                    LocationKind.Pearl => new(-6f, 0f),
                    LocationKind.FixedWarp => new(10f, 0f),
                    LocationKind.ThroneWarp => new(8f, 0f),
                    LocationKind.SpreadRot => new(4f, 0f),
                    LocationKind.Prince => new(6f, 0f),
                    _ => default
                };

                public float Padding => kind switch
                {
                    LocationKind.FixedWarp => -15f,
                    LocationKind.Prince => -10f,
                    LocationKind.ThroneWarp => -10f,
                    LocationKind.Echo => -7f,
                    LocationKind.SpinningTop => -7f,
                    _ => default
                };

                public LocationKind kind = kind;

                public static CheckIcon New(LocationInfo check)
                {
                    FSprite sprite = check.ToFSprite();

                    CheckIcon ret = new(sprite.element.name, check.kind)
                    {
                        color = check.Collected ? sprite.color : new Color(0.2f, 0.2f, 0.2f),
                        scale = sprite.scale,
                    };
                    return ret;
                }
            }
        }
    }
}
