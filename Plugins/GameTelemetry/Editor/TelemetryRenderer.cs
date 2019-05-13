// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetryRenderer.cs
//
// Provides building for Telemetry events
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GameTelemetry
{
    public class GameTelemetryRenderer
    {
        private GameObject host;
        public GameObject Host
        {
            get
            {
                if(host == null)
                {
                    host = new GameObject("GameTelemetry");
                }

                return host;
            }
        }

        public double HeatmapValueMin = -1;
        public double HeatmapValueMax = -1;

        private List<TelemetryEventGameObject> gameObjectCollection = new List<TelemetryEventGameObject>();
        private bool needsTelemetryObjectUpdate = false;

        public void TriggerTelemetryUpdate()
        {
            needsTelemetryObjectUpdate = true;
        } 

        // Master draw call.  Updates animation if running, otherwise checks if objects need to be updated
        public void Tick(List<EventEditorContainer> filterCollection, float heatmapSize, HeatmapColors heatmapColor, int heatmapType, int heatmapShape, bool useOrientation, string subEvent, ref AnimationController animController)
        {
            if (!animController.IsStopped())
            {
                if (animController.ShouldAnimate)
                {
                    if (animController.NeedRefresh())
                    {
                        DestroyTelemetryObjects();

                        //If we are playing in reverse, we need to add the remaining points to be removed later
                        if (animController.PlaySpeed < 0)
                        {
                            EventEditorContainer eventContainer = animController.EventContainer;
                            int j = 0;
                            if (eventContainer != null)
                            {
                                foreach (var currEvent in eventContainer.events)
                                {
                                    CreateTelemetryObject(j, currEvent, animController.Color, animController.Type);
                                    j++;
                                }
                            }
                        }
                    }

                    List<TelemetryEvent> tempArray;

                    if (animController.PlaySpeed >= 0)
                    {
                        if (animController.IsHeatmap)
                        {
                            //Regenerate the heatmap only using a subset of the events
                            int start = animController.GetNextIndex();
                            int next = animController.GetNextEventCount();
                            EventEditorContainer tempContainer = animController.EventContainer;
                            animController.AnimSlider = animController.GetTimeScaleFromTime() * animController.AnimSliderMax;
                            if (start != next)
                            {
                                GenerateHeatmap(tempContainer, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, next, tempContainer.events.Count - 1, subEvent);
                            }
                        }
                        else
                        {
                            //Update forward point animation by getting events that have occured over the last timespan and creating them
                            tempArray = animController.GetNextEvents();
                            int i = animController.GetNextIndex();
                            animController.AnimSlider = animController.GetTimeScaleFromTime() * animController.AnimSliderMax;
                            foreach (var currEvent in tempArray)
                            {
                                CreateTelemetryObject(i, currEvent, animController.Color, animController.Type);
                                i++;
                            }
                        }
                    }
                    else
                    {
                        if (animController.IsHeatmap)
                        {
                            //Regenerate the heatmap only using a subset of the events
                            int start = animController.GetPrevIndex();
                            int next = animController.GetPrevEventCount();
                            EventEditorContainer tempContainer = animController.EventContainer;
                            animController.AnimSlider = (1 - animController.GetTimeScaleFromTime()) * animController.AnimSliderMax;
                            if (start != next)
                            {
                                GenerateHeatmap(tempContainer, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, next, tempContainer.events.Count - 1, subEvent);
                            }
                        }
                        else
                        {
                            //Update reverse point animation by removing the last TelemetryObject for as many events occured over the last timespan
                            tempArray = animController.GetPrevEvents();
                            animController.AnimSlider = (1 - animController.GetTimeScaleFromTime()) * animController.AnimSliderMax;
                            foreach (var currEvent in tempArray)
                            {
                                DestroyLastTelemetryObject();
                            }
                        }
                    }

                    //Set the location of the slider
                    if (!animController.ShouldAnimate && animController.PlaySpeed >= 0)
                    {
                        animController.AnimSlider = animController.AnimSliderMax;
                    }
                    else if (!animController.ShouldAnimate && animController.PlaySpeed < 0)
                    {
                        animController.AnimSlider = 0;
                    }
                }
            }
            else
            {
                if (needsTelemetryObjectUpdate)
                {
                    //Draw data points
                    needsTelemetryObjectUpdate = false;
                    DestroyTelemetryObjects();

                    foreach (var events in filterCollection)
                    {
                        if (events.ShouldDraw)
                        {
                            int startIndex = 0;
                            int endIndex = events.events.Count;

                            CreateTelemetryObjects(events.events, startIndex, endIndex, events.Color, events.Type);
                        }
                    }
                }
            }
        }

        //Generate a heatmap for the range of events
        public void GenerateHeatmap(EventEditorContainer collection, float heatmapSize, HeatmapColors heatmapColor, int heatmapType, int heatmapShape, bool useOrientation, string subEvent)
        {
            GenerateHeatmap(collection, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, 0, collection.events.Count - 1, subEvent);
        }

        public void GenerateHeatmap(EventEditorContainer collection, float heatmapSize, HeatmapColors heatmapColor, int heatmapType, int heatmapShape, bool useOrientation, int first, int last, string subEvent)
        {
            DestroyTelemetryObjects();

            if (collection.events.Count != 0)
            {
                if ((first == 0 && first == last) || (first == collection.events.Count - 1 && first == last))
                {
                    first = 0;
                    last = collection.events.Count - 1;
                }

                //Segment the world in to blocks of the specified size encompassing all points
                Vector3 origin = collection.GetPointRange(first, last).center;

                //For each segment, collect all of the points inside and decide what data to watch based on the heatmap type
                float scaledHeatmapSize = heatmapSize / 100;
                Vector3 tempPoint;
                Dictionary<Vector3, HeatmapNode> heatmapNodes = new Dictionary<Vector3, HeatmapNode>();

                //For each segment, collect all of the points inside and decide what data to watch based on the heatmap type
                TelemetryEventGameObject tempTelemetryObject;

                if ((HeatmapValueMin == -1 && HeatmapValueMax == -1) || HeatmapValueMin == HeatmapValueMax)
                {
                    double largestValue = 0;
                    double smallestValue = 0;
                    int largestNumValue = 0;

                    if (useOrientation)
                    {
                        for (int j = first; j <= last; j++)
                        {
                            tempPoint = (collection.events[j].Point - origin) / heatmapSize;
                            tempPoint.x = (float)Math.Floor(tempPoint.x);
                            tempPoint.y = (float)Math.Floor(tempPoint.y);
                            tempPoint.z = (float)Math.Floor(tempPoint.z);

                            HeatmapNode tempNode;

                            if (heatmapNodes.ContainsKey(tempPoint))
                            {
                                heatmapNodes[tempPoint].numValues++;
                                heatmapNodes[tempPoint].values += collection.events[j].GetValue(subEvent);
                                heatmapNodes[tempPoint].orientation += collection.events[j].Orientation;
                                tempNode = heatmapNodes[tempPoint];
                            }
                            else
                            {
                                tempNode = new HeatmapNode(1, collection.events[j].GetValue(subEvent), collection.events[j].Orientation);
                                heatmapNodes.Add(tempPoint, tempNode);
                            }

                            smallestValue = Math.Min(smallestValue, tempNode.values / tempNode.numValues);
                            largestValue = Math.Max(largestValue, tempNode.values / tempNode.numValues);
                            largestNumValue = Math.Max(largestNumValue, tempNode.numValues);
                        }

                        foreach (var node in heatmapNodes)
                        {
                            node.Value.orientation = node.Value.orientation / node.Value.numValues;
                        }
                    }
                    else
                    {
                        for (int j = first; j <= last; j++)
                        {
                            tempPoint = (collection.events[j].Point - origin) / heatmapSize;
                            tempPoint.x = (float)Math.Floor(tempPoint.x);
                            tempPoint.y = (float)Math.Floor(tempPoint.y);
                            tempPoint.z = (float)Math.Floor(tempPoint.z);

                            HeatmapNode tempNode;

                            if (heatmapNodes.ContainsKey(tempPoint))
                            {
                                heatmapNodes[tempPoint].numValues++;
                                heatmapNodes[tempPoint].values += collection.events[j].GetValue(subEvent);
                                tempNode = heatmapNodes[tempPoint];
                            }
                            else
                            {
                                tempNode = new HeatmapNode(1, collection.events[j].GetValue(subEvent));
                                heatmapNodes.Add(tempPoint, tempNode);
                            }

                            smallestValue = Math.Min(smallestValue, tempNode.values / tempNode.numValues);
                            largestValue = Math.Max(largestValue, tempNode.values / tempNode.numValues);
                            largestNumValue = Math.Max(largestNumValue, tempNode.numValues);
                        }
                    }

                    HeatmapValueMin = 0;

                    if (heatmapType == (int)Globals.HeatmapType.Value || heatmapType == (int)Globals.HeatmapType.Value_Bar)
                    {
                        if (subEvent.StartsWith("pct_"))
                        {
                            HeatmapValueMin = 0;
                            HeatmapValueMax = 100;
                        }
                        else
                        {
                            HeatmapValueMin = smallestValue;
                            HeatmapValueMax = largestValue;
                        }
                    }
                    else
                    {
                        HeatmapValueMax = largestNumValue;
                    }
                }
                else
                {
                    if (useOrientation)
                    {
                        for (int j = first; j <= last; j++)
                        {
                            tempPoint = (collection.events[j].Point - origin) / heatmapSize;
                            tempPoint.x = (float)Math.Floor(tempPoint.x);
                            tempPoint.y = (float)Math.Floor(tempPoint.y);
                            tempPoint.z = (float)Math.Floor(tempPoint.z);

                            HeatmapNode tempNode;

                            if (heatmapNodes.ContainsKey(tempPoint))
                            {
                                heatmapNodes[tempPoint].numValues++;
                                heatmapNodes[tempPoint].values += collection.events[j].GetValue(subEvent);
                                heatmapNodes[tempPoint].orientation += collection.events[j].Orientation;
                                tempNode = heatmapNodes[tempPoint];
                            }
                            else
                            {
                                tempNode = new HeatmapNode(1, collection.events[j].GetValue(subEvent), collection.events[j].Orientation);
                                heatmapNodes.Add(tempPoint, tempNode);
                            }
                        }

                        foreach (var node in heatmapNodes)
                        {
                            node.Value.orientation = node.Value.orientation / node.Value.numValues;
                        }
                    }
                    else
                    {
                        for (int j = first; j <= last; j++)
                        {
                            tempPoint = (collection.events[j].Point - origin) / heatmapSize;
                            tempPoint.x = (float)Math.Floor(tempPoint.x);
                            tempPoint.y = (float)Math.Floor(tempPoint.y);
                            tempPoint.z = (float)Math.Floor(tempPoint.z);

                            HeatmapNode tempNode;

                            if (heatmapNodes.ContainsKey(tempPoint))
                            {
                                heatmapNodes[tempPoint].numValues++;
                                heatmapNodes[tempPoint].values += collection.events[j].GetValue(subEvent);
                                tempNode = heatmapNodes[tempPoint];
                            }
                            else
                            {
                                tempNode = new HeatmapNode(1, collection.events[j].GetValue(subEvent));
                                heatmapNodes.Add(tempPoint, tempNode);
                            }
                        }
                    }
                }

                if (heatmapNodes.Count > 0)
                {
                    double tempValue;
                    float tempColorValue;
                    int i = 0;

                    if (heatmapType == (int)Globals.HeatmapType.Value)
                    {
                        foreach (var node in heatmapNodes)
                        {
                            tempValue = node.Value.values / node.Value.numValues;
                            tempColorValue = (float)((tempValue - HeatmapValueMin) / (HeatmapValueMax - HeatmapValueMin));
                            tempColorValue = Mathf.Clamp(tempColorValue, 0, 1);

                            tempTelemetryObject = new TelemetryEventGameObject();
                            tempTelemetryObject.SetHeatmapEvent(i, (node.Key * heatmapSize) + origin, node.Value.orientation, heatmapColor.GetColorFromRange(tempColorValue), (PrimitiveType)heatmapShape, heatmapSize, tempValue);
                            CreateHeatmapObject(tempTelemetryObject);
                            i++;
                        }
                    }
                    else if (heatmapType == (int)Globals.HeatmapType.Population)
                    {
                        foreach (var node in heatmapNodes)
                        {
                            tempValue = node.Value.values / HeatmapValueMax;
                            tempColorValue = (float)((tempValue - HeatmapValueMin) / (HeatmapValueMax - HeatmapValueMin));
                            tempColorValue = Mathf.Clamp(tempColorValue, 0, 1);

                            tempTelemetryObject = new TelemetryEventGameObject();
                            tempTelemetryObject.SetHeatmapEvent(i, (node.Key * heatmapSize) + origin, node.Value.orientation, heatmapColor.GetColorFromRange(tempColorValue), (PrimitiveType)heatmapShape, heatmapSize, tempValue);
                            CreateHeatmapObject(tempTelemetryObject);
                            i++;
                        }
                    }
                    else if (heatmapType == (int)Globals.HeatmapType.Value_Bar)
                    {
                        float tempHeight;

                        foreach (var node in heatmapNodes)
                        {
                            tempValue = node.Value.values / node.Value.numValues;
                            tempColorValue = (float)((tempValue - HeatmapValueMin) / (HeatmapValueMax - HeatmapValueMin));
                            tempColorValue = Mathf.Clamp(tempColorValue, 0, 1);
                            tempHeight = (float)(tempValue / HeatmapValueMax) * heatmapSize;

                            tempTelemetryObject = new TelemetryEventGameObject();
                            tempTelemetryObject.SetHeatmapEvent(i, (node.Key * heatmapSize) + origin, Vector3.zero, heatmapColor.GetColorFromRange(tempColorValue), (PrimitiveType)heatmapShape, new Vector3(heatmapSize, heatmapSize, tempHeight), tempValue);
                            CreateHeatmapObject(tempTelemetryObject);
                            i++;
                        }
                    }
                    else if (heatmapType == (int)Globals.HeatmapType.Population_Bar)
                    {
                        float tempHeight;

                        foreach (var node in heatmapNodes)
                        {
                            tempValue = node.Value.values / HeatmapValueMax;
                            tempColorValue = (float)((tempValue - HeatmapValueMin) / (HeatmapValueMax - HeatmapValueMin));
                            tempColorValue = Mathf.Clamp(tempColorValue, 0, 1);
                            tempHeight = (float)tempValue * heatmapSize;

                            tempTelemetryObject = new TelemetryEventGameObject();
                            tempTelemetryObject.SetHeatmapEvent(i, (node.Key * heatmapSize) + origin, Vector3.zero, heatmapColor.GetColorFromRange(tempColorValue), (PrimitiveType)heatmapShape, new Vector3(heatmapSize, heatmapSize, tempHeight), tempValue);
                            CreateHeatmapObject(tempTelemetryObject);
                            i++;
                        }
                    }
                }
            }
        }

        // Set up and create the gameobject for a heatmap component
        public void CreateHeatmapObject(TelemetryEventGameObject inEvent)
        {
            gameObjectCollection.Add(inEvent);
            inEvent.GameObject.transform.SetParent(host.transform);
            UnityEngine.Object.DestroyImmediate(inEvent.GameObject.GetComponent<Collider>());
        }

        // Set up and create the gameobject for a telemetry event
        public void CreateTelemetryObject(int index, TelemetryEvent data, Color color, PrimitiveType shape)
        {
            TelemetryEventGameObject tempTelemetryObject = new TelemetryEventGameObject();
            tempTelemetryObject.SetEvent(data, color, shape, index);

            gameObjectCollection.Add(tempTelemetryObject);
            tempTelemetryObject.GameObject.transform.SetParent(host.transform);
            UnityEngine.Object.DestroyImmediate(tempTelemetryObject.GameObject.GetComponent<Collider>());
        }

        // Create the gameobjects for a list of telemetry events
        public void CreateTelemetryObjects(List<TelemetryEvent> data, Color color, PrimitiveType shape)
        {
            for(int i = 0; i < data.Count; i++)
            {
                CreateTelemetryObject(i, data[i], color, shape);
            }
        }

        // Create the gameobjects for a list of telemetry events in a specific range
        public void CreateTelemetryObjects(List<TelemetryEvent> data, int start, int end, Color color, PrimitiveType shape)
        {
            for (int i = start; i < data.Count && i < end; i++)
            {
                CreateTelemetryObject(i, data[i], color, shape);
            }
        }

        // Destroys all known gameobjects
        public void DestroyTelemetryObjects()
        {
            foreach(var gameObject in gameObjectCollection)
            {
                UnityEngine.Object.DestroyImmediate(gameObject.GameObject);
            }

            gameObjectCollection.Clear();
        }

        // Destroys the last known gameobject
        public void DestroyLastTelemetryObject()
        {
            if (gameObjectCollection.Count > 0)
            {
                UnityEngine.Object.DestroyImmediate(gameObjectCollection[gameObjectCollection.Count - 1].GameObject);
                gameObjectCollection.RemoveAt(gameObjectCollection.Count - 1);
            }
        }
    }
}
