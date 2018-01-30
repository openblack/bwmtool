using System;
using System.IO;

namespace BWMTool
{
    /// <summary>
    /// Represents a Black & White 2 model.
    /// </summary>
    public class BWM
    {
        #region Constants
        private const String MagicFileIdentifier = "LiOnHeAdMODEL";
        private const UInt32 MagicNumberIdentifier = 0x2B00B1E5;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filepath">Path to the BWM file.</param>
        public BWM(string filepath)
        {
            if (filepath == null)
                throw new ArgumentNullException(nameof(filepath));

            if (!File.Exists(filepath))
                throw new FileNotFoundException($"File {filepath} does not exist.", filepath);

            using (var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                ReadBWM(fs);
            }
        }

        public BWM(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            ReadBWM(stream);
        }
        #endregion

        #region Enums
        /// <summary>
        /// File format versions
        /// </summary>
        public enum BWMVersion
        {
            /// <summary>
            /// Version type 5, least common.
            /// </summary>
            BWM5 = 5,
            /// <summary>
            /// Version type 6, most common.
            /// </summary>
            BWM6 = 6
        }
        #endregion

        #region Structs

        /// <summary>
        /// Represents what BW2 calls a ModelHeader in a BWM.
        /// </summary>
        /*public struct ModelHeader
        {
            public UInt32 MaterialDefinitionCount;
            public UInt32 MeshDescriptionCount;
            public UInt32 BoneCount;
            public UInt32 EntityCount;
            public UInt32 Something4Count;
            public UInt32 Something5Count;
            public UInt32 VertexCount;
            public UInt32 StrideCount;
            public UInt32 IndexCount;
        }*/

        /// <summary>
        /// Represents a material definition within a BWM.
        /// </summary>
        public struct MaterialDefinition
        {
            public String DiffuseMap { get; internal set; }
            public String LightMap { get; internal set; }
            public String Unknown3 { get; internal set; }
            public String SpecularMap { get; internal set; }
            public String Unknown5 { get; internal set; }
            public String NormalMap { get; internal set; }
            public String Type { get; internal set; }
        }

        /// <summary>
        /// Represents a mesh description within a BWM.
        /// </summary>
        public struct MeshDescription
        {
            public UInt32 ID;
            public String Name;

            public UInt32 FacesCount;
            public UInt32 IndiciesPointer;

            public MaterialRef[] MaterialRefs;

            /// <summary>
            /// Represents a reference to a material within a mesh.
            /// </summary>
            public struct MaterialRef
            {
                /// <summary>The corresponding MaterialDefinition within this BWM.</summary>
                public UInt32 MaterialDefinition;
                /// <summary>The offset in the BWM indicies array to start from.</summary>
                public UInt32 IndiciesOffset;
                /// <summary>The number of indicies to draw.</summary>
                public UInt32 IndiciesSize;
                public UInt32 VertexOffset;
                public UInt32 VertexSize;
                public UInt32 FacesOffset;
                public UInt32 FacesSize;
                public UInt32 Unknown;
            }
        }

        /// <summary>
        /// Represents a bone in a BWM.
        /// </summary>
        public struct Bone
        {

        }

        /// <summary>
        /// Represents an entity in a BWM.
        /// </summary>
        public struct Entity
        {
            public String Name;
            public LHPoint Position;
            public LHPoint Unknown1;
            public LHPoint Unknown2;
            public LHPoint Unknown3;
        }

        public struct Vertex
        {
            public LHPoint Position;
            public LHPoint Normal;
            public Single U;
            public Single V;
            public Single U1;
            public Single U2;
            public Single U3;
            public Single U4;
        }

        #endregion

        #region Properties

        /// <summary>
        /// File format version, either BWM5 or BWM6.
        /// </summary>
        public BWMVersion FileFormatVersion { get; private set; }

        public UInt32 NotHeaderSize { get; private set; }

