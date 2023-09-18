using System;
using System.Collections.Generic;
using System.IO;

namespace TSMapEditor.CCEngine
{
    public class ShpLoadException : Exception
    {
        public ShpLoadException(string message) : base(message)
        {
        }
    }

    [Flags]
    public enum ShpCompression
    {
        None = 0,
        HasTransparency = 1,
        UsesRle = 2
    }

    /// <summary>
    /// Represents the header of a SHP file.
    /// </summary>
    struct ShpFileHeader
    {
        public const int SizeOf = 8;

        public ShpFileHeader(byte[] buffer)
        {
            if (buffer.Length < SizeOf)
                throw new ShpLoadException(nameof(ShpFileHeader) + ": buffer is not long enough");

            Unknown = BitConverter.ToUInt16(buffer, 0);
            if (Unknown != 0)
                throw new ShpLoadException("Unexpected field value in SHP header");

            SpriteWidth = BitConverter.ToUInt16(buffer, 2);
            SpriteHeight = BitConverter.ToUInt16(buffer, 4);
            FrameCount = BitConverter.ToUInt16(buffer, 6);
        }

        public ushort Unknown;
        public ushort SpriteWidth;
        public ushort SpriteHeight;
        public ushort FrameCount;
    }

    /// <summary>
    /// Represents the information of a single frame in a SHP file.
    /// </summary>
    public class ShpFrameInfo
    {
        private const int SizeOf = 24;

        public ShpFrameInfo(Stream stream)
        {
            if (stream.Length < stream.Position + SizeOf)
                throw new ShpLoadException(nameof(ShpFrameInfo) + ": buffer is not long enough");

            XOffset = ReadUShortFromStream(stream);
            YOffset = ReadUShortFromStream(stream);
            Width = ReadUShortFromStream(stream);
            Height = ReadUShortFromStream(stream);
            Flags = (ShpCompression)ReadUIntFromStream(stream);
            byte r = (byte)stream.ReadByte();
            byte g = (byte)stream.ReadByte();
            byte b = (byte)stream.ReadByte();
            AverageColor = new RGBColor(r, g, b);
            Unknown1 = (byte)stream.ReadByte();
            Unknown2 = ReadUIntFromStream(stream);
            DataOffset = ReadUIntFromStream(stream);
        }

        private ushort ReadUShortFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 2);
            return BitConverter.ToUInt16(buffer, 0);
        }

        private uint ReadUIntFromStream(Stream stream)
        {
            stream.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }

        byte[] buffer = new byte[4];

        public ushort XOffset;
        public ushort YOffset;
        public ushort Width;
        public ushort Height;
        public ShpCompression Flags;
        public RGBColor AverageColor;
        public byte Unknown1;
        public uint Unknown2;
        public uint DataOffset;
    }

    /// <summary>
    /// Represents a SHP file. Combines the header and frame information
    /// and makes it possible to parse the actual graphical data.
    /// </summary>
    public class ShpFile
    {
        public ShpFile() { }

        public ShpFile(string fileName)
        {
            this.fileName = fileName;
        }

        private readonly string fileName;

        private ShpFileHeader shpFileHeader;
        private List<ShpFrameInfo> shpFrameInfos;

        

        public int FrameCount => shpFrameInfos.Count;

        public int Width => shpFileHeader.SpriteWidth;
        public int Height => shpFileHeader.SpriteHeight;

        public void ParseFromFile(string filePath)
        {
            using (FileStream stream = File.OpenRead(filePath))
            {
                Parse(stream);
            }
        }

        public void Parse(Stream stream)
        {
            byte[] buffer = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(buffer, 0, buffer.Length);
            ParseFromBuffer(buffer);
        }

        public void ParseFromBuffer(byte[] buffer)
        {
            try
            {
                shpFileHeader = new ShpFileHeader(buffer);
                shpFrameInfos = new List<ShpFrameInfo>(shpFileHeader.FrameCount);

                using (var memoryStream = new MemoryStream(buffer))
                {
                    memoryStream.Position = ShpFileHeader.SizeOf;

                    for (int i = 0; i < shpFileHeader.FrameCount; i++)
                    {
                        var shpFrameInfo = new ShpFrameInfo(memoryStream);
                        shpFrameInfos.Add(shpFrameInfo);
                    }
                }
            }
            catch (ShpLoadException ex)
            {
                throw new ShpLoadException("Failed to load SHP file. Make sure that the file is not corrupted. Filename: " + fileName + ", original exception: " + ex.Message);
            }
        }

        public ShpFrameInfo GetShpFrameInfo(int frameIndex) => shpFrameInfos[frameIndex];

        public byte[] GetUncompressedFrameData(int frameIndex, byte[] fileData)
        {
            ShpFrameInfo frameInfo = shpFrameInfos[frameIndex];

            if (frameInfo.DataOffset == 0)
                return null;

            byte[] frameData = new byte[frameInfo.Width * frameInfo.Height];
            if ((frameInfo.Flags & ShpCompression.UsesRle) == ShpCompression.None)
            {
                for (int i = 0; i < frameData.Length; i++)
                {
                    frameData[i] = fileData[frameInfo.DataOffset + i];
                }
            }
            else
            {
                // Decompression of RLE-Zero data by Nyerguds
                // Taken from Engie File Converter
                // http://nyerguds.arsaneus-design.com/project_stuff/2018/EngieFileConverter/

                int offset = (int)frameInfo.DataOffset;
                int dataLength = fileData.Length;
                int outLineOffset = 0;
                for (int y = 0; y < frameInfo.Height; ++y)
                {
                    int outOffset = outLineOffset;
                    int nextLineOffset = outLineOffset + frameInfo.Width;
                    if (offset + 2 >= dataLength)
                        throw new ShpLoadException("Not enough lines in RLE-Zero data!");
                    // Compose little-endian UInt16 from 2 bytes
                    int lineLen = fileData[offset] | (fileData[offset + 1] << 8);
                    int end = offset + lineLen;
                    if (lineLen < 2 || end > dataLength)
                        throw new ShpLoadException("Bad value in RLE-Zero line header!");
                    // Skip header
                    offset += 2;
                    bool readZero = false;
                    for (; offset < end; ++offset)
                    {
                        if (outOffset >= nextLineOffset)
                            throw new ShpLoadException("Bad line alignment in RLE-Zero data!");
                        if (readZero)
                        {
                            // Zero has been read. Process 0-repeat.
                            readZero = false;
                            int zeroes = fileData[offset];
                            for (; zeroes > 0 && outOffset < nextLineOffset; zeroes--)
                                frameData[outOffset++] = 0;
                        }
                        else if (fileData[offset] == 0)
                        {
                            // Rather than manually increasing the offset, just flag that
                            // "a 0 value has been read" so the next loop can read the repeat value.
                            readZero = true;
                        }
                        else
                        {
                            // Simply copy a value.
                            frameData[outOffset++] = fileData[offset];
                        }
                    }
                    // If a data line ended on a 0, there's something wrong.
                    if (readZero)
                        throw new ShpLoadException("Incomplete zero-repeat command!");
                    outLineOffset = nextLineOffset;
                }
            }

            return frameData;
        }
    }


}
