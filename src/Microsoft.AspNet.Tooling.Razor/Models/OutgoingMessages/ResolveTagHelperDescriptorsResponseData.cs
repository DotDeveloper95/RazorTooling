﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Tooling.Razor.Models.OutgoingMessages
{
    public class ResolveTagHelperDescriptorsResponseData
    {
        public string AssemblyName { get; set; }
        public IEnumerable<TagHelperDescriptor> Descriptors { get; set; }
        public IEnumerable<RazorError> Errors { get; set; }
    }
}