﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Disassembler
{
    /// <summary>
    /// Represents an executable file or library file to be disassembled.
    /// </summary>
    /// <remarks>
    /// The object model looks like this:
    /// 
    /// Assembly                Executable              Library
    /// (physical)
    /// |-- Module1             LoadModule(1)           ObjectModule(*)
    /// |-- Module2
    ///     |-- Segment1        All segments share      Each segment has
    ///     |-- Segmeng2        the same image chunk    its own image chunk
    ///     |-- Segment3
    ///     |-- ...
    /// |-- Module3
    /// |-- ...
    /// * Note: An assembly actually bypasses the Module layer to manage
    ///         segments directly.
    /// (logical)
    /// |-- Procedures
    ///     |-- Procedure1      procedures should       procedures may
    ///     |-- Procedure2      not cross segments      cross segments
    ///     |-- ...
    /// |-- Symbols
    ///     |-- Symbol1         deduced from analysis   directly read
    ///     |-- Symbol2
    ///     |-- ...
    /// 
    /// </remarks>
    public class Assembly
    {
        private readonly XRefCollection crossReferences;
        private readonly BasicBlockCollection basicBlocks;
        private readonly ProcedureCollection procedures;
        private readonly ModuleCollection modules;
        private readonly ErrorCollection errors;
        private readonly Dictionary<int, ImageChunk> segments;

        public Assembly()
        {
            this.crossReferences = new XRefCollection();
            this.basicBlocks = new BasicBlockCollection();
            this.procedures = new ProcedureCollection();
            this.modules = new ModuleCollection();
            this.errors = new ErrorCollection();
            this.segments = new Dictionary<int, ImageChunk>();
        }

        public XRefCollection CrossReferences
        {
            get { return crossReferences; }
        }

        public BasicBlockCollection BasicBlocks
        {
            get { return basicBlocks; }
        }

        public ProcedureCollection Procedures
        {
            get { return procedures; }
        }

        public ModuleCollection Modules
        {
            get { return modules; }
        }

        public ErrorCollection Errors
        {
            get { return errors; }
        }

        /// <summary>
        /// Finds the segment with the given segment selector.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public virtual ImageChunk GetSegment(int segmentSelector)
        {
            return segments[segmentSelector];
        }

        /// <summary>
        /// Finds the segment with the given segment selector.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public void AddSegment(int segmentSelector, ImageChunk image)
        {
            segments.Add(segmentSelector, image);
        }
    }

    /// <summary>
    /// Represents a module in an assembly. For an executable, there is only
    /// one module which is the LoadModule, so this is not very interesting;
    /// for a library, it contains multiple ObjectModules.
    /// </summary>
    public abstract class Module
    {
    }

    public class ModuleCollection : List<Module>
    {
    }
}
