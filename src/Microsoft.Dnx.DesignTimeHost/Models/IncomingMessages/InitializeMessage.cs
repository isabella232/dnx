// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Dnx.DesignTimeHost.Models.IncomingMessages
{
    public class InitializeMessage
    {
        public int Version { get; set; }

        public string Configuration { get; set; }

        public string ProjectFolder { get; set; }
    }
}