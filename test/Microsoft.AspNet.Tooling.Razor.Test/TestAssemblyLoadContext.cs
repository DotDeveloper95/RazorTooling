﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Dnx.Runtime;

namespace Microsoft.AspNet.Tooling.Razor.Tests
{
    public class TestAssemblyLoadContext : IAssemblyLoadContext
    {
        private readonly IReadOnlyDictionary<string, Assembly> _assemblyNameLookups;

        public TestAssemblyLoadContext()
            : this(new Dictionary<string, Assembly>(StringComparer.Ordinal))
        {
        }

        public TestAssemblyLoadContext(IReadOnlyDictionary<string, Assembly> assemblyNameLookups)
        {
            _assemblyNameLookups = assemblyNameLookups;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Assembly Load(AssemblyName assemblyName)
        {
            return _assemblyNameLookups[assemblyName.Name];
        }

        public Assembly LoadFile(string path)
        {
            throw new NotImplementedException();
        }

        public Assembly LoadStream(Stream assemblyStream, Stream assemblySymbols)
        {
            throw new NotImplementedException();
        }
    }
}