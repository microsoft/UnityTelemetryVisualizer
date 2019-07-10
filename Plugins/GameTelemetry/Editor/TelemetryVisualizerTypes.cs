// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetryVisualizerTypes.cs
//
// Provides base types for the visualizer
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using UnityEngine;

namespace GameTelemetry
{
    public static class Globals
    {
        //Setting whether to draw recieved events by default
        public const bool DefaultDrawSetting = true;

        //Set of default colors for drawing events
        public static Color[] DefaultColors =
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.grey,
            Color.magenta,
            Color.black,
            Color.white
        };
        

        //Types of heatmaps offered
        public enum HeatmapType
        {
            Population,
            Value,
            Population_Bar,
            Value_Bar
        };

        public static string[] HeatmapTypeString =
        {
            "Population",
            "Value",
            "Population - Bar",
            "Value - Bar"
        };
        
        public enum TelemetryAnimationState
        {
            Stopped,
            Playing,
            Paused,
        };
        
        //Strings associated with different viz settings for UI
        public static string[] ShapeStrings =
        {
            "Sphere",
            "Capsule",
            "Cylinder",
            "Cube",
            "Plane"
        };
        
        public static string[] AndOrStrings =
        {
            "Or",
            "And"
        };

        public enum QueryField
        {
            Category,
            BuildId,
            BuildType,
            ClientId,
            Platform,
            ProcessId,
            SessionId
        };

        public static string[] QueryFieldStrings =
        {
            "Category",
            "Build ID",
            "Build Type",
            "Client ID",
            "Platform",
            "Process ID",
            "Session ID"
        };

        public static string[] QueryExpectedStrings =
        {
            QueryIds.Category,
            QueryIds.BuildId,
            QueryIds.BuildType,
            QueryIds.ClientId,
            QueryIds.Platform,
            QueryIds.ProcessId,
            QueryIds.SessionId
        };

        public enum QueryOperator
        {
            Equal,
            Not_Equal,
            GreaterThan,
            LessThan,
            GreaterThanOrEqual,
            LessThanOrEqual
        };

        public static string[] QueryOperatorStrings =
        {
            "==",
            "<>",
            ">",
            "<",
            ">=",
            "<="
        };
    }

    //Wraps colors for heatmap settings
    public class HeatmapColors
    {
        private Color lowColor;
        public Color LowColor
        {
            get
            {
                return lowColor;
            }

            set
            {
                lowColor = value;
                UpdateRange();
            }
        }

        private Color highColor;
        public Color HighColor
        {
            get
            {
                return highColor;
            }

            set
            {
                highColor = value;
                UpdateRange();
            }
        }

        private Color range;

        private void UpdateRange()
        {
            range.a = highColor.a - lowColor.a;
            range.b = highColor.b - lowColor.b;
            range.g = highColor.g - lowColor.g;
            range.r = highColor.r - lowColor.r;
        }

        public HeatmapColors()
        {
            lowColor = Color.green;
            highColor = Color.red;

            lowColor.a = .7f;
            highColor.a = .7f;
            UpdateRange();
        }

        public Color GetColorFromRange(float location)
        {
            Color retColor = lowColor;

            retColor.a += range.a * location;
            retColor.r += range.r * location;
            retColor.g += range.g * location;
            retColor.b += range.b * location;

            return retColor;
        }
    };

    //Nodes used when preparing a heatmap
    public class HeatmapNode
    {
        public int numValues;
        public double values;
        public Vector3 orientation;

        public HeatmapNode()
        {
            numValues = 0;
            values = 0;
            orientation = Vector3.zero;
        }
        public HeatmapNode(int inNumVals, double inValues)
        {
            numValues = inNumVals;
            values = inValues;
            orientation = Vector3.zero;
        }

        public HeatmapNode(int inNumVals, double inValues, Vector3 inOrientation)
        {
            numValues = 0;
            values = 0;
            orientation = inOrientation;
        }
    };

    //Container for each UI line of the query
    public class QuerySetting
    {
        public bool isAnd;
        public Globals.QueryField Field;
        public Globals.QueryOperator Operator;
        public string Value;

        public QuerySetting()
        {
            isAnd = false;
            Field = 0;
            Operator = 0;
            Value = "";
        }
    };
}