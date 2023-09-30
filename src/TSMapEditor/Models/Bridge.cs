using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.Models
{
    public class BridgeLoadException : Exception
    {
        public BridgeLoadException(string message) : base(message)
        {
        }
    }

    public enum BridgeType
    {
        Low,
        High
    }

    public class BridgeConfig
    {
        public BridgeConfig(IniSection iniSection, BridgeDirection direction, Bridge bridge, Rules rules)
        {
            string suffix = direction == BridgeDirection.EastWest ? "EW" : "NS";

            if (bridge.Type == BridgeType.Low)
            {
                string bridgeStart = iniSection.GetStringValue($"BridgeStart.{suffix}", null);
                if (bridgeStart == null)
                    throw new BridgeLoadException($"Low bridge {bridge.Name} has no start overlay!");
                Start = rules.FindOverlayType(bridgeStart)?.Index ??
                        throw new BridgeLoadException($"Low bridge {bridge.Name} has an invalid start overlay {bridgeStart}!");

                string bridgeEnd = iniSection.GetStringValue($"BridgeEnd.{suffix}", null);
                if (bridgeEnd == null)
                    throw new BridgeLoadException($"Low bridge {bridge.Name} has no end overlay!");
                End = rules.FindOverlayType(bridgeEnd)?.Index ??
                      throw new BridgeLoadException($"Low bridge {bridge.Name} has an invalid end overlay {bridgeEnd}!");

                Pieces = iniSection.GetListValue($"BridgePieces.{suffix}", ',', (overlayName) => rules.FindOverlayType(overlayName)?.Index ??
                    throw new BridgeLoadException($"Low bridge {bridge.Name} has an invalid bridge piece {overlayName}!"));
                if (Pieces.Count == 0)
                    throw new BridgeLoadException($"Low bridge {bridge.Name} has no bridge pieces!");
            }
            else
            {
                string piece = iniSection.GetStringValue($"BridgePieces.{suffix}", null);
                if (piece == null)
                    throw new BridgeLoadException($"High bridge {bridge.Name} has no bridge piece!");
                int bridgePiece = rules.FindOverlayType(piece)?.Index ??
                      throw new BridgeLoadException($"High bridge {bridge.Name} has an invalid bridge piece {piece}!");
                Pieces.Add(bridgePiece);
            }
        }

        public int Start;
        public int End;
        public List<int> Pieces = new List<int>();
    }

    public class Bridge
    {
        public Bridge(IniSection iniSection, Rules rules)
        {
            Name = iniSection.SectionName;

            string bridgeType = iniSection.GetStringValue("Type", null);
            if (bridgeType == "Low")
                Type = BridgeType.Low;
            else if (bridgeType == "High")
                Type = BridgeType.High;
            else throw new BridgeLoadException($"Bridge {Name} has an invalid Type!");

            NorthSouth = new BridgeConfig(iniSection, BridgeDirection.NorthSouth, this, rules);
            EastWest = new BridgeConfig(iniSection, BridgeDirection.EastWest, this, rules);

            if (Type == BridgeType.High)
                TileSetIndex = iniSection.GetIntValue("TileSet", -1);
        }

        public string Name;
        public BridgeType Type;

        public BridgeConfig NorthSouth;
        public BridgeConfig EastWest;
        public int TileSetIndex;
    }
}
