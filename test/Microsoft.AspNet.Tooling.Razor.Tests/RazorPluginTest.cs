﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;
using Microsoft.AspNet.Tooling.Razor.Models.OutgoingMessages;
using Microsoft.AspNet.Tooling.Razor.Tests;
using Microsoft.Framework.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNet.Tooling.Razor
{
    public class RazorPluginTest
    {
        private const string DefaultPrefix = "";

        private static readonly Type CustomTagHelperType = typeof(CustomTagHelper);
        private static readonly string CustomTagHelperAssembly = CustomTagHelperType.Assembly.GetName().Name;
        private static readonly TagHelperDescriptor CustomTagHelperDescriptor =
            new TagHelperDescriptor(
                DefaultPrefix,
                "custom",
                CustomTagHelperType.FullName,
                CustomTagHelperAssembly,
                attributes: new TagHelperAttributeDescriptor[0],
                requiredAttributes: new string[0]);

        [Fact]
        public void ProcessMessage_ThrowsWhenNoMessageType()
        {
            // Arrange
            var message = new JObject();
            var messageBroker = new TestPluginMessageBroker();
            var assemblyLoadContext = new TestAssemblyLoadContext();
            var plugin = new RazorPlugin(messageBroker);
            var expectedMessage = "'MessageType' must be provided for a 'RazorPluginRequestMessage' message.";

            // Act & Assert
            var error = Assert.Throws<InvalidOperationException>(
                () => plugin.ProcessMessage(message, assemblyLoadContext));
            Assert.Equal(expectedMessage, error.Message, StringComparer.Ordinal);
        }

        [Fact]
        public void ProcessMessage_ThrowsWhenNoData()
        {
            // Arrange
            var message = new JObject
            {
                { "MessageType", RazorPluginMessageTypes.ResolveTagHelperDescriptors }
            };
            var messageBroker = new TestPluginMessageBroker();
            var assemblyLoadContext = new TestAssemblyLoadContext();
            var plugin = new RazorPlugin(messageBroker);
            var expectedMessage = "'Data' must be provided for a '" +
                RazorPluginMessageTypes.ResolveTagHelperDescriptors + "' message.";

            // Act & Assert
            var error = Assert.Throws<InvalidOperationException>(
                () => plugin.ProcessMessage(message, assemblyLoadContext));
            Assert.Equal(expectedMessage, error.Message, StringComparer.Ordinal);
        }

        [Fact]
        public void ProcessMessage_NoOpsForUnknownMessageType()
        {
            var messageData = new JObject();
            var message = new JObject
            {
                { "MessageType", "SomethingUnknown" },
                { "Data", messageData },
            };
            var called = false;
            var messageBroker = new TestPluginMessageBroker((_) => called = true);
            var assemblyLoadContext = new TestAssemblyLoadContext();
            var plugin = new RazorPlugin(messageBroker);

            plugin.ProcessMessage(message, assemblyLoadContext);

            Assert.False(called);
        }

        [Fact]
        public void ProcessMessage_ResolveTagHelperDescriptors_ThrowsWhenNoAssemblyName()
        {
            // Arrange
            var messageData = new JObject();
            var message = new JObject
            {
                { "MessageType", RazorPluginMessageTypes.ResolveTagHelperDescriptors },
                { "Data", messageData },
            };
            var messageBroker = new TestPluginMessageBroker();
            var assemblyLoadContext = new TestAssemblyLoadContext();
            var plugin = new RazorPlugin(messageBroker);
            var expectedMessage = "'AssemblyName' must be provided for a 'ResolveTagHelperDescriptors' message.";

            // Act & Assert
            var error = Assert.Throws<InvalidOperationException>(
                () => plugin.ProcessMessage(message, assemblyLoadContext));
            Assert.Equal(expectedMessage, error.Message, StringComparer.Ordinal);
        }

        [Fact]
        public void ProcessMessage_ResolveTagHelperDescriptors_ThrowsWhenNoSourceLocation()
        {
            // Arrange
            var messageData = new JObject
            {
                { "AssemblyName", "SomeAssembly" }
            };
            var message = new JObject
            {
                { "MessageType", RazorPluginMessageTypes.ResolveTagHelperDescriptors },
                { "Data", messageData },
            };
            var messageBroker = new TestPluginMessageBroker();
            var assemblyLoadContext = new TestAssemblyLoadContext();
            var plugin = new RazorPlugin(messageBroker);
            var expectedMessage = "'SourceLocation' must be provided for a 'ResolveTagHelperDescriptors' message.";

            // Act & Assert
            var error = Assert.Throws<InvalidOperationException>(
                () => plugin.ProcessMessage(message, assemblyLoadContext));
            Assert.Equal(expectedMessage, error.Message, StringComparer.Ordinal);
        }

        [Fact]
        public void ProcessMessage_PluginProtocol_ResolvesCurrentProtocol()
        {
            // Arrange
            var message = new JObject
            {
                { "MessageType", RazorPluginMessageTypes.PluginProtocol },
            };
            PluginProtocolMessage responseMessage = null;
            var messageBroker = new TestPluginMessageBroker(data => responseMessage = (PluginProtocolMessage)data);
            var assemblyLoadContext = new TestAssemblyLoadContext();
            var plugin = new RazorPlugin(messageBroker);

            // Act
            plugin.ProcessMessage(message, assemblyLoadContext);

            // Assert
            Assert.NotNull(responseMessage);
            Assert.Equal(RazorPluginMessageTypes.PluginProtocol, responseMessage.MessageType, StringComparer.Ordinal);
            var responseData = responseMessage.Data;
            Assert.Equal("1.0.0", responseData, StringComparer.Ordinal);
        }

        [Fact]
        public void ProcessMessage_ResolveTagHelperDescriptors_ResolvesTagHelperDescriptors()
        {
            // Arrange
            var expectedSourceLocation = new SourceLocation(absoluteIndex: 1, lineIndex: 2, characterIndex: 3);
            var sourceLocationJson = JsonConvert.SerializeObject(expectedSourceLocation);
            var messageData = new JObject
            {
                { "AssemblyName", CustomTagHelperAssembly },
                { "SourceLocation", JObject.Parse(sourceLocationJson) }
            };
            var message = new JObject
            {
                { "MessageType", RazorPluginMessageTypes.ResolveTagHelperDescriptors },
                { "Data", messageData },
            };
            ResolveTagHelperDescriptorsMessage responseMessage = null;
            var messageBroker = new TestPluginMessageBroker(
                data => responseMessage = (ResolveTagHelperDescriptorsMessage)data);
            var assembly = new TestAssembly(typeof(CustomTagHelper));
            var assemblyNameLookups = new Dictionary<string, Assembly>
            {
                { CustomTagHelperAssembly, assembly }
            };
            var assemblyLoadContext = new TestAssemblyLoadContext(assemblyNameLookups);
            var plugin = new RazorPlugin(messageBroker);

            // Act
            plugin.ProcessMessage(message, assemblyLoadContext);

            // Assert
            Assert.NotNull(responseMessage);
            Assert.Equal(
                RazorPluginMessageTypes.ResolveTagHelperDescriptors,
                responseMessage.MessageType,
                StringComparer.Ordinal);
            var responseData = responseMessage.Data;
            Assert.Equal(CustomTagHelperAssembly, responseData.AssemblyName, StringComparer.Ordinal);
            var actualDescriptor = Assert.Single(responseData.Descriptors);
            Assert.Equal(CustomTagHelperDescriptor, actualDescriptor, TagHelperDescriptorComparer.Default);
            Assert.Empty(responseData.Errors);
        }

        [Fact]
        public void ProcessMessage_ResolveTagHelperDescriptors_ReturnsErrors()
        {
            // Arrange
            var expectedSourceLocation = new SourceLocation(absoluteIndex: 1, lineIndex: 2, characterIndex: 3);
            var sourceLocationJson = JsonConvert.SerializeObject(expectedSourceLocation);
            var messageData = new JObject
            {
                { "AssemblyName", "invalid" },
                { "SourceLocation", JObject.Parse(sourceLocationJson) }
            };
            var message = new JObject
            {
                { "MessageType", RazorPluginMessageTypes.ResolveTagHelperDescriptors },
                { "Data", messageData },
            };
            ResolveTagHelperDescriptorsMessage responseMessage = null;
            var messageBroker = new TestPluginMessageBroker(
                data => responseMessage = (ResolveTagHelperDescriptorsMessage)data);
            var assemblyLoadContext = new ThrowingAssemblyLoadContext("Invalid assembly");
            var plugin = new RazorPlugin(messageBroker);

            // Act
            plugin.ProcessMessage(message, assemblyLoadContext);

            // Assert
            Assert.NotNull(responseMessage);
            Assert.Equal(
                RazorPluginMessageTypes.ResolveTagHelperDescriptors,
                responseMessage.MessageType,
                StringComparer.Ordinal);
            var responseData = responseMessage.Data;
            Assert.Equal("invalid", responseData.AssemblyName, StringComparer.Ordinal);
            Assert.Empty(responseData.Descriptors);
            var error = Assert.Single(responseData.Errors);
            Assert.Equal(
                "Cannot resolve TagHelper containing assembly 'invalid'. Error: Invalid assembly: invalid",
                error.Message,
                StringComparer.Ordinal);
            Assert.Equal(expectedSourceLocation, error.Location);
            Assert.Equal(1, error.Length);
        }

        private class ThrowingAssemblyLoadContext : TestAssemblyLoadContext, IAssemblyLoadContext
        {
            private readonly string _errorMessage;

            public ThrowingAssemblyLoadContext(string errorMessage)
            {
                _errorMessage = errorMessage;
            }

            Assembly IAssemblyLoadContext.Load(AssemblyName assemblyName)
            {
                throw new Exception(_errorMessage + ": " + assemblyName.Name);
            }
        }
    }

    // Needs to be a public, non nested type to be a valid TagHelper
    public class CustomTagHelper : TagHelper
    {
    }
}