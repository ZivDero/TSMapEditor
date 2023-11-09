using Rampastring.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TSMapEditor.GameMath;
using TSMapEditor.Rendering;

namespace TSMapEditor.Models
{
    public class ConnectedOverlayFrame
    {
        public OverlayType OverlayType { get; set; }
        public int FrameIndex { get; set; }
        public BitArray ConnectsTo { get; set; }
    }
    public class ConnectedOverlayType
    {
        public ConnectedOverlayType(IniSection iniSection, Rules rules)
        {
            Name = iniSection.SectionName;
            FrameCount = iniSection.GetIntValue("Frames", 0);

            if (FrameCount < 1)
                throw new Exception($"Connected overlay type {Name} has an invalid frame count {FrameCount}!");

            string connectionMaskString = iniSection.GetStringValue("ConnectionMask", null);
            if (connectionMaskString == null || connectionMaskString.Length != 8 || Regex.IsMatch(connectionMaskString, "[^01]"))
                throw new Exception($"Connected overlay type has an invalid connection mask {connectionMaskString}!");
            ConnectionMask = new BitArray(connectionMaskString.Select(c => c == '1').ToArray());

            Frames = new List<ConnectedOverlayFrame>();

            for (int i = 0; i < FrameCount; i++)
            {
                string overlayName = iniSection.GetStringValue($"Frame{i}.Overlay", null);
                OverlayType overlayType = rules.FindOverlayType(overlayName) ??
                              throw new Exception($"Connected overlay type {i} has an invalid overlay name {overlayName}!");

                int frameIndex = iniSection.GetIntValue($"Frame{i}.FrameIndex", -1);
                if (frameIndex < 0)
                    throw new Exception($"Connected overlay type {i} has an invalid frame index {frameIndex}!");

                string connectsToString = iniSection.GetStringValue($"Frame{i}.ConnectsTo", null);
                if (connectsToString == null || connectsToString.Length != 8 || Regex.IsMatch(connectsToString, "[^01]"))
                    throw new Exception($"Connected overlay type {i} has an invalid ConnectsTo mask {connectsToString}!");
                BitArray connectsTo = new BitArray(connectsToString.Select(c => c == '1').ToArray());

                Frames.Add(new ConnectedOverlayFrame()
                {
                    OverlayType = overlayType,
                    FrameIndex = frameIndex,
                    ConnectsTo = connectsTo
                });
            }
        }

        public string Name { get; set; }
        public int FrameCount { get; set; }
        public BitArray ConnectionMask { get; set; }
        public List<ConnectedOverlayFrame> Frames { get; set; }

        public ConnectedOverlayFrame GetOverlayForCell(IMutationTarget mutationTarget, Point2D cellCoords)
        {
            BitArray connectionMask = new BitArray(8);
            for (int xOffset = -1; xOffset <= 1; xOffset++)
            {
                for (int yOffset = -1; yOffset <= 1; yOffset++)
                {
                    if (xOffset == 0 && yOffset == 0)
                        continue;

                    var tile = mutationTarget.Map.GetTile(cellCoords + new Point2D(xOffset, yOffset));

                    if (tile == null || tile.Overlay == null)
                        continue;

                    if (ContainsOverlay(tile.Overlay))
                    {
                        int bitIndex = (yOffset + 1) + (xOffset + 1) * 3;
                        if (bitIndex >= 4)
                            bitIndex--;
                        connectionMask.Set(bitIndex, true);
                    }
                }
            }

            connectionMask.And(ConnectionMask);
            return Frames.Find(frame => ((BitArray)frame.ConnectsTo.Clone()).Xor(connectionMask).OfType<bool>().All(e => !e));
        }

        public bool ContainsOverlay(Overlay overlay)
        {
            return ContainsOverlay(overlay.OverlayType, overlay.FrameIndex);
        }

        public bool ContainsOverlay(OverlayType overlayType, int frameIndex)
        {
            foreach (var frame in Frames)
            {
                if (overlayType == frame.OverlayType &&
                    frameIndex == frame.FrameIndex)
                {
                    return true;
                }
            }

            return false;
        }
    }

}
