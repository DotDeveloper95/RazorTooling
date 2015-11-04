﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor;
using Microsoft.AspNet.Razor.Compilation.TagHelpers;
using Microsoft.AspNet.Tooling.Razor.Models.IncomingMessages;
using Microsoft.AspNet.Tooling.Razor.Models.OutgoingMessages;
using Microsoft.Dnx.DesignTimeHost;
using Microsoft.Dnx.Runtime.Common.CommandLine;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.Tooling.Razor
{
    internal static class ResolveTagHelpersCommand
    {
        public static void Register(CommandLineApplication app, IAssemblyLoadContext assemblyLoadContext)
        {
            app.Command("resolve-taghelpers", config =>
            {
                config.Description = "Resolves TagHelperDescriptors in the specified assembly(s).";
                config.HelpOption("-?|-h|--help");
                var protocolOption = config.Option(
                    "-p|--protocol",
                    "Protocol to resolve TagHelperDescriptors with.",
                    CommandOptionType.SingleValue);
                var assemblyNames = config.Argument(
                    "[name]",
                    "Assembly name to resolve TagHelperDescriptors in.",
                    multipleValues: true);

                config.OnExecute(() =>
                {
                    var messageBroker = new CommandMessageBroker();
                    var plugin = new RazorPlugin(messageBroker);
                    var protocol = int.Parse(protocolOption.Value());

                    plugin.Protocol = protocol;

                    var success = true;
                    foreach (var assemblyName in assemblyNames.Values)
                    {
                        var messageData = new ResolveTagHelperDescriptorsRequestData
                        {
                            AssemblyName = assemblyName,
                            SourceLocation = SourceLocation.Zero
                        };
                        var message = new RazorPluginRequestMessage(
                            RazorPluginMessageTypes.ResolveTagHelperDescriptors,
                            JObject.FromObject(messageData));

                        success &= plugin.ProcessMessage(JObject.FromObject(message), assemblyLoadContext);
                    }

                    var resolvedDescriptors = messageBroker.Results.SelectMany(result => result.Data.Descriptors);
                    var resolvedErrors = messageBroker.Results.SelectMany(result => result.Data.Errors);
                    var resolvedResult = new ResolvedTagHelperDescriptorsResult
                    {
                        Descriptors = resolvedDescriptors,
                        Errors = resolvedErrors
                    };
                    var serializedResult = JsonConvert.SerializeObject(resolvedResult, Formatting.Indented);

                    Console.WriteLine(serializedResult);

                    return success ? 0 : 1;
                });
            });
        }

        private class ResolvedTagHelperDescriptorsResult
        {
            public IEnumerable<TagHelperDescriptor> Descriptors { get; set; }

            public IEnumerable<RazorError> Errors { get; set; }
        }

        private class CommandMessageBroker : IPluginMessageBroker
        {
            public CommandMessageBroker()
            {
                Results = new List<ResolveTagHelperDescriptorsMessage>();
            }

            public List<ResolveTagHelperDescriptorsMessage> Results { get; }

            public void SendMessage(object data)
            {
                var responseMessage = data as ResolveTagHelperDescriptorsMessage;
                if (responseMessage != null)
                {
                    Results.Add(responseMessage);
                }
            }
        }
    }
}
