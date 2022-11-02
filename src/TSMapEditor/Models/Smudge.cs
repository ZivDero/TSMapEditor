﻿namespace TSMapEditor.Models
{
    public class Smudge : GameObject
    {
        public override RTTIType WhatAmI() => RTTIType.Smudge;

        public SmudgeType SmudgeType { get; set; }

        public override int GetYDrawOffset()
        {
            return Constants.CellSizeY / -2;
        }
    }
}
