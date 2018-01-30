using System;
using System.Globalization;
using System.IO;

namespace BWMTool
{
    static class Engine
    {
        public static uint StrideInBytesFromFormat(uint format)
        {
            return new uint[] { 4, 8, 12, 4, 1 }[format];
        }

        public static uint StrideInBytesFromStreamDef(Stream stream)
        {
            var reader = new BinaryReader(stream);
            var count = reader.ReadUInt32();
            
            if (count == 0)
                return 0;

            uint totalStride = 0;
            for (int i = 0; i < count; i++)
            {
                var stride_id = reader.ReadUInt32(); // id
                var stride_size = StrideInBytesFromFormat(reader.ReadUInt32()); // size
                
                totalStride += stride_size;
            }

            return totalStride;
        }
    }

    static class Util
    {
        public static string ReadNullTerminatedString(BinaryReader reader, int size)
        {
            string result = null;

            unsafe
            {
                byte[] stringArray = reader.ReadBytes(size);

                fixed (byte* pAscii = &stringArray[0])
                {
                    result = new String((sbyte*)pAscii, 0, size);
                }
            }

            return result.TrimEnd('\0');
        }
    }

    public struct LHPoint
    {
        public Single X;
        public Single Y;
        public Single Z;

        public LHPoint(Single x, Single y, Single z)
        {
            X = x; Y = y; Z = z;
        }

        public LHPoint(BinaryReader reader)
        {
            X = reader.ReadSingle();
            Y = reader.ReadSingle();
            Z = reader.ReadSingle();
        }

        public override string ToString()
        {
            CultureInfo currentCulture = CultureInfo.CurrentCulture;
            return string.Format((IFormatProvider)currentCulture, "{{X:{0} Y:{1} Z:{2}}}", (object)this.X.ToString((IFormatProvider)currentCulture), (object)this.Y.ToString((IFormatProvider)currentCulture), (object)this.Z.ToString((IFormatProvider)currentCulture));
        }
    }
}
