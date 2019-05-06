// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetryEventGameObject.cs
//
// Event structure used for rendering/managing each event in the game world
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using UnityEngine;
using System;

namespace GameTelemetry
{
    public class EventInfo : MonoBehaviour
    {
        //Client Time of the event
        public DateTime Time;

        //Value for an event
        public float Value;

        //Session of the event
        public string Session;

        //User ID of the event
        public string User;

        //Build string of the event
        public string Build;

        //Name of the event
        public string Name;

        //Category of the event
        public string Category;

        public void CopyFrom(EventInfo inEvent)
        {
            this.Time = inEvent.Time;
            this.Value = inEvent.Value;
            this.Session = inEvent.Session;
            this.User = inEvent.User;
            this.Build = inEvent.Build;
            this.Category = inEvent.Category;
            this.Name = inEvent.Name;
        }
    }

    //Type of mesh to draw for an event
    public class TelemetryEventGameObject
    {
        private Renderer eventRenderer;
        private MaterialPropertyBlock properties = new MaterialPropertyBlock();

        //Name and category of event
        private string eventName;

        //Location of an event
        private Vector3 location;

        //Orientation of an event
        private Vector3 orientation;

        //Client time of the event
        private EventInfo eventInfo;

        //Color of the event
        private Color color;
        public Color Color
        {
            get
            {
                return color;
            }

            set
            {
                if (color != value)
                {
                    color = value;
                    ApplyColor();
                }
            }
        }

        //Shape of the event
        private PrimitiveType type;
        public PrimitiveType Type
        {
            get
            {
                return type;
            }

            set
            {
                if (type != value)
                {
                    type = value;
                    ApplyShapeType();
                }
            }
        }

        //Host object
        private GameObject gameObject;
        public GameObject GameObject
        {
            get
            {
                return gameObject;
            }
        }

        private Vector3 scale;
        public float Scale
        {
            set
            {
                if (scale.x != value)
                {
                    scale = new Vector3(value, value, value);

                    if (gameObject != null)
                    {
                        gameObject.transform.localScale = scale;
                    }
                }

            }
        }

        private Material localMaterial = new Material(Shader.Find("GameTelemetry"));

        //Populates event values based on a telemetry event
        public void SetEvent(TelemetryEvent inEvent, Color inColor, PrimitiveType inType)
        {
            SetEvent(inEvent, inColor, inType, -1);
        }

        public void SetEvent(TelemetryEvent inEvent, Color inColor, PrimitiveType inType, int index)
        {
            eventInfo = new EventInfo();

            eventInfo.Time = inEvent.Time;
            eventInfo.Value = inEvent.Value;
            eventInfo.Session = inEvent.Session;
            eventInfo.User = inEvent.User;
            eventInfo.Build = inEvent.Build;
            eventInfo.Name = inEvent.Name;
            eventInfo.Category = inEvent.Category;
            location = inEvent.Point;
            orientation = inEvent.Orientation;
            color = inColor;
            type = inType;

            scale = new Vector3(0.5f, 0.5f, 0.5f);

            eventName = $"{eventInfo.Category} {eventInfo.Name}";

            if (index >= 0)
            {
                eventName += index;
            }

            ApplyShapeType();
        }

        //Populates event values for heatmaps
        public void SetHeatmapEvent(int index, Vector3 inPoint, Vector3 inOrientation, Color inColor, PrimitiveType inType, float inScale, float inValue)
        {
            SetHeatmapEvent(index, inPoint, inOrientation, inColor, inType, new Vector3(inScale, inScale, inScale), inValue);
        }

        public void SetHeatmapEvent(int index, Vector3 inPoint, Vector3 inOrientation, Color inColor, PrimitiveType inType, Vector3 inScale, float inValue)
        {
            eventInfo = new EventInfo();
            eventInfo.Value = inValue;
            location = inPoint;
            orientation = inOrientation;
            color = inColor;
            type = inType;
            scale = inScale;

            eventName = $"Heatmap {index}";

            ApplyShapeType();
        }

        private void ApplyColor()
        {
            if (gameObject != null)
            {
                eventRenderer.GetPropertyBlock(properties);
                properties.SetColor("_Color", color);
                eventRenderer.SetPropertyBlock(properties);
            }
        }

        private void ApplyShapeType()
        {
            gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = eventName;
            gameObject.transform.position = location;
            gameObject.transform.localScale = scale;

            if (orientation != Vector3.zero)
            {
                gameObject.transform.rotation = Quaternion.LookRotation(orientation);
            }

            eventRenderer = gameObject.GetComponent<Renderer>();
            eventRenderer.material = localMaterial;

            EventInfo newInfo = gameObject.AddComponent<EventInfo>();
            newInfo.CopyFrom(eventInfo);

            ApplyColor();
        }
    };
}