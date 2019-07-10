// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// QueryEvent.cs
//
// Event structure returned from queries
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameTelemetry
{
    public static class QueryIds
    {
        public const string PlayerPos = "pos";
        public const string PlayerDir = "dir";
        public const string CamPos = "cam_pos";
        public const string CamDir = "cam_dir";
        public const string Id = "id";
        public const string ClientId = "client_id";
        public const string SessionId = "session_id";
        public const string Sequence = "seq";
        public const string EventName = "name";
        public const string EventAction = "action";
        public const string ClientTimestamp = "client_ts";
        public const string EventVersion = "e_ver";
        public const string BuildType = "build_type";
        public const string BuildId = "build_id";
        public const string ProcessId = "process_id";
        public const string Platform = "platform";
        public const string Category = "cat";
    }

    //Wrapper for each event
    public class QueryEvent
    {
        Dictionary<string, JSONObj> Attributes = new Dictionary<string, JSONObj>();

        public QueryEvent() { }

        public QueryEvent(JSONObj jObj)
        {
            Debug.Assert(jObj.IsObject);

            for (int i = 0; i < jObj.keys.Count; i++)
            {
                this.Attributes.Add(jObj.keys[i], jObj.list[i]);
            }
        }

        public string Id
        {
            get
            {
                return GetString(QueryIds.Id);
            }
        }

        public string ClientId
        {
            get
            {
                return GetString(QueryIds.ClientId);
            }
        }

        public string SessionId
        {
            get
            {
                return GetString(QueryIds.SessionId);
            }
        }

        public string Name
        {
            get
            {
                return GetString(QueryIds.EventName);
            }
        }

        public string BuildType
        {
            get
            {
                return GetString(QueryIds.BuildType);
            }
        }

        public string BuildId
        {
            get
            {
                return GetString(QueryIds.BuildId);
            }
        }

        public string Platform
        {
            get
            {
                return GetString(QueryIds.Platform);
            }
        }

        public string Category
        {
            get
            {
                return GetString(QueryIds.Category);
            }
        }

        UInt32 Sequence
        {
            get
            {
                return (UInt32)GetNumber(QueryIds.Sequence);
            }
        }
    
        public DateTime Time
        {
            get
            {
                return DateTime.Parse(GetString(QueryIds.ClientTimestamp));
            }
        }

        public Vector3 PlayerPosition
        {
            get
            {
                return GetVector(QueryIds.PlayerPos);
            }
        }

        public Vector3 PlayerDirection
        {
            get
            {
                return GetVector(QueryIds.PlayerDir);
            }
        }

        public Vector3 CameraPosition
        {
            get
            {
                return GetVector(QueryIds.CamPos);
            }
        }

        public Vector3 CameraDirection
        {
            get
            {
                return GetVector(QueryIds.CamDir);
            }
        }

        public QueryEvent Parse(JSONObj jObj)
        {
            Debug.Assert(jObj.IsObject);

            QueryEvent ev = new QueryEvent();
            for (int i = 0; i < jObj.keys.Count; i++)
            {
                ev.Attributes.Add(jObj.keys[i], jObj.list[i]);
            }

            return ev;
        }

        public void GetAttributes(ref Dictionary<string, JSONObj> inMap)
        {
            foreach (var attr in Attributes)
            {
                if (attr.Key.StartsWith("pct_") || attr.Key.StartsWith("val_"))
                {
                    inMap.Add(attr.Key, attr.Value);
                }
            }
        }

        public bool TryGetString(string name, out string outString)
        {
            if(Attributes.ContainsKey(name))
            {
                JSONObj Value = Attributes[name];
                if (Value != null && Value.IsString)
                {
                    string newString = Value.ToString();
                    outString = newString.Substring(1, newString.Length - 2);
                    return true;
                }
            }

            outString = "";
            return false;
        }

        public string GetString(string name)
        {
            string value;
            TryGetString(name, out value);
            return value;
        }

        public bool TryGetNumber(string name, out double number)
        {
            if (Attributes.ContainsKey(name))
            {
                JSONObj Value = Attributes[name];
                if (Value != null && Value.IsNumber)
                {
                    number = Value.n;
                    return true;
                }
            }

            number = 0;
            return false;
        }

        public double GetNumber(string name)
        {
            double Value;
            TryGetNumber(name, out Value);
            return Value;
        }

        public bool TryGetVector(string baseName, out Vector3 vector)
        {
            vector = new Vector3();

            if (Attributes.ContainsKey(baseName))
            {
                JSONObj Value = Attributes[baseName];

                if (Value != null && Value.IsObject)
                {
                    for(int i = 0; i < Value.list.Count; i++)
                    {
                        if(Value.list[i].IsNumber)
                        {
                            if (Value.keys[i] == "x")
                            {
                                vector.x = Value.list[i].f;
                            }
                            else if (Value.keys[i] == "y")
                            {
                                vector.y = Value.list[i].f;
                            }
                            else if (Value.keys[i] == "z")
                            {
                                vector.z = Value.list[i].f;
                            }
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public Vector3 GetVector(string baseName)
        {
            Vector3 vector;
            TryGetVector(baseName, out vector);
            return vector;
        }

        public bool TryGetBool(string Name, out bool outBool)
        {
            if(Attributes.ContainsKey(Name))
            {
                JSONObj Value = Attributes[Name];
                if (Value != null && Value.IsBool)
                {
                    outBool = Value.b;
                    return true;
                }
            }

            outBool = false;
            return false;
        }

        public bool GetBool(string Name)
        {
            bool value;
            TryGetBool(Name, out value);
            return value;
        }
    }
}
