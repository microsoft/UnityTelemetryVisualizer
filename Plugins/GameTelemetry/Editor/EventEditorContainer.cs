// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// EventEditorContainer.cs
//
// Events and Containers for managing telemetry events within the UI
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GameTelemetry
{
    //Primary container for events managed by the UI
    public class TelemetryEvent
    {
        public string User;
        public string Build;
        public string Name;
        public string Category;
        public string Session;
        public Vector3 Point;
        public Vector3 Orientation;

        public DateTime Time;
        public float Value;
        private bool isPct;
        public bool IsPercentage
        {
            get
            {
                return isPct;
            }
        }


        public TelemetryEvent()
        {
            Name = "";
            Category = "";
            Session = "";
            User = "";
            Build = "";
            Point = Vector3.zero;
            Orientation = Vector3.zero;
            Time = DateTime.Now;
            Value = 0;
            isPct = false;
        }

        public TelemetryEvent(string inName, string inCategory, string inSession, string inBuild, string inUser, Vector3 point, DateTime time, float value, bool isPct)
        {
            this.Point = point;
            this.Time = time;
            this.Value = value;
            this.isPct = isPct;
            Orientation = Vector3.zero;
            Name = inName;
            Category = inCategory;
            Session = inSession;
            User = inUser;
            Build = inBuild;
        }

        public TelemetryEvent(string inName, string inCategory, string inSession, string inBuild, string inUser, Vector3 point, Vector3 orientation, DateTime time, float value, bool isPct)
        {
            this.Point = point;
            this.Orientation = orientation;
            this.Time = time;
            this.Value = value;
            this.isPct = isPct;
            Name = inName;
            Category = inCategory;
            Session = inSession;
            User = inUser;
            Build = inBuild;
        }
    };

    //Wrapper for the event container with draw information
    public class EventEditorContainer
    {
        private Color color;
        public Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
            }
        }
   
        private bool shouldDraw;
        public bool ShouldDraw
        {
            get
            {
                return shouldDraw;
            }
            set
            {
                shouldDraw = value;
            }
        }

        private bool shouldAnimate;
        public bool ShouldAnimate
        {
            get
            {
                return shouldAnimate;
            }
            set
            {
                shouldAnimate = value;
            }
        }

        private PrimitiveType type;
        public PrimitiveType Type
        {
            get
            {
                return (PrimitiveType)type;
            }
            set
            {
                type = (PrimitiveType)value;
            }
        }

        private bool isPct;
        public bool IsPercentage
        {
            get
            {
                return isPct;
            }
        }

        public DateTime TimeStart
        {
            get
            {
                return events[events.Count - 1].Time;
            }
        }

        public DateTime TimeEnd
        {
            get
            {
                return events[0].Time;
            }
        }

        public string Name;
        public string Session;
        public List<TelemetryEvent> events;

        public EventEditorContainer()
        {
            shouldDraw = Globals.DefaultDrawSetting;
            shouldAnimate = false;
            isPct = false;
            color = Color.red;
            type = (int)PrimitiveType.Sphere;
            events = new List<TelemetryEvent>();
        }

        public EventEditorContainer(string Name, int index) : this()
        {
            this.Name = Name;
            color = Globals.DefaultColors[index % Globals.DefaultColors.Length];
        }

        public EventEditorContainer(List<QueryEvent> inEvents)
        {
            shouldDraw = Globals.DefaultDrawSetting;
            shouldAnimate = false;
            color = Color.red;
            type = (int)PrimitiveType.Sphere;

            if (inEvents.Count > 0)
            {
                Fill(inEvents);
                Name = inEvents[0].Name;
                isPct = events[0].IsPercentage;
            }
        }

        public void AddEvent(QueryEvent newEvent)
        {
            string valueName = "";
            double value = 0;
            bool isPct = false;

            //Check for a specified value the event wants to draw
            if (newEvent.TryGetString("disp_val", out valueName))
            {
                value = newEvent.GetNumber(valueName);
                isPct = valueName.StartsWith("pct_");
            }

            events.Add(new TelemetryEvent(
                newEvent.Name,
                newEvent.Category,
                newEvent.SessionId,
                $"{newEvent.BuildType} {newEvent.BuildId} {newEvent.Platform}",
                newEvent.UserId,
                newEvent.PlayerPosition,
                newEvent.PlayerDirection,
                newEvent.Time,
                (float)value,
                isPct));
        }

        //Add an array of query results
        public void Fill(List<QueryEvent> inEvents)
        {
            foreach (var newEvent in inEvents)
            {
                AddEvent(newEvent);
            }
        }

        //Provides the total timespan for all events
        public TimeSpan GetTimespan()
        {
            return TimeEnd - TimeStart;
        }

        //Provides a percent location based on tick time
        public float GetTimeScaleFromTime(TimeSpan inTime)
        {
            return (float)(inTime.TotalMilliseconds/(TimeEnd - TimeStart).TotalMilliseconds);
        }

        //Provides a percent location based on the element
        public float GetEventTimeScale(int index)
        {
            return (float)((events[events.Count - 1 - index].Time - TimeStart).TotalMilliseconds / (TimeEnd - TimeStart).TotalMilliseconds);
        }

        //Provides an element based on the percent location
        public int GetEventIndexForTimeScale(float scale)
        {
            int i;
            DateTime targetValue = TimeStart.AddTicks((long)((TimeEnd - TimeStart).Ticks * scale));

            for (i = events.Count - 1; i > 0; i--)
            {
                if (events[i].Time > targetValue) break;
            }

            return i;
        }


        //Gets the box for where events happen (for heatmap)
        public Bounds GetPointRange()
        {
            return GetPointRange(0, events.Count - 1);
        }

        //Gets the box for where specified events happen (for heatmap animations)
        public Bounds GetPointRange(int start, int end)
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int i = start; i <= end; i++)
            {
                min.x = Math.Min(min.x, events[i].Point.x);
                min.y = Math.Min(min.y, events[i].Point.y);
                min.z = Math.Min(min.z, events[i].Point.z);
                max.x = Math.Max(max.x, events[i].Point.x);
                max.y = Math.Max(max.y, events[i].Point.y);
                max.z = Math.Max(max.z, events[i].Point.z);
            }

            Bounds range = new Bounds();
            range.SetMinMax(min, max);
            return range;
        }
    };
}