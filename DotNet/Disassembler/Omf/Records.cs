﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.IO;

namespace Disassembler.Omf
{
    public enum RecordNumber : byte
    {
        None = 0,

        /// <summary>Translator Header Record</summary>
        THEADR = 0x80,

        /// <summary>Library Module Header Record</summary>
        LHEADR = 0x82,

        /// <summary>Comment Record (with extensions)</summary>
        COMENT = 0x88,

        /// <summary>Module End Record (16-bit)</summary>
        MODEND = 0x8A,

        /// <summary>Module End Record (32-bit)</summary>
        MODEND32 = 0x8B,

        /// <summary>External Names Definition Record</summary>
        EXTDEF = 0x8C,

        /// <summary>Public Names Definition Record (16-bit)</summary>
        PUBDEF = 0x90,

        /// <summary>Public Names Definition Record (32-bit)</summary>
        PUBDEF32 = 0x91,

        /// <summary>Line Numbers Record (16-bit)</summary>
        LINNUM = 0x94,

        /// <summary>Line Numbers Record (32-bit)</summary>
        LINNUM32 = 0x95,

        /// <summary>List of Names Record</summary>
        LNAMES = 0x96,

        /// <summary>Segment Definition Record (16-bit)</summary>
        SEGDEF = 0x98,

        /// <summary>Segment Definition Record (32-bit)</summary>
        SEGDEF32 = 0x99,

        /// <summary>Group Definition Record</summary>
        GRPDEF = 0x9A,

        /// <summary>Fixup Record (16-bit)</summary>
        FIXUPP = 0x9C,

        /// <summary>Fixup Record (32-bit)</summary>
        FIXUPP32 = 0x9D,

        /// <summary>Logical Enumerated Data Record (16-bit)</summary>
        LEDATA = 0xA0,

        /// <summary>Logical Enumerated Data Record (32-bit)</summary>
        LEDATA32 = 0xA1,

        /// <summary>Logical Iterated Data Record (16-bit)</summary>
        LIDATA = 0xA2,

        /// <summary>Logical Iterated Data Record (32-bit)</summary>
        LIDATA32 = 0xA3,

        /// <summary>Communal Names Definition Record</summary>
        COMDEF = 0xB0,

        /// <summary>Backpatch Record (16-bit)</summary>
        BAKPAT = 0xB2,

        /// <summary>Backpatch Record (32-bit)</summary>
        BAKPAT32 = 0xB3,

        /// <summary>Local External Names Definition Record (32-bit)</summary>
        LEXTDEF = 0xB4,

        /// <summary>Local External Names Definition Record (32-bit)</summary>
        LEXTDEF32 = 0xB5,

        /// <summary>Local Public Names Definition Record (16-bit)</summary>
        LPUBDEF = 0xB6,

        /// <summary>Local Public Names Definition Record (32-bit)</summary>
        LPUBDEF32 = 0xB7,

        /// <summary>Local Communal Names Definition Record</summary>
        LCOMDEF = 0xB8,

        /// <summary>COMDAT External Names Definition Record</summary>
        CEXTDEF = 0xBC,

        /// <summary>Initialized Communal Data Record (16-bit)</summary>
        COMDAT = 0xC2,

        /// <summary>Initialized Communal Data Record (32-bit)</summary>
        COMDAT32 = 0xC3,

        /// <summary>Symbol Line Numbers Record (16-bit)</summary>
        LINSYM = 0xC4,

        /// <summary>Symbol Line Numbers Record (32-bit)</summary>
        LINSYM32 = 0xC5,

        /// <summary>Alias Definition Record</summary>
        ALIAS = 0xC6,

        /// <summary>Named Backpatch Record (16-bit)</summary>
        NBKPAT = 0xC8,

        /// <summary>Named Backpatch Record (32-bit)</summary>
        NBKPAT32 = 0xC9,

        /// <summary>Local Logical Names Definition Record</summary>
        LLNAMES = 0xCA,

        /// <summary>OMF Version Number Record</summary>
        VERNUM = 0xCC,

        /// <summary>Vendor-specific OMF Extension Record</summary>
        VENDEXT = 0xCE,

        /// <summary>Library Header Record</summary>
        LibraryHeader = 0xF0,

        /// <summary>Library End Record</summary>
        LibraryEnd = 0xF1,
    }

    /// <summary>
    /// Provides methods to read the fields of an OMF record.
    /// </summary>
    internal class RecordReader
    {
        public RecordNumber RecordNumber { get; private set; }
        public byte[] Data { get; private set; }
        public byte Checksum { get; private set; }
        public int Position { get; private set; }

        private int index; // currentIndex

        public RecordReader(BinaryReader reader)
        {
            this.Position = (int)reader.BaseStream.Position;
            this.RecordNumber = (RecordNumber)reader.ReadByte();

            int recordLength = reader.ReadUInt16();
            if (recordLength == 0)
                throw new InvalidDataException("RecordLength must be greater than zero.");

            this.Data = reader.ReadBytes(recordLength - 1);
            if (Data.Length != recordLength - 1)
                throw new EndOfStreamException("Cannot read enough bytes.");

            this.Checksum = reader.ReadByte();
        }

        public bool IsEOF
        {
            get { return index == Data.Length; }
        }

        public byte PeekByte()
        {
            if (index >= Data.Length)
                throw new InvalidDataException();
            return Data[index];
        }

        public byte ReadByte()
        {
            if (index >= Data.Length)
                throw new InvalidDataException();
            return Data[index++];
        }

        public byte[] ReadToEnd()
        {
            byte[] remaining = new byte[Data.Length - index];
            Array.Copy(Data, index, remaining, 0, remaining.Length);
            index = Data.Length;
            return remaining;
        }

        public string ReadToEndAsString()
        {
            string s = Encoding.ASCII.GetString(Data, index, Data.Length - index);
            index = Data.Length;
            return s;
        }

        public UInt16 ReadUInt16()
        {
            if (index + 2 > Data.Length)
                throw new InvalidDataException();
            byte b1 = Data[index++];
            byte b2 = Data[index++];
            return (UInt16)(b1 | (b2 << 8));
        }

        public UInt16 ReadUInt24()
        {
            if (index + 3 > Data.Length)
                throw new InvalidDataException();
            byte b1 = Data[index++];
            byte b2 = Data[index++];
            byte b3 = Data[index++];
            return (UInt16)(b1 | (b2 << 8) | (b3 << 16));
        }

        public UInt32 ReadUInt32()
        {
            if (index + 4 > Data.Length)
                throw new InvalidDataException();
            byte b1 = Data[index++];
            byte b2 = Data[index++];
            byte b3 = Data[index++];
            byte b4 = Data[index++];
            return (UInt32)(b1 | (b2 << 8) | (b3 << 16) | (b4 << 24));
        }

        /// <summary>
        /// Reads UInt16 if the record number is even, or UInt32 if the
        /// record number is odd.
        /// </summary>
        /// <returns></returns>
        public UInt32 ReadUInt16Or32()
        {
            if (((int)RecordNumber & 1) == 0)
                return ReadUInt16();
            else
                return ReadUInt32();
        }

        /// <summary>
        /// Reads a string encoded as an 8-bit unsigned 'count' followed by
        /// 'count' bytes of string data.
        /// </summary>
        public string ReadPrefixedString()
        {
            if (index >= Data.Length)
                throw new InvalidDataException();

            byte len = Data[index++];
            if (len == 0)
                return "";

            if (index + len > Data.Length)
                throw new InvalidDataException();

            string s = Encoding.ASCII.GetString(Data, index, len);
            index += len;
            return s;
        }

        /// <summary>
        /// Reads an index in the range [0, 0x7FFF], encoded by 1 or 2 bytes.
        /// </summary>
        public UInt16 ReadIndex()
        {
            if (index >= Data.Length)
                throw new InvalidDataException();

            byte b1 = Data[index++];
            if ((b1 & 0x80) == 0)
            {
                return b1;
            }
            else
            {
                if (index >= Data.Length)
                    throw new InvalidDataException();

                byte b2 = Data[index++];
                return (UInt16)(((b1 & 0x7F) << 8) | b2);
            }
        }
    }

    internal class RecordContext
    {
        public readonly List<string> Names = new List<string>();

        // FRAME threads.
        public readonly ThreadDefinition[] FrameThreads = new ThreadDefinition[4];

        // TARGET threads.
        public readonly ThreadDefinition[] TargetThreads = new ThreadDefinition[4];

        // Contains the last record.
        public Record LastRecord = null;

        public ObjectModule Module { get; private set; }

        internal RecordContext(ObjectModule module)
        {
            if (module == null)
                throw new ArgumentNullException("module");
            this.Module = module;
        }
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class Record
    {
        [Browsable(false)]
        public RecordNumber RecordNumber { get; private set; }

        /// <summary>
        /// Gets the position of the record expressed as a byte offset to the
        /// beginning of the OBJ or LIB file.
        /// </summary>
        [Browsable(false)]
        public int Position { get; private set; }

        internal Record(RecordReader reader, RecordContext context)
        {
            this.Position = reader.Position;
            this.RecordNumber = reader.RecordNumber;
        }

        internal static Record ReadRecord(BinaryReader binaryReader, RecordContext context)
        {
            return ReadRecord(binaryReader, context, RecordNumber.None);
        }

        internal static Record ReadRecord(
            BinaryReader binaryReader, 
            RecordContext context,
            RecordNumber expectedRecord)
        {
            RecordReader reader = new RecordReader(binaryReader);
            if (expectedRecord != RecordNumber.None &&
                reader.RecordNumber != expectedRecord)
            {
                throw new InvalidDataException(string.Format(
                    "Expecting record {0}, but got record {1}.",
                    expectedRecord, reader.RecordNumber));
            }

            Record r;
            switch (reader.RecordNumber)
            {
                case RecordNumber.LibraryHeader:
                    r = new LibraryHeaderRecord(reader, context);
                    break;
                case RecordNumber.LibraryEnd:
                    r = new LibraryEndRecord(reader, context);
                    break;
                case RecordNumber.ALIAS:
                    r = new AliasDefinitionRecord(reader, context);
                    break;
                case RecordNumber.CEXTDEF:
                    r = new COMDATExternalNamesDefinitionRecord(reader, context);
                    break;
                case RecordNumber.COMDAT:
                case RecordNumber.COMDAT32:
                    r = new InitializedCommunalDataRecord(reader, context);
                    break;
                case RecordNumber.COMDEF:
                    r = new CommunalNamesDefinitionRecord(reader, context);
                    break;
                case RecordNumber.COMENT:
                    r = new CommentRecord(reader, context);
                    break;
                case RecordNumber.EXTDEF:
                    r = new ExternalNamesDefinitionRecord(reader, context);
                    break;
                case RecordNumber.FIXUPP:
                case RecordNumber.FIXUPP32:
                    r = new FixupRecord(reader, context);
                    break;
                case RecordNumber.GRPDEF:
                    r = new GroupDefinitionRecord(reader, context);
                    break;
                case RecordNumber.LEDATA:
                case RecordNumber.LEDATA32:
                    r = new LogicalEnumeratedDataRecord(reader, context);
                    break;
                case RecordNumber.LEXTDEF:
                case RecordNumber.LEXTDEF32:
                    r = new LocalExternalNamesDefinitionRecord(reader, context);
                    break;
                case RecordNumber.LHEADR:
                    r = new LibraryModuleHeaderRecord(reader, context);
                    break;
                case RecordNumber.LIDATA:
                case RecordNumber.LIDATA32:
                    r = new LogicalIteratedDataRecord(reader, context);
                    break;
                case RecordNumber.LNAMES:
                    r = new ListOfNamesRecord(reader, context);
                    break;
                case RecordNumber.LPUBDEF:
                case RecordNumber.LPUBDEF32:
                    r = new LocalPublicNameDefinitionRecord(reader, context);
                    break;
                case RecordNumber.MODEND:
                    r = new ModuleEndRecord(reader, context);
                    break;
                case RecordNumber.PUBDEF:
                case RecordNumber.PUBDEF32:
                    r = new PublicNamesDefinitionRecord(reader, context);
                    break;
                case RecordNumber.SEGDEF:
                case RecordNumber.SEGDEF32:
                    r = new SegmentDefinitionRecord(reader, context);
                    break;
                case RecordNumber.THEADR:
                    r = new TranslatorHeaderRecord(reader, context);
                    break;
                default:
                    r = new UnknownRecord(reader, context);
                    break;
            }

            // TODO: check all bytes are consumed.
            // ...

            // Update RecordContext.LastRecord. This is necessary so that
            // a FIXUPP record knows which record to fix up.
            if (context != null)
            {
                context.LastRecord = r;
            }

            return r;
        }

        public override string ToString()
        {
            return string.Format("{0} @ {1:X}", RecordNumber, Position);
        }
    }

    public class UnknownRecord : Record
    {
        public byte[] Data { get; private set; }

        internal UnknownRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            this.Data = reader.Data;
        }

        public override string ToString()
        {
            return string.Format("? {0} @ {1:X}", RecordNumber, Position);
        }
    }

    public class LibraryHeaderRecord : Record
    {
        public int PageSize { get; private set; }

        internal LibraryHeaderRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            this.PageSize = reader.Data.Length + 4;

            // Record data consists of 7 bytes of dictionary information
            // (which we ignore), followed by padding bytes to make the next
            // record (which should be THEADR) aligned on page boundary.
        }
    }

    public class LibraryEndRecord : Record
    {
        internal LibraryEndRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            // Record data serves as padding to align the dictionary that
            // follows at 512-byte boundary.
        }
    }

    /// <summary>
    /// Contains the name of the object module.
    /// </summary>
    public class TranslatorHeaderRecord : Record
    {
        public string Name { get; private set; }

        internal TranslatorHeaderRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            this.Name = reader.ReadPrefixedString();
            context.Module.SourceName = Name;
        }
    }

    /// <summary>
    /// Contains the name of a module within a library file.
    /// </summary>
    public class LibraryModuleHeaderRecord : Record
    {
        public string Name { get; private set; }

        internal LibraryModuleHeaderRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            this.Name = reader.ReadPrefixedString();
        }
    }

    /// <summary>
    /// Denotes the end of an object module.
    /// </summary>
    public class ModuleEndRecord : Record
    {
        public bool IsMainModule { get; private set; }
        public bool HasStartAddress { get; private set; }
        public bool IsStartAddressRelocatable { get; private set; }

        internal ModuleEndRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            byte type = reader.ReadByte();
            this.IsMainModule = (type & 0x80) != 0;
            this.HasStartAddress = (type & 0x40) != 0;
            this.IsStartAddressRelocatable = (type & 0x01) != 0;

            // TODO: other fields...
        }
    }

    #region External Name Related Records

    /// <summary>
    /// Contains a list of symbolic external references, i.e. references to
    /// symbols defined in other object modules.
    /// </summary>
    /// <remarks>
    /// EXTDEF names are ordered by occurrence jointly with the COMDEF and
    /// LEXTDEF records, and referenced by an index in FIXUPP records.
    /// </remarks>
    public class ExternalNamesDefinitionRecord : Record
    {
        public ExternalNameDefinition[] Symbols { get; private set; }

        internal ExternalNamesDefinitionRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            List<ExternalNameDefinition> symbols = new List<ExternalNameDefinition>();
            while (!reader.IsEOF)
            {
                ExternalNameDefinition symbol = new ExternalNameDefinition();
                symbol.Name = reader.ReadPrefixedString();
                symbol.TypeIndex = reader.ReadIndex();
                symbols.Add(symbol);
            }
            this.Symbols = symbols.ToArray();

            context.Module.externalNames.AddRange(Symbols);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// LEXTDEF records are associated with LPUBDEF and LCOMDEF records and
    /// ordered with EXTDEF and COMDEF records by occurrence, so that they
    /// may be referenced by an external name index for fixups.
    /// </remarks>
    public class LocalExternalNamesDefinitionRecord : ExternalNamesDefinitionRecord
    {
        internal LocalExternalNamesDefinitionRecord(
            RecordReader reader, RecordContext context)
            : base(reader, context)
        {
        }
    }

    /// <summary>
    /// Declares a list of communal variables (uninitialized static data or
    /// data that may match initialized static data in another compilation
    /// unit).
    /// </summary>
    /// <remarks>
    /// COMDEF records are ordered by occurrence, together with the items
    /// named in EXTDEF and LEXTDEF records, for reference in FIXUP records.
    /// </remarks>
    public class CommunalNamesDefinitionRecord : Record
    {
        public CommunalNameDefinition[] Definitions { get; private set; }

        internal CommunalNamesDefinitionRecord(
            RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            var defs = new List<CommunalNameDefinition>();
            while (!reader.IsEOF)
            {
                var def = new CommunalNameDefinition();
                def.Name = reader.ReadPrefixedString();
                def.TypeIndex = reader.ReadIndex();
                def.DataType = reader.ReadByte();
                def.ElementCount = ReadEncodedInteger(reader);
                if (def.DataType == 0x61) // FAR data: count, elemsize
                    def.ElementSize = ReadEncodedInteger(reader);
                else
                    def.ElementSize = 1;
                defs.Add(def);
            }
            this.Definitions = defs.ToArray();

            context.Module.externalNames.AddRange(this.Definitions);
        }

        private static UInt32 ReadEncodedInteger(RecordReader reader)
        {
            byte b = reader.ReadByte();
            if (b == 0x81)
                return reader.ReadUInt16();
            else if (b == 0x84)
                return reader.ReadUInt24();
            else if (b == 0x88)
                return reader.ReadUInt32();
            else
                return b;
        }
    }

    /// <summary>
    /// Serves the same purpose as the EXTDEF record. The difference is that
    /// the symbol named is referred to through a Logical Name Index field,
    /// which is defined through an LNAMES or LLNAMES record.
    /// </summary>
    /// <remarks>
    /// This record is produced when a FIXUPP record refers to a COMDAT
    /// symbol.
    /// </remarks>
    public class COMDATExternalNamesDefinitionRecord : Record
    {
        public ExternalNameDefinition[] Symbols { get; private set; }

        internal COMDATExternalNamesDefinitionRecord(
            RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            List<ExternalNameDefinition> symbols = new List<ExternalNameDefinition>();
            while (!reader.IsEOF)
            {
                UInt16 nameIndex = reader.ReadIndex();
                if (nameIndex == 0 || nameIndex > context.Names.Count)
                    throw new InvalidDataException("LogicalNameIndex is out of range.");

                UInt16 typeIndex = reader.ReadIndex();
                ExternalNameDefinition symbol = new ExternalNameDefinition();
                symbol.Name = context.Names[nameIndex - 1];
                symbol.TypeIndex = typeIndex;
                symbols.Add(symbol);
            }
            this.Symbols = symbols.ToArray();

            //if (reader.RecordNumber == Omf.RecordNumber.LEXTDEF ||
            //    reader.RecordNumber == Omf.RecordNumber.LEXTDEF32)
            //{
            //    context.LocalExternalNames.AddRange(Symbols);
            //}
            //else
            //{
                context.Module.externalNames.AddRange(Symbols);
            //}
        }
    }

    #endregion

    /// <summary>
    /// Defines public symbols in this object module. The symbols are also
    /// available for export if so indicated in an EXPDEF comment record.
    /// </summary>
    /// <remarks>
    /// All defined functions and initialized global variables generate
    /// PUBDEF records in most compilers.
    /// </remarks>
    public class PublicNamesDefinitionRecord : Record
    {
        public LogicalSegment BaseSegment { get; private set; }
        public GroupDefinition BaseGroup { get; private set; }
        public UInt16 BaseFrame { get; private set; }
        public PublicNameDefinition[] Symbols { get; private set; }

        internal PublicNamesDefinitionRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            int baseGroupIndex = reader.ReadIndex();
            if (baseGroupIndex > context.Module.groups.Count)
                throw new InvalidDataException("Group index out of range.");
            if (baseGroupIndex > 0)
                this.BaseGroup = context.Module.groups[baseGroupIndex - 1];

            int baseSegmentIndex = reader.ReadIndex();
            if (baseSegmentIndex > context.Module.segments.Count)
                throw new InvalidDataException("Segment index out of range.");
            if (baseSegmentIndex == 0)
                this.BaseFrame = reader.ReadUInt16();
            else
                this.BaseSegment = context.Module.segments[baseSegmentIndex - 1];

            List<PublicNameDefinition> symbols = new List<PublicNameDefinition>();
            while (!reader.IsEOF)
            {
                PublicNameDefinition symbol = new PublicNameDefinition();
                symbol.Name = reader.ReadPrefixedString();
                symbol.Offset = reader.ReadUInt16Or32();
                symbol.TypeIndex = reader.ReadIndex();
                symbol.BaseSegment = BaseSegment;
                symbol.BaseGroup = BaseGroup;
                symbol.BaseFrame = BaseFrame;
                symbol.IsLocal =
                    reader.RecordNumber == Omf.RecordNumber.LPUBDEF ||
                    reader.RecordNumber == Omf.RecordNumber.LPUBDEF32;
                symbols.Add(symbol);
            }
            this.Symbols = symbols.ToArray();
            
            //if (reader.RecordNumber == Omf.RecordNumber.LPUBDEF ||
            //    reader.RecordNumber == Omf.RecordNumber.LPUBDEF32)
            //{
            //    context.LocalPublicNames.AddRange(Symbols);
            //}
            //else
            //{
                context.Module.publicNames.AddRange(Symbols);
            //}
        }
    }

    public class LocalPublicNameDefinitionRecord : PublicNamesDefinitionRecord
    {
        internal LocalPublicNameDefinitionRecord(
            RecordReader reader, RecordContext context)
            : base(reader, context)
        {
        }
    }

    /// <summary>
    /// Defines a list of names that can be referenced by subsequent records
    /// in the object module.
    /// </summary>
    public class ListOfNamesRecord : Record
    {
        public string[] Names { get; private set; }

        internal ListOfNamesRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            List<string> names = new List<string>();
            while (!reader.IsEOF)
            {
                names.Add(reader.ReadPrefixedString());
            }
            this.Names = names.ToArray();
            context.Names.AddRange(Names);
        }
    }

    public class SegmentDefinitionRecord : Record
    {
        public LogicalSegment Definition { get; private set; }

        internal SegmentDefinitionRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            LogicalSegment def = new LogicalSegment();

            byte acbp = reader.ReadByte();

            int alignment = acbp >> 5;
            switch (alignment)
            {
                case 0: def.Alignment = Alignment.None; break;
                case 1: def.Alignment = Alignment.Byte; break;
                case 2: def.Alignment = Alignment.Word; break;
                case 3: def.Alignment = Alignment.Paragraph; break;
                case 4: def.Alignment = Alignment.Page; break;
                case 5: def.Alignment = Alignment.DWord; break;
                case 6:
                case 7:
                    throw new InvalidDataException("Unsupported segment alignment: " + alignment);
            }

            int combination = (acbp >> 2) & 7;
            switch (combination)
            {
                case 0: def.Combination = SegmentCombination.Private; break;
                case 2:
                case 4:
                case 7: def.Combination = SegmentCombination.Public; break;
                case 5: def.Combination = SegmentCombination.Stack; break;
                case 6: def.Combination = SegmentCombination.Common; break;
                case 1:
                case 3:
                    throw new InvalidDataException("Unsupported segment combination: " + combination);
            }

            bool isBig = (acbp & 0x02) != 0;
            def.IsUse32 = (acbp & 0x01) != 0;

            if (def.Alignment == Alignment.None) // absolute segment
            {
                UInt16 frame = reader.ReadUInt16();
                reader.ReadByte(); // Offset is ignored by MS LINK
                def.StartAddress = new Pointer(frame, 0);
            }
            else
            {
                def.StartAddress = Pointer.Invalid;
            }

            long length = reader.ReadUInt16Or32();
            if (isBig)
            {
                if (reader.RecordNumber == Omf.RecordNumber.SEGDEF32)
                    length = 0x100000000L;
                else
                    length = 0x10000;
            }
            def.Length = length;
            def.Data = new byte[length];
            def.DataFixups = new ushort[length];

            int segmentNameIndex = reader.ReadIndex();
            if (segmentNameIndex > context.Names.Count)
                throw new InvalidDataException("SegmentNameIndex out of range.");
            if (segmentNameIndex > 0)
                def.Name = context.Names[segmentNameIndex - 1];

            int classNameIndex = reader.ReadIndex();
            if (classNameIndex > context.Names.Count)
                throw new InvalidDataException("ClassNameIndex out of range.");
            if (classNameIndex > 0)
                def.Class = context.Names[classNameIndex - 1];

            int overlayNameIndex = reader.ReadIndex();
            if (overlayNameIndex > context.Names.Count)
                throw new InvalidDataException("OverlayNameIndex is out of range.");
            if (overlayNameIndex > 0)
                def.OverlayName = context.Names[overlayNameIndex - 1];

            this.Definition = def;
            context.Module.segments.Add(def);
        }
    }

    public class GroupDefinitionRecord : Record
    {
        public GroupDefinition Definition { get; private set; }

        internal GroupDefinitionRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            GroupDefinition def = new GroupDefinition();

            UInt16 groupNameIndex = reader.ReadIndex();
            if (groupNameIndex == 0 || groupNameIndex > context.Names.Count)
            {
                throw new InvalidDataException("GroupNameIndex is out of range.");
            }
            def.Name = context.Names[groupNameIndex - 1];

            List<LogicalSegment> segments = new List<LogicalSegment>();
            while (!reader.IsEOF)
            {
                reader.ReadByte(); // 'type' ignored
                UInt16 segmentIndex = reader.ReadIndex();
                if (segmentIndex == 0 || segmentIndex > context.Module.segments.Count)
                {
                    throw new InvalidDataException("SegmentIndex is out of range.");
                }
                segments.Add(context.Module.segments[segmentIndex - 1]);
            }
            def.Segments = segments.ToArray();

            this.Definition = def;
            context.Module.groups.Add(def);
        }
    }

    /// <summary>
    /// Contains contiguous binary data to be copied into the program's
    /// executable binary image.
    /// </summary>
    public class LogicalEnumeratedDataRecord : Record
    {
        public LogicalSegment Segment { get; private set; }
        public UInt32 DataOffset { get; private set; }
        public byte[] Data { get; private set; }

        internal LogicalEnumeratedDataRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            UInt16 segmentIndex = reader.ReadIndex();
            if (segmentIndex == 0 || segmentIndex > context.Module.segments.Count)
            {
                throw new InvalidDataException("SegmentIndex is out of range.");
            }
            this.Segment = context.Module.segments[segmentIndex - 1];

            this.DataOffset = reader.ReadUInt16Or32();
            this.Data = reader.ReadToEnd(); // TBD: this can be optimized to
                                            // reduce extra data copy

            // Fill the segment's data.
            if (Data.Length + DataOffset > Segment.Length)
                throw new InvalidDataException("The LEDATA overflows the segment.");

            Array.Copy(Data, 0, Segment.Data, DataOffset, Data.Length);
        }
    }

    /// <summary>
    /// Contains contiguous binary data to be copied into the program's
    /// executable binary image. The data is stored as a repeating pattern.
    /// </summary>
    public class LogicalIteratedDataRecord : Record
    {
        public UInt16 SegmentIndex { get; private set; }
        public UInt32 DataOffset { get; private set; }
        public byte[] Data { get; private set; }

        internal LogicalIteratedDataRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            this.SegmentIndex = reader.ReadIndex();
            if (SegmentIndex == 0 || SegmentIndex > context.Module.segments.Count)
                throw new InvalidDataException("SegmentIndex is out of range.");

            this.DataOffset = reader.ReadUInt16Or32();
            this.Data = reader.ReadToEnd();

            // TODO: parse LIDATA (recursive; a bit messy)
        }
    }

    public class InitializedCommunalDataRecord : Record
    {
        internal InitializedCommunalDataRecord(
            RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            // TODO: parse contents.
        }
    }

    public class AliasDefinitionRecord : Record
    {
        public AliasDefinition[] Aliases { get; private set; }

        internal AliasDefinitionRecord(RecordReader reader, RecordContext context)
            : base(reader, context)
        {
            List<AliasDefinition> aliases = new List<AliasDefinition>();
            while (!reader.IsEOF)
            {
                AliasDefinition alias = new AliasDefinition();
                alias.AliasName = reader.ReadPrefixedString();
                alias.SubstituteName = reader.ReadPrefixedString();
            }
            this.Aliases = aliases.ToArray();
        }
    }

    public struct AliasDefinition
    {
        public string AliasName { get; internal set; }
        public string SubstituteName { get; internal set; }
    }
}
