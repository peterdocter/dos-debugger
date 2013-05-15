﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Disassembler.Omf
{
    /// <summary>
    /// Contains information about a logical segment defined in an object
    /// module.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class LogicalSegment
    {
        /// <summary>
        /// Gets the segment's name, such as "_TEXT". A segment's name
        /// together with its class name uniquely identifies the segment.
        /// </summary>
        [Browsable(true)]
        public string SegmentName { get; internal set; }

        /// <summary>
        /// Gets the segment's class name, such as "CODE". A segment's name
        /// together with its class name uniquely identifies the segment.
        /// </summary>
        [Browsable(true)]
        public string ClassName { get; internal set; }

        /// <summary>
        /// Ignored by MS LINK.
        /// </summary>
        [Browsable(false)]
        public string OverlayName { get; internal set; }

        [Browsable(true)]
        public SegmentAlignment Alignment { get; internal set; }

        [Browsable(true)]
        public SegmentCombination Combination { get; internal set; }

        /// <summary>
        /// Gets the start address of an absolute segment. This value is only
        /// relevant if Alignment is Absolute.
        /// </summary>
        [Browsable(true)]
        public Pointer StartAddress { get; internal set; }

        /// <summary>
        /// Gets the length (in bytes) of the logical segment. This length
        /// does not include COMDAT records. If COMDAT records are present,
        /// their size should be added to this length.
        /// </summary>
        [Browsable(true)]
        public long Length { get; internal set; }

        [Browsable(false)]
        public bool IsUse32 { get; internal set; }

        public byte[] Data { get; internal set; }

        // datafixups[i] corresponds to data[i]
        // it is 1+index of the fix up that covers this byte. 0=none
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public UInt16[] DataFixups { get; internal set; }

        internal readonly List<FixupDefinition> fixups = new List<FixupDefinition>();

        public FixupDefinition[] Fixups
        {
            get { return fixups.ToArray(); }
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", SegmentName, ClassName);
        }
    }

#if false
    // todo: make a general enum for alignment, i.e.
    public enum Alignment
    {
        None = 0, // irrelevant or default
        Byte = 1,
        Word = 2,
        DWord = 4,
        QWord = 8,
        Paragraph = 16, // i.e. dqword
        //Page = 256,
    }
#endif

    /// <summary>
    /// Specifies the alignment requirement of a relocatable LSEG (logical
    /// segment).
    /// </summary>
    public enum SegmentAlignment : byte
    {
        /// <summary>
        /// Indicates a non-relocatable, absolute LSEG. This mode is not
        /// supported by MS LINK.
        /// </summary>
        Absolute = 0,

        /// <summary>
        /// Indicates a relocatable, byte aligned LSEG.
        /// </summary>
        ByteAligned = 1,

        /// <summary>
        /// Indicates a relocatable, word aligned LSEG.
        /// </summary>
        WordAligned = 2,

        /// <summary>
        /// Indicates a relocatable, paragraph (16 bytes) aligned LSEG.
        /// </summary>
        ParagraphAligned = 3,

        /// <summary>
        /// Indicates a relocatable, page (256 bytes) aligned LSEG.
        /// </summary>
        PageAligned = 4,

        /// <summary>
        /// Indicates a relocatable, dword aligned LSEG.
        /// </summary>
        DWordAligned = 5,
    }

    /// <summary>
    /// Specifies how the linker should combine two segments with the same
    /// name and class.
    /// </summary>
    public enum SegmentCombination : byte
    {
        /// <summary>
        /// Do not combine the segment with segments from other modules, even
        /// if they have the same segment name and class name.
        /// </summary>
        Private = 0,

        /// <summary>
        /// Concatenates all segments having the same name (and class name)
        /// to form a single, contiguous segment (subject to alignment
        /// requirements). This may leave a gap in between two segments.
        /// </summary>
        Public = 2,

        /// <summary>Same as Public.</summary>
        Public2 = 4,

        /// <summary>
        /// Concatenates all segments having the same name (and class name)
        /// and causes the operating system to set SS:00 to the bottom and
        /// SS:SP to the top of the resulting segment.
        /// </summary>
        Stack = 5,

        /// <summary>
        /// Combine by overlapping the two segments. They either have the same
        /// start address or the same end address. The combine segment size is
        /// the larger size of the two.
        /// </summary>
        Common = 6,

        /// <summary>Same as Public.</summary>
        Public3 = 7,
    }

    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class GroupDefinition
    {
        /// <summary>
        /// Gets the name of the group. Groups from different object modules
        /// are combined if their names are identical.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the logical segments contained in this group.
        /// </summary>
        public LogicalSegment[] Segments { get; internal set; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Defines a symbolic name.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class NameDefinition
    {
        public string Name { get; internal set; }
        public UInt16 TypeIndex { get; internal set; }

        public override string ToString()
        {
            if (TypeIndex == 0)
                return Name;
            else
                return string.Format("{0}:{1}", Name, TypeIndex);
        }
    }

    /// <summary>
    /// Contains information about an external symbolic name defined by one
    /// of the following records:
    /// EXTDEF  -- ExternalNamesDefinitionRecord
    /// LEXTDEF -- LocalExternalNamesDefinitionRecord
    /// CEXTDEF -- COMDATExternalNamesDefinitionRecord
    /// One of these records must be defined so that a FIXUPP record can
    /// refer to the symbol's address.
    /// </summary>
    public class ExternalNameDefinition : NameDefinition
    {
    }

    public class CommunalNameDefinition : ExternalNameDefinition
    {
        public byte DataType { get; internal set; }
        public UInt32 ElementCount { get; internal set; }
        public UInt32 ElementSize { get; internal set; }
    }

    /// <summary>
    /// Contains information about a public (exported) symbol.
    /// </summary>
    public class PublicNameDefinition : NameDefinition
    {
        /// <summary>
        /// Gets the LSEG (logical segment) in which this symbol is defined.
        /// If BaseSegment is not null, the Offset field is relative to the
        /// beginning of BaseSegment. If BaseSegment is null, the Offset field
        /// is relative to the physical frame indicated by FrameNumber.
        /// 
        /// Note: when BaseSegment is null, the public name is typically used
        /// to represent a constant.
        /// </summary>
        [Browsable(true)]
        public LogicalSegment BaseSegment { get; internal set; }

        /// <summary>
        /// Gets the group associated with this symbol. Such association is
        /// used to resolve the FRAME in a FIXUPP -- if BaseGroup is not null,
        /// then the group's frame is used as the FRAME of the fixup.
        /// </summary>
        [Browsable(true)]
        public GroupDefinition BaseGroup { get; internal set; }

        /// <summary>
        /// Gets the frame number of the address of this symbol. This is only
        /// relevant if BaseSegment is null, which indicates that the symbol
        /// refers to an absolute SEG:OFF address.
        /// </summary>
        [Browsable(true)]
        public UInt16 BaseFrame { get; internal set; }

        /// <summary>
        /// Gets the offset of the symbol relative to the start of the LSEG
        /// (logical segment) in which it is defined.
        /// </summary>
        [Browsable(true)]
        public UInt32 Offset { get; internal set; }

        /// <summary>
        /// Gets a flag indicating whether this symbol is local, i.e. is only
        /// visible within the object module where it is defined.
        /// </summary>
        [Browsable(true)]
        public bool IsLocal { get; internal set; }

        public override string ToString()
        {
            if (BaseSegment == null)
            {
                return string.Format("{0} @ {1:X4}:{2:X4}", Name, BaseFrame, Offset);
            }
            else
            {
                return string.Format("{0} @ {1}+{2:X}h", Name, BaseSegment.SegmentName, Offset);
            }
        }
    }

    public enum MemoryModel
    {
        Unknown = 0,
        Tiny,
        Small,
        Medium,
        Compact,
        Large,
        Huge
    }
}
