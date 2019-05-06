// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetryProperties.cs
//
// Telemetry common types
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

namespace GameTelemetry
{
    // A single telemetry property 
    public class TelemetryProperty : System.Tuple<string, object>
    {
        public TelemetryProperty(string key, object value) : base (key, value) {}
    }

    // A collection of telemetry properties
    public class TelemetryProperties : System.Collections.Generic.Dictionary<string, object>
    {
        public TelemetryProperties() : base() { }

        public TelemetryProperties(TelemetryProperties properties) : base(properties) { }
    }
}