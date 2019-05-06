// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetryBuilder.cs
//
// Useful wrapper for building telemetry events correctly
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

namespace GameTelemetry
{
    public class TelemetryBuilder : TelemetryProperties
    {
        public TelemetryBuilder() : base() { }

        public TelemetryBuilder(TelemetryProperties properties) : base(properties) { }

        public void SetProperty<T>(string name, T value)
        {
            if (this.ContainsKey(name))
            {
                this[name] = value;
            }
            else
            {
                Add(name, value);
            }
        }

        public void SetProperty(TelemetryProperty property)
        {
            if (this.ContainsKey(property.Item1))
            {
                this[property.Item1] = property.Item2;
            }
            else
            {
                Add(property.Item1, property.Item2);
            }
        }

        public void SetProperties(TelemetryProperties otherProperties)
        {
            foreach(var prop in otherProperties)
            {
                if (this.ContainsKey(prop.Key))
                {
                    this[prop.Key] = prop.Value;
                }
                else
                {
                    Add(prop.Key, prop.Value);
                }
            }
        }
    }
}