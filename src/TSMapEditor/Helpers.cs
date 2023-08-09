﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using System;
using TSMapEditor.GameMath;
using TSMapEditor.Models.Enums;

namespace TSMapEditor
{
    public static class Helpers
    {
        public static bool IsStringNoneValue(string str)
        {
            return str.Equals(Constants.NoneValue1, StringComparison.InvariantCultureIgnoreCase) ||
                str.Equals(Constants.NoneValue2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static string BoolToIntString(bool value)
        {
            return value ? "1" : "0";
        }

        public static Point2D? CoordStringToPoint(string coordsString)
        {
            int coords = Conversions.IntFromString(coordsString, -1);
            if (coords < 0 || coordsString.Length < 4)
            {
                Logger.Log("CoordStringToPoint: invalid coord string " + coordsString);
                return null;
            }

            string xCoordPart = coordsString.Substring(coordsString.Length - 3);
            int x = Conversions.IntFromString(xCoordPart, -1);
            if (x < 0)
            {
                Logger.Log("CoordStringToPoint: invalid X coord " + x);
                return null;
            }

            string yCoordPart = coordsString.Substring(0, coordsString.Length - 3);
            int y = Conversions.IntFromString(yCoordPart, -1);
            if (y < 0)
            {
                Logger.Log("CoordStringToPoint: invalid Y coord " + y);
                return null;
            }

            return new Point2D(x, y);
        }

        public static string LandTypeToString(int landType)
        {
            return GetLandTypeName(landType) + " (0x" + landType.ToString("X") + ")";
        }

        private static string GetLandTypeName(int landType)
        {
            switch (landType)
            {
                case 0x0:
                case 0xD:
                    return "Clear";
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4:
                    return "Ice";
                case 0x5:
                    return "Tunnel";
                case 0x6:
                    return "Railroad";
                case 0x7:
                case 0x8:
                    return "Rock";
                case 0x9:
                    return "Water";
                case 0xA:
                    return "Beach";
                case 0xB:
                case 0xC:
                    return "Road";
                case 0xE:
                    return "Rough";
                case 0xF:
                    return "Rock";
                default:
                    return "Unknown";
            }
        }

        public static bool IsLandTypeImpassable(int landType, bool considerLandUnitsOnly = false)
        {
            switch (landType)
            {
                case 0x1:
                case 0x2:
                case 0x3:
                case 0x4:
                case 0x9:
                    return considerLandUnitsOnly;
                case 0x7:
                case 0x8:
                case 0xF:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsLandTypeImpassable(LandType landType, bool considerLandUnitsOnly)
        {
            return landType == LandType.Rock || (considerLandUnitsOnly && landType == LandType.Water);
        }

        public static bool IsLandTypeWater(int landType)
        {
            return landType == 0x9;
        }

        public static int GetWaypointNumberFromAlphabeticalString(string str)
        {
            if (str.Length < 1 || str.Length > 2 ||
                str[0] < 'A' || str[0] > 'Z' || (str.Length == 2 && (str[1] < 'A' || str[1] > 'Z')))
            {
                //throw new InvalidOperationException("Waypoint values are only valid between A and ZZ. Invalid value: " + str);
                Logger.Log($"Waypoint values are only valid between A and ZZ. Invalid value: {str}. Converting into 0.");
                return 0;
            }

            if (str.Length == 1)
                return str[0] - 'A';

            const int CharCount = 26;

            int multiplier = (str[0] - 'A' + 1);
            return (multiplier * CharCount) + (str[1] - 'A');
        }

        public static string WaypointNumberToAlphabeticalString(int waypointNumber)
        {
            if (waypointNumber > Constants.MaxWaypoint)
                return "A"; // matches 0

            const int CharCount = 26;

            int firstLetterValue = (waypointNumber / CharCount);
            int secondLetterValue = waypointNumber % CharCount;

            if (firstLetterValue == 0)
                return ((char)('A' + secondLetterValue)).ToString();

            return ((char)('A' + (firstLetterValue - 1))).ToString() + ((char)('A' + secondLetterValue)).ToString();
        }

        private static Point2D[] visualDirectionToPointTable = new Point2D[]
        {
            new Point2D(0, -1), new Point2D(1, -1), new Point2D(1, 0),
            new Point2D(1, 1), new Point2D(0, 1), new Point2D(-1, 1),
            new Point2D(-1, 0), new Point2D(-1, -1)
        };

        public static Point2D VisualDirectionToPoint(Direction direction) => visualDirectionToPointTable[(int)direction];

        /// <summary>
        /// Creates and returns a new UI texture.
        /// </summary>
        /// <param name="gd">A GraphicsDevice instance.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mainColor">The background color of the texture.</param>
        /// <param name="secondaryColor">The first border color.</param>
        /// <param name="tertiaryColor">The second border color.</param>
        /// <returns></returns>
        public static Texture2D CreateUITexture(GraphicsDevice gd, int width, int height, Color mainColor, Color secondaryColor, Color tertiaryColor)
        {
            Texture2D Texture = new Texture2D(gd, width, height, false, SurfaceFormat.Color);

            Color[] color = new Color[width * height];

            // background color
            // ***

            for (int i = 0; i < color.Length; i++)
                color[i] = mainColor;

            // main border
            // ***

            // top
            for (int i = width; i < (width * 3); i++)
                color[i] = secondaryColor;

            // bottom
            for (int i = color.Length - (width * 3); i < color.Length - width; i++)
                color[i] = secondaryColor;

            // right
            for (int i = 1; i < color.Length - width - 2; i = i + width)
                color[i] = secondaryColor;

            for (int i = 2; i < color.Length - width - 2; i = i + width)
                color[i] = secondaryColor;

            // left
            for (int i = width - 3; i < color.Length; i = i + width)
                color[i] = secondaryColor;

            for (int i = width - 2; i < color.Length; i = i + width)
                color[i] = secondaryColor;

            // outer border
            // ***

            // top
            for (int i = 0; i < width; i++)
                color[i] = tertiaryColor;

            // bottom
            for (int i = color.Length - width; i < color.Length; i++)
                color[i] = tertiaryColor;

            // right
            for (int i = 0; i < color.Length - width; i = i + width)
                color[i] = tertiaryColor;

            // left
            for (int i = width - 1; i < color.Length; i = i + width)
                color[i] = tertiaryColor;

            Texture.SetData(color);

            return Texture;
        }

        public static float AngleFromVector(Vector2 vector)
        {
            return (float)Math.Atan2(vector.Y, vector.X);
        }

        public static Vector2 VectorFromLengthAndAngle(float length, float angle)
        {
            return new Vector2(length * (float)Math.Cos(angle), length * (float)Math.Sin(angle));
        }
    }
}
