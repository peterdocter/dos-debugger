﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using FileFormats.Omf;
using Util.Data;

namespace Disassembler
{
    /// <summary>
    /// Represents a logical segment in an object module.
    /// </summary>
    /// <remarks>
    /// A logical segment is defined by a SEGDEF record.
    /// 
    /// Multiple logical segments are often combined to form a
    /// CombinedSegment.
    /// </remarks>
    /// <example>
    /// Examples: fopen._TEXT, crt0._DATA, etc.
    /// </example>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class LogicalSegment : Segment // create new class LibrarySegment
                                          // to handle book-keeping of Segment
    {
        SegmentDefinition definition;

        private int id;
        readonly string fullName;
        readonly byte[] data;
        readonly FixupCollection fixups = new FixupCollection();

        internal LogicalSegment(
            int id,
            SegmentDefinition def,
            Dictionary<object, object> objectMap,
            ObjectModule module)
        {
            if (def.IsUse32)
                throw new NotSupportedException("Use32 is not supported.");
            if (def.Length > 0x10000)
                throw new NotSupportedException("Segments larger than 64KB are not supported.");

            this.definition = def;
            this.id = id;
            this.fullName = module.Name + "." + def.SegmentName;
            this.data = def.Data;
        }

        public void SetId(int id)
        {
            this.id = id;
        }

        public override string Name
        {
            get { return fullName; }
        }

        public override Range<int> OffsetBounds
        {
            get { return new Range<int>(0, this.Length); }
        }

        /// <summary>
        /// Gets the segment's name, such as "_TEXT". A segment's name
        /// together with its class name uniquely identifies the segment.
        /// </summary>
        //public new string Name
        //{
        //    get { return definition.SegmentName; }
        //}

        // TODO: make Segment an interface, and explicitly implement
        // its Name property.
        public string FullName
        {
            get { return fullName; }
        }

        /// <summary>
        /// Gets the segment's class, such as "CODE". A segment's name
        /// together with its class name uniquely identifies the segment.
        /// </summary>
        public string Class
        {
            get { return definition.ClassName; }
        }

        /// <summary>
        /// Gets the frame number of an absolute segment. This is only
        /// relevant if Alignment is Absolute.
        /// </summary>
        public UInt16 AbsoluteFrame
        {
            get { return definition.Frame; } // ignore Offset
        }

        /// <summary>
        /// Gets the length (in bytes) of the logical segment. This length
        /// does not include COMDAT records. If COMDAT records are present,
        /// their size should be added to this length.
        /// </summary>
        public int Length
        {
            get { return Data.Length; }
        }

        /// <summary>
        /// Gets the bytes in this logical segment.
        /// </summary>
        public byte[] Data
        {
            get { return data; }
        }

        public FixupCollection Fixups
        {
            get { return fixups; }
        }

        protected override string GetLabel()
        {
            return Name;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1}", Name, Class);
        }

        public override int Id
        {
            get { return this.id; }
        }
    }
}
