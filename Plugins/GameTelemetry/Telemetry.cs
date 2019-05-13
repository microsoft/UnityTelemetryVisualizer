// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// Telemetry.cs
//
// Telemetry definition common functions.
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using UnityEngine;

namespace GameTelemetry
{
    //Common usage library for telemetry
    public static class Telemetry
    {
        // Format a semantic version string given the provided values
        public static string FormatVersion(int major, int minor, int patch)
        {
            return string.Format("{0}.{1}.{2}", major, minor, patch);
        }

        // Creates an empty telemetry builder
        public static TelemetryBuilder Create() { return new TelemetryBuilder(); }

        // Creates a telemetry builder based on provided properties
        public static TelemetryBuilder Create(TelemetryProperties properties)
        {
            return new TelemetryBuilder(properties);
        }

        //Initializes the telemetry manager. Required before recording events
        public static void Initialize(GameObject gameObject)
        {
            TelemetryManager.Instance.Initialize(gameObject);
        }

        // Records an event and places it in the buffer to be sent
        public static void Record(string name, string category, string version, TelemetryBuilder properties)
        {
            TelemetryManager.Instance.Record(name, category, version, properties);
        }

        // Records an event and places it in the buffer to be sent
        public static void Record(string name, string category, string version, TelemetryProperties properties)
        { 
            TelemetryManager.Instance.Record(name, category, version, new TelemetryBuilder(properties));
        }

        // Helper functions for consistently formatting special properties
        // name of the event
        public static TelemetryProperty EventName(string name)
        {
            return new TelemetryProperty("name", name);
        }

        // Category for an event - Useful for grouping events of a similary type
        public static TelemetryProperty Category(string value)
        {
            return new TelemetryProperty("cat", value);
        }

        // Position vector for an entity
        public static TelemetryProperty Position(Vector3 value)
        {
            return new TelemetryProperty("pos", value);
        }

        // Orientation unit vector for an entity
        public static TelemetryProperty Orientation(Vector3 vec)
        {
            return new TelemetryProperty("dir", vec);
        }

        // Timestamp of the event using the client's clock by default
        public static TelemetryProperty ClientTimestamp() {
            return new TelemetryProperty("client_ts", System.DateTime.UtcNow);
        }

        public static TelemetryProperty ClientTimestamp(System.DateTime value)
        {
            return new TelemetryProperty("client_ts", value);
        }

        // Unique client id for the device playing the game
        // Typically set in the Common properties
        public static TelemetryProperty ClientId(string value)
        {
            return new TelemetryProperty("ClientId", value);
        }

        // Unique user id for the user playing the game
        // Typically set in the Common properties
        public static TelemetryProperty UserId(string value)
        {
            return new TelemetryProperty("UserId", value);
        }

        // Unique session id for the current play session
        public static TelemetryProperty SessionId(string value)
        {
            return new TelemetryProperty("SessionId", value);
        }

        // Semantic version of the telemetry event
        // Use this to help data pipelines understand how to process the event after ingestion
        public static TelemetryProperty Version(string value)
        {
            return new TelemetryProperty("tver", value);
        }

        // Semantic version of a component of the telemetry - Typically used by Telemetry Providers
        // Use this to help data pipelines understand how to process the event after ingestion
        public static TelemetryProperty Version(string subentity, string value)
        {
            return new TelemetryProperty("tver_" + subentity, value);
        }

        // A value that represents a percentage float between 0 and 100
        public static TelemetryProperty Percentage(string subentity, float value)
        {
            return new TelemetryProperty("pct_" + subentity, Mathf.Clamp(value, 0, 100));
        }

        // A float value
        public static TelemetryProperty Value(string subentity, float value)
        {
            return new TelemetryProperty("val_" + subentity, value);
        }

        // Generic telemetry property maker
        public static TelemetryProperty Prop<T>(string name, T value)
        {
            return new TelemetryProperty(name, value);
        }

        // Debug utility function for outputting telemetry to a string for printing
        public static string DumpJson(TelemetryProperties properties)
        {
            JSONObj jObject = new JSONObj(JSONObj.ObjectType.OBJECT);
            foreach(var Prop in properties)
            {
                jObject.AddField(Prop.Key, Prop.Value);
            }
            
            return jObject.Print();
        }
    }
}
