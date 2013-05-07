﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Disassembler
{
    /// <summary>
    /// Represents a pointer that contains a 32-bit flat address.
    /// </summary>
    public struct LinearPointer : IComparable<LinearPointer>
    {
        private int address;

        public LinearPointer(int address)
        {
            //if (address < 0)
            //    throw new ArgumentOutOfRangeException("address");
            this.address = address;
        }

        /// <summary>
        /// Gets the address this pointer points to. This may be negative.
        /// </summary>
        public int Address
        {
            get { return address; }
        }

        public Pointer ToFarPointer(UInt16 segmentAddress)
        {
            return new Pointer(segmentAddress, this);
        }

        public override string ToString()
        {
            return address.ToString("X5");
        }

        public static LinearPointer operator +(LinearPointer a, int offset)
        {
            return new LinearPointer(a.address + offset);
        }

        public static LinearPointer operator -(LinearPointer a, int offset)
        {
            return new LinearPointer(a.address - offset);
        }

        /// <summary>
        /// Increments the pointer by one.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static LinearPointer operator ++(LinearPointer a)
        {
            return new LinearPointer(a.address + 1);
        }

        public static bool operator <(LinearPointer a, LinearPointer b)
        {
            return a.address < b.address;
        }

        public static bool operator >(LinearPointer a, LinearPointer b)
        {
            return a.address > b.address;
        }

        public static bool operator <=(LinearPointer a, LinearPointer b)
        {
            return a.address <= b.address;
        }

        public static bool operator >=(LinearPointer a, LinearPointer b)
        {
            return a.address >= b.address;
        }

        public static int operator -(LinearPointer a, LinearPointer b)
        {
            return a.address - b.address;
        }

        public static bool operator ==(LinearPointer a, LinearPointer b)
        {
            return a.address == b.address;
        }

        public static bool operator !=(LinearPointer a, LinearPointer b)
        {
            return a.address != b.address;
        }

        public override bool Equals(object obj)
        {
            return (obj is LinearPointer) && ((LinearPointer)obj == this);
        }

        public override int GetHashCode()
        {
            return address.GetHashCode();
        }

        public int CompareTo(LinearPointer other)
        {
            return address.CompareTo(other.address);
        }
    }

    /// <summary>
    /// Represents a far pointer consisting of segment and offset components.
    /// For the moment, we only support 16-bit far pointers.
    /// </summary>
    public struct Pointer
    {
        private UInt16 segment;
        private UInt16 offset;

        /// <summary>
        /// Creates a far pointer with the given segment and offset values.
        /// </summary>
        /// <param name="segment">Segment address.</param>
        /// <param name="offset">Offset within segment.</param>
        public Pointer(UInt16 segment, UInt16 offset)
            : this()
        {
            this.segment = segment;
            this.offset = offset;
        }

        /// <summary>
        /// Creates a far pointer from a linear address and a segment address.
        /// </summary>
        /// <param name="segment">Segment address.</param>
        /// <param name="offset">Offset within segment.</param>
        public Pointer(UInt16 segment, LinearPointer address)
            : this()
        {
            int offset = address.Address - segment * 16;
            if (offset < 0 || offset > 0xFFFF)
                throw new ArgumentException("The supplied linear address is not contained in the given segment.");

            this.segment = segment;
            this.offset = (UInt16)offset;
        }

        /// <summary>
        /// Gets or sets the segment address.
        /// </summary>
        public UInt16 Segment
        {
            get { return segment; }
            set { segment = value; }
        }

        /// <summary>
        /// Gets or sets the offset within the segment.
        /// </summary>
        public UInt16 Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        /// <summary>
        /// Gets the linear (physical) address represented by this pointer.
        /// Note that this address may exceed 1MB, so care should be taken
        /// when using this address directly to access memory.
        /// </summary>
        public LinearPointer LinearAddress
        {
            get { return new LinearPointer(segment * 16 + offset); }
        }

        public override string ToString()
        {
            return string.Format("{0:X4}:{1:X4}", segment, offset);
        }

        public static Pointer Parse(string s)
        {
            Pointer ptr;
            if (!TryParse(s, out ptr))
                throw new ArgumentException("s");
            return ptr;
        }

        public static bool TryParse(string s, out Pointer pointer)
        {
            if (s == null)
                throw new ArgumentNullException("s");

            pointer = new Pointer();

            int k = s.IndexOf(':');
            if (k <= 0 || k >= s.Length - 1)
                return false;

            if (!UInt16.TryParse(
                    s.Substring(0, k),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out pointer.segment))
                return false;

            if (!UInt16.TryParse(
                    s.Substring(k + 1),
                    NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture,
                    out pointer.offset))
                return false;

            return true;
        }

        /// <summary>
        /// Increments the offset by the given amount, allowing it to wrap
        /// around 0xFFFF.
        /// </summary>
        /// <param name="increment">The amount to increment. A negative value
        /// specifies decrement.</param>
        /// <returns>The incremented pointer, possibly wrapped.</returns>
        public Pointer IncrementWithWrapping(int increment)
        {
            return new Pointer(segment, (ushort)(offset + increment));
        }

        /// <summary>
        /// Increments the offset by the given amount.
        /// </summary>
        /// <param name="increment">The amount to increment. A negative value
        /// specifies decrement.</param>
        /// <returns>The incremented pointer</returns>
        /// <exception cref="AddressWrappedException">If the offset would be
        /// wrapped around 0xFFFF.</exception>
        public Pointer Increment(int increment)
        {
            if ((increment > 0 && increment > 0xFFFF - offset) ||
                (increment < 0 && increment < -(int)offset))
            {
                throw new AddressWrappedException();
            }
            // TODO: check result.LinearAddress.
            return IncrementWithWrapping(increment);
        }

        /// <summary>
        /// Same as p.Increment(increment).
        /// </summary>
        /// <param name="p"></param>
        /// <param name="increment"></param>
        /// <returns></returns>
        public static Pointer operator +(Pointer p, int increment)
        {
            return p.Increment(increment);
        }

        /// <summary>
        /// Represents an invalid pointer value (FFFF:FFFF).
        /// </summary>
        public static readonly Pointer Invalid = new Pointer(0xFFFF, 0xFFFF);

        /// <summary>
        /// Returns true if two pointers have the same segment and offset
        /// values.
        /// </summary>
        /// <param name="a">First pointer.</param>
        /// <param name="b">Second pointer.</param>
        /// <returns></returns>
        public static bool operator ==(Pointer a, Pointer b)
        {
            return (a.segment == b.segment) && (a.offset == b.offset);
        }

        /// <summary>
        /// Returns true unless two pointers have the same segment and offset
        /// values.
        /// </summary>
        /// <param name="a">First pointer.</param>
        /// <param name="b">Second pointer.</param>
        /// <returns></returns>
        public static bool operator !=(Pointer a, Pointer b)
        {
            return (a.segment != b.segment) || (a.offset != b.offset);
        }

        /// <summary>
        /// Returns true if two pointers have the same segment and offset
        /// values.
        /// </summary>
        public override bool Equals(object obj)
        {
            return (obj is Pointer) && (this == (Pointer)obj);
        }

        public override int GetHashCode()
        {
            return this.LinearAddress.GetHashCode();
        }
    }

    public class AddressWrappedException : Exception
    {
    }
}
