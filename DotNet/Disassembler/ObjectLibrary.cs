﻿using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Disassembler2
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ObjectLibrary : Assembly
    {
#if false
        public ObjectLibrary(IEnumerable<ObjectModule> modules)
        {
            if (modules == null)
                throw new ArgumentNullException("modules");

            foreach (ObjectModule module in modules)
                base.Modules.Add(module);
        }
#endif

#if false
        /// <summary>
        /// Gets a list of object modules in this library.
        /// </summary>
        //[TypeConverter(typeof(ExpandableObjectConverter))]
        //[TypeConverter(typeof(ArrayConverter))]
        //[TypeConverter(typeof(CollectionConverter))]
        //[TypeConverter(typeof(ExpandableCollectionConverter))]
        [Browsable(true)]
        public ObjectModule[] Modules { get; internal set; }
#endif

        public Dictionary<string, List<ObjectModule>> DuplicateSymbols
            = new Dictionary<string, List<ObjectModule>>();

        public Dictionary<string, List<ObjectModule>> UnresolvedSymbols
            = new Dictionary<string, List<ObjectModule>>();

        public void ResolveAllSymbols()
        {
            // First, we need to build a map of each public name.
            var nameDefs = new Dictionary<string, ObjectModule>();
            foreach (ObjectModule module in Modules)
            {
                foreach (var name in module.DefinedNames)
                {
                    if (nameDefs.ContainsKey(name.Name))
                    {
                        var prevDef = nameDefs[name.Name];
                    }
                    nameDefs[name.Name] = module;
                }
            }

            // Create a dummy node for "unresolved external symbols".
            // ...

            // Next, we create an edge for each external symbol reference.
            foreach (ObjectModule module in Modules)
            {
                foreach (var name in module.ExternalNames)
                {
                    ObjectModule defModule;
                    if (nameDefs.TryGetValue(name.Name, out defModule))
                    {
                        // ...
                    }
                    else // unresolved external symbol
                    {
                        // ...
                        List<ObjectModule> list;
                        if (!UnresolvedSymbols.TryGetValue(name.Name, out list))
                        {
                            list = new List<ObjectModule>();
                            UnresolvedSymbols.Add(name.Name, list);
                        }
                        list.Add(module);
                    }
                }
            }
        }
    }
}