        /// <summary>
        /// The material definitions within this BWM.
        /// </summary>
        public MaterialDefinition[] MaterialDefinitions
        {
            get;
            private set;
        }

        /// <summary>
        /// The mesh descriptions within this BWM.
        /// </summary>
        public MeshDescription[] MeshDescriptions
        {
            get;
            private set;
        }

        /// <summary>
        /// The bones within this BWM.
        /// </summary>
        public Bone[] Bones
        {
            get;
            private set;
        }

        /// <summary>
        /// The entities within this BWM.
        /// </summary>
        public Entity[] Entities
        {
            get;
            private set;
        }

        /// <summary>
        /// The strides within this BWM.
        /// </summary>
        public UInt32[] Strides;

        /// <summary>
        /// The indicies within this BWM.
        /// </summary>
        public UInt16[] Indices;

        /// <summary>
        /// The verticies byte array(?) within this BWM.
        /// </summary>
        public Vertex[] Verticies;

        /// <summary>
        /// Cleave points within this BWM. Only if FileFormatVersion == BWM6.
        /// </summary>
        public LHPoint[] ModelCleaves;
        
        #endregion

        #region Private Methods

        private void ReadBWM(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (new string(reader.ReadChars(13)) != MagicFileIdentifier)
                    throw new ArgumentException("This file is not a Lionhead Model file. (Magic String mismatch)");
                reader.BaseStream.Position += 27;

                // this should be equal to stream.Length - 44. It tells us how much of the file is left in bytes.
                var size = reader.ReadInt32();

                if (reader.ReadUInt32() != MagicNumberIdentifier)
                    throw new ArgumentException("This file is not a Lionhead Model file. (Magic Header mismatch)");

                var fileFormatVersion = reader.ReadUInt32();
                if (fileFormatVersion != 5 && fileFormatVersion != 6)
                    throw new ArgumentException("This file format version is not supported.");

                FileFormatVersion = (BWMVersion)fileFormatVersion;
                NotHeaderSize = reader.ReadUInt32();


                // <ModelHeader>

                reader.ReadBytes(68);
                // uint32 0, 0, 0 12
                // 7 floats 28


                MaterialDefinitions = new MaterialDefinition[reader.ReadUInt32()]; // 68
                MeshDescriptions = new MeshDescription[reader.ReadUInt32()]; // 72
                Bones = new Bone[reader.ReadUInt32()]; // 76
                Entities = new Entity[reader.ReadUInt32()]; // 80

                var numSomething4 = reader.ReadUInt32(); // 84
                var numSomething5 = reader.ReadUInt32(); // 88
                reader.ReadBytes(20); // 92
                var numVerticies = reader.ReadUInt32(); // 112
                var numStrides = reader.ReadUInt32(); // 116
                reader.ReadUInt32(); // 120 (*((_DWORD *)v75[66] + 30) = 2;)
                Indices = new UInt16[reader.ReadUInt32()];

                // </ModelHeader>
                
                for (int i = 0; i < MaterialDefinitions.Length; i++)
                    MaterialDefinitions[i] = ReadMaterialDefinition(reader);
                for (int i = 0; i < MeshDescriptions.Length; i++)
                    MeshDescriptions[i] = ReadMeshDescription(reader);
                for (int i = 0; i < MeshDescriptions.Length; i++)
                    for (int j = 0; j < MeshDescriptions[i].MaterialRefs.Length; j++)
                        MeshDescriptions[i].MaterialRefs[j] = ReadMeshMaterialReference(reader);
                for (int i = 0; i < Bones.Length; i++)
                    reader.BaseStream.Position += 48; // todo
                for (int i = 0; i < Entities.Length; i++)
                    Entities[i] = ReadEntity(reader);
                for (int i = 0; i < numSomething4; i++)
                    reader.BaseStream.Position += 12; // todo (unknown) CircleFootprint
                for (int i = 0; i < numSomething5; i++)
                    reader.BaseStream.Position += 12; // todo (unknown)
                
                var strides = new byte[numStrides][];
                for (int i = 0; i < numStrides; i++)
                {
                    var stride = reader.ReadBytes(136);
                    strides[i] = stride;
                }

                Verticies = new Vertex[numVerticies];

                for (int i = 0; i < numStrides; i++)
                {
                    if (i == 1)
                        throw new ArgumentException("Stride count of more then 1 not supported yet.");

                    var stride = Engine.StrideInBytesFromStreamDef(new MemoryStream(strides[i]));
                    var totalSize = numVerticies * stride;

                    //if (stride != 32)
                    //    Console.WriteLine("unknown stride uhoh {0}", stride);

                    for (int j = 0; j < numVerticies; j++)
                    {

                        Verticies[j] = new Vertex
                        {
                            Position = new LHPoint(reader),
                            Normal = new LHPoint(reader),
                            U = reader.ReadSingle(),
                            V = reader.ReadSingle(),
                        };

                        if (stride > 32)
                            reader.BaseStream.Position += (stride - 32);

                    }
                }

                for (int i = 0; i < Indices.Length; i++)
                    Indices[i] = reader.ReadUInt16();

                if (FileFormatVersion != BWMVersion.BWM6)
                    return;

                ModelCleaves = new LHPoint[reader.ReadUInt32()];
                for (int i = 0; i < ModelCleaves.Length; i++)
                    ModelCleaves[i] = new LHPoint(reader);
            }
        }

        private MaterialDefinition ReadMaterialDefinition(BinaryReader reader)
        {
            return new MaterialDefinition
            {
                DiffuseMap = Util.ReadNullTerminatedString(reader, 64),
                LightMap = Util.ReadNullTerminatedString(reader, 64),
                Unknown3 = Util.ReadNullTerminatedString(reader, 64),
                SpecularMap = Util.ReadNullTerminatedString(reader, 64),
                Unknown5 = Util.ReadNullTerminatedString(reader, 64),
                NormalMap = Util.ReadNullTerminatedString(reader, 64),
                Type = Util.ReadNullTerminatedString(reader, 64)
            };
        }

        private Entity ReadEntity(BinaryReader reader)
        {
            return new Entity
            {
                Unknown1 = new LHPoint(reader),
                Unknown2 = new LHPoint(reader),
                Unknown3 = new LHPoint(reader),
                Position = new LHPoint(reader),
                Name = Util.ReadNullTerminatedString(reader, 256)
            };
        }

        private MeshDescription ReadMeshDescription(BinaryReader reader)
        {
            var facesCount = reader.ReadUInt32();
            var indiciesOffset = reader.ReadUInt32();

            reader.BaseStream.Position += 124; // IDK
            var unknown1 = reader.ReadUInt32();
            var numMaterialRefs = reader.ReadUInt32();
            var U2 = reader.ReadUInt32();
            var ID = reader.ReadUInt32();
            var name = Util.ReadNullTerminatedString(reader, 64);
            reader.ReadUInt32();
            reader.ReadUInt32();

            return new MeshDescription
            {
                Name = name,
                MaterialRefs = new MeshDescription.MaterialRef[numMaterialRefs]
            };
        }

        private MeshDescription.MaterialRef ReadMeshMaterialReference(BinaryReader reader)
        {
            return new MeshDescription.MaterialRef
            {
                MaterialDefinition = reader.ReadUInt32(),
                IndiciesOffset = reader.ReadUInt32(),
                IndiciesSize = reader.ReadUInt32(),
                VertexOffset = reader.ReadUInt32(),
                VertexSize = reader.ReadUInt32(),
                FacesOffset = reader.ReadUInt32(),
                FacesSize = reader.ReadUInt32(),
                Unknown = reader.ReadUInt32()
            };
        }

    #endregion
    }
}
