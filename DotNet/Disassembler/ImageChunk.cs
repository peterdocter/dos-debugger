﻿using System;
using System.Collections.Generic;
using System.Text;
using Disassembler.Omf;
using X86Codec;
using System.Collections.ObjectModel;

namespace Disassembler
{
    /// <summary>
    /// Contains information about a contiguous chunk of bytes in a binary
    /// image. The bytes may contain code, data, and unknown bytes. In
    /// particular, any fix-up information is associated.
    /// </summary>
    public class ImageChunk
    {
        byte[] image;
        ByteAttribute[] attrs;
        FixupCollection fixups;

        public ImageChunk(int length)
            : this(new byte[length])
        {
        }

        /// <summary>
        /// Creates an image chunk with the supplied binary data.
        /// </summary>
        /// <param name="image"></param>
        public ImageChunk(byte[] image)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            this.image = image;
            this.attrs = new ByteAttribute[image.Length];
            this.fixups = new FixupCollection();
        }

        /// <summary>
        /// Gets the binary image data.
        /// </summary>
        public byte[] Data
        {
            get { return image; }
        }

        public int Length
        {
            get { return image.Length; }
        }

        /// <summary>
        /// Gets the fixups defined on this chunk.
        /// </summary>
        public FixupCollection Fixups
        {
            get { return fixups; }
        }

        /// <summary>
        /// Decodes an instruction at the given offset, applying associated
        /// fix-up information if present.
        /// </summary>
        /// <returns>The decoded instruction.</returns>
        /// <exception cref="ArgumentOutOfRangeException">If offset refers to
        /// a location outside of the image.</exception>
        public Instruction DecodeInstruction(int offset)
        {
            if (offset < 0 || offset >= image.Length)
                throw new ArgumentOutOfRangeException("offset");

            Instruction instruction = X86Codec.Decoder.Decode(
                image, offset, CpuMode.RealAddressMode);

            // Find the first fixup that covers the instruction.
            int fixupIndex = fixups.BinaryLocate(offset);
            if (fixupIndex < 0)
                fixupIndex = ~fixupIndex;

            for (int i = 0; i < instruction.Operands.Length; i++)
            {
                if (fixupIndex >= fixups.Count) // no more fixups
                    break;

                Fixup fixup = fixups[fixupIndex];
                if (fixup.StartIndex >= offset + instruction.EncodedLength) // past end
                    break;

                Operand operand = instruction.Operands[i];
                if (operand is RelativeOperand)
                {
                    RelativeOperand opr = (RelativeOperand)operand;
                    int pos = offset + opr.Offset.Location.StartOffset;
                    int pos2 = pos + opr.Offset.Location.Length;

                    if (fixup.StartIndex >= pos2)
                        continue;

                    if (fixup.StartIndex != pos || fixup.EndIndex != pos2)
                        throw new BrokenFixupException(fixup);

                    //var target = new SymbolicTarget(fixup, module);
                    //instruction.Operands[i] = new SymbolicRelativeOperand(target);
                    ++fixupIndex;

                    //instruction.Operands[i] = new SourceAwareRelativeOperand(
                    //    (RelativeOperand)instruction.Operands[i],
                    //    address + instruction.EncodedLength);
                }
            }

            if (fixupIndex < fixups.Count &&
                fixups[fixupIndex].StartIndex < offset + instruction.EncodedLength)
            {
                throw new BrokenFixupException(fixups[fixupIndex]);
            }
            return instruction;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// This is a bit-field, described as below:
        /// 
        ///   7   6   5   4   3   2   1   0
        /// +---+---+---+---+---+---+---+---+
        /// | - | - | FL| F | L | - |  TYPE |
        /// +---+---+---+---+---+---+---+---+
        /// 
        /// -   : reserved
        /// TYPE: 00 = unknown
        ///       01 = padding
        ///       10 = code
        ///       11 = data
        /// L (LeadByte): 0 = not a lead byte
        ///               1 = is lead byte of code or data
        /// F (Fix-up):   0 = no fix-up info
        ///               1 = has fix-up info
        /// </remarks>
        struct ByteAttribute
        {
            byte attr;

            public ByteType Type
            {
                get { return (ByteType)(attr & 0x3); }
                set { attr = (byte)((attr & ~3) | ((int)value & 3)); }
            }

            public bool IsLeadByte
            {
                get { return (attr & 0x08) != 0; }
                set
                {
                    if (value)
                        attr |= 0x08;
                    else
                        attr &= 0xF7;
                }
            }

            public bool HasFixup
            {
                get { return (attr & 0x10) != 0; }
                set
                {
                    if (value)
                        attr |= 0x10;
                    else
                        attr &= 0xEF;
                }
            }
        }

        /// <summary>
        /// Provides methods to access the fixups defined on this image chunk.
        /// </summary>
        public class FixupCollection : IList<Fixup>
        {
            List<Fixup> fixups;
            bool done;

            public FixupCollection()
            {
                this.fixups = new List<Fixup>();
                this.done = false;
            }

            public void Seal()
            {
                if (!done)
                {
                    fixups.Sort((x, y) => x.StartIndex.CompareTo(y.StartIndex));
                    for (int i = 1; i < fixups.Count; i++)
                    {
                        if (fixups[i - 1].EndIndex > fixups[i].StartIndex)
                            throw new InvalidOperationException("Some fixups overlap.");
                    }
                    done = true;
                }
            }

            /// <summary>
            /// Finds the fixup associated with the given position. If no
            /// fixup is found, find the first one that comes after that
            /// position.
            /// </summary>
            /// <param name="offset"></param>
            /// <returns></returns>
            public int BinaryLocate(int offset)
            {
                //fixups.BinarySearch(
                //return fixups[offset];
                throw new NotImplementedException();
            }

            public int IndexOf(Fixup item)
            {
                throw new NotSupportedException();
            }

            public void Insert(int index, Fixup item)
            {
                throw new NotSupportedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotSupportedException();
            }

            public Fixup this[int index]
            {
                get
                {
                    if (!done)
                        throw new InvalidOperationException("Cannot access the collection until Seal() is called.");
                    return fixups[index];
                }
                set { throw new NotSupportedException(); }
            }

            public void Add(Fixup item)
            {
                if (item == null)
                    throw new ArgumentNullException("item");
                fixups.Add(item);
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(Fixup item)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(Fixup[] array, int arrayIndex)
            {
                if (!done)
                {
                    throw new InvalidOperationException("Cannot access the collection until Seal() is called.");
                }
                fixups.CopyTo(array, arrayIndex);
            }

            public int Count
            {
                get { return fixups.Count; }
            }

            public bool IsReadOnly
            {
                get
                {
                    if (done)
                        return true;
                    else
                        return false;
                }
            }

            public bool Remove(Fixup item)
            {
                throw new NotSupportedException();
            }

            public IEnumerator<Fixup> GetEnumerator()
            {
                if (!done)
                {
                    throw new InvalidOperationException("Cannot access the collection until Seal() is called.");
                }   
                return fixups.GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }

#if false
    
            // Create a BinaryImage with the code.
            BinaryImage image = new BinaryImage(codeSegment.Data, new Pointer(0, 0));

            // Disassemble the instructions literally. Note that this should
            // be improved, but we don't do that yet.
            var addr = image.BaseAddress;
            for (var i = image.StartAddress; i < image.EndAddress; )
            {
                var instruction = image.DecodeInstruction(addr);

                // An operand may have zero or one component that may be
                // fixed up. Check this.
                for (int k = 0; k < instruction.Operands.Length; k++)
                {
                    var operand = instruction.Operands[k];
                    if (operand is RelativeOperand)
                    {
                        var opr = (RelativeOperand)operand;
                        var loc = opr.Offset.Location;
                        int j = i - image.StartAddress + loc.StartOffset;
                        int fixupIndex = codeSegment.DataFixups[j];
                        if (fixupIndex != 0)
                        {
                            FixupDefinition fixup = codeSegment.Fixups[fixupIndex - 1];
                            if (fixup.DataOffset != j)
                                continue;

                            var target = new SymbolicTarget(fixup, module);
                            instruction.Operands[k] = new SymbolicRelativeOperand(target);
                            System.Diagnostics.Debug.WriteLine(instruction.ToString());
                        }
                    }
                }

                image.CreatePiece(addr, addr + instruction.EncodedLength, ByteType.Code);
                image[addr].Instruction = instruction;
                addr = addr.Increment(instruction.EncodedLength);

                // TODO: we need to check more accurately.

#if false
                // Check if any bytes covered by this instruction has a fixup
                // record associated with it. Note that an instruction might
                // have multiple fixup records associated with it, such as 
                // in a far call.
                for (int j = 0; j < instruction.EncodedLength; j++)
                {
                    int fixupIndex = codeSegment.DataFixups[i - image.StartAddress + j];
                    if (fixupIndex != 0)
                    {
                        FixupDefinition fixup = codeSegment.Fixups[fixupIndex - 1];
                        if (fixup.DataOffset != i - image.StartAddress + j)
                            continue;

                        if (fixup.Target.Method == FixupTargetSpecFormat.ExternalPlusDisplacement ||
                            fixup.Target.Method == FixupTargetSpecFormat.ExternalWithoutDisplacement)
                        {
                            var extIndex = fixup.Target.IndexOrFrame;
                            var extName = module.ExternalNames[extIndex - 1];
                            var disp = fixup.Target.Displacement;

                            System.Diagnostics.Debug.WriteLine(string.Format(
                                "{0} refers to {1}+{2} : {3}",
                                instruction, extName, disp, fixup.Location));
                        }
                    }
                }
#endif
#endif
}
