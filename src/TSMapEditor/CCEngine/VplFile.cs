using System;
using System.Collections.Generic;
using System.IO;
using CNCMaps.FileFormats.VirtualFileSystem;

namespace CNCMaps.FileFormats
{

    public class VplFile : VirtualFile
    {
        public VplFile(Stream baseStream, string filename, int baseOffset, int fileSize, bool isBuffered = false)
            : base(baseStream, filename, baseOffset, fileSize, isBuffered) { }

        public VplFile(Stream baseStream, string filename = "", bool isBuffered = true)
            : base(baseStream, filename, isBuffered) { }

        private uint firstRemap;
        private uint lastRemap;
        private uint numSections;
        private uint unknown;
        // private Palette _palette; // unused
        private List<byte[]> lookupSections = new List<byte[]>();

        private bool parsed = false;
        private void Parse()
        {
            firstRemap = ReadUInt32();
            lastRemap = ReadUInt32();
            numSections = ReadUInt32();
            unknown = ReadUInt32();
            var pal = Read(768);
            // palette = new Palette(pal, "voxels.vpl");
            for (uint i = 0; i < numSections; i++)
                lookupSections.Add(Read(256));
            parsed = true;
        }

        public byte GetPaletteIndex(byte normal, byte maxNormal, byte color)
        {
            if (!parsed) Parse();
            int vplSection = (int)(Math.Min(normal, maxNormal - 1) * numSections / maxNormal);
            return lookupSections[vplSection][color];
        }

    }
}