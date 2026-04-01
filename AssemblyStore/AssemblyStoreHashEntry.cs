using System;
using System.IO;

namespace Xamarin.Android.AssemblyStore
{
    public class AssemblyStoreHashEntry
    {
        public bool Is32Bit { get; }

        public ulong Hash { get; }
        public uint MappingIndex { get; }
        public uint LocalStoreIndex { get; }
        public uint StoreID { get; }

        internal AssemblyStoreHashEntry(BinaryReader reader, bool is32Bit)
        {
            Is32Bit = is32Bit;

            Hash = reader.ReadUInt64();
            MappingIndex = reader.ReadUInt32();
            LocalStoreIndex = reader.ReadUInt32();
            StoreID = reader.ReadUInt32();
        }

        /// <summary>V2 format: 12-byte entries (hash64 + mapping_index32)</summary>
        internal AssemblyStoreHashEntry(BinaryReader reader, uint storeVersion)
        {
            Is32Bit = false;
            Hash = reader.ReadUInt64();
            MappingIndex = reader.ReadUInt32();
            LocalStoreIndex = MappingIndex;
            StoreID = 0;
        }
    }
}
