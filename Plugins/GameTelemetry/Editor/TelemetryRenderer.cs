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

        public float HeatmapValueMin = -1;
        public float HeatmapValueMax = -1;

        private List<TelemetryEventGameObject> gameObjectCollection = new List<TelemetryEventGameObject>();
        private bool needsTelemetryObjectUpdate = false;

        public void TriggerTelemetryUpdate()
        {
            needsTelemetryObjectUpdate = true;
        } 

        // Master draw call.  Updates animation if running, otherwise checks if objects need to be updated
        public void Tick(List<EventEditorContainer> filterCollection, float heatmapSize, HeatmapColors heatmapColor, int heatmapType, int heatmapShape, bool useOrientation, ref AnimationController animController)
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
                                GenerateHeatmap(tempContainer, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, next, tempContainer.events.Count - 1);
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
                                GenerateHeatmap(tempContainer, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, next, tempContainer.events.Count - 1);
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
        public void GenerateHeatmap(EventEditorContainer collection, float heatmapSize, HeatmapColors heatmapColor, int heatmapType, int heatmapShape, bool useOrientation)
        {
            GenerateHeatmap(collection, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, 0, collection.events.Count - 1);
        }

        public void GenerateHeatmap(EventEditorContainer collection, float heatmapSize, HeatmapColors heatmapColor, int heatmapType, int heatmapShape, bool useOrientation, int first, int last)
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
                Bounds range = collection.GetPointRange(first, last);
                float extent = heatmapSize / 2;
                Vector3 boxSize = new Vector3(heatmapSize, heatmapSize, heatmapSize);
                List<Bounds> parts = new List<Bounds>();

                range.max = new Vector3(range.max.x + heatmapSize, range.max.y + heatmapSize, range.max.z + heatmapSize);

                for (float x = range.min.x + extent; x < range.max.x; x += heatmapSize)
                {
                    for (float y = range.min.y + extent; y < range.max.y; y += heatmapSize)
                    {
                        for (float z = range.min.z + extent; z < range.max.z; z += heatmapSize)
                        {
                            parts.Add(new Bounds(new Vector3(x, y, z), boxSize));
                        }
                    }
                }

                if (parts.Count > 0)
                {
                    //For each segment, collect all of the points inside and decide what data to watch based on the heatmap type
                    List<int> numValues = new List<int>();
                    List<float> values = new List<float>();
                    List<Vector3> orientation = new List<Vector3>();
                    TelemetryEventGameObject tempTelemetryObject;

                    numValues.AddRange(System.Linq.Enumerable.Repeat(0, parts.Count));
                    values.AddRange(System.Linq.Enumerable.Repeat<float>(0, parts.Count));
                    orientation.AddRange(System.Linq.Enumerable.Repeat<Vector3>(new Vector3(), parts.Count));

                    if ((HeatmapValueMin == -1 && HeatmapValueMax == -1) || HeatmapValueMin == HeatmapValueMax)
                    {
                        float largestValue = 0;
                        int largestNumValue = 0;

                        if (useOrientation)
                        {
                            for (int i = 0; i < parts.Count; i++)
                            {
                                for (int j = first; j <= last; j++)
                                {
                                    if (parts[i].Contains(collection.events[j].Point))
                                    {
                                        numValues[i]++;
                                        orientation[i] += collection.events[j].Orientation;
                                        values[i] += collection.events[j].Value;
                                    }
                                }

                                largestValue = Math.Max(largestValue, values[i]);
                                largestNumValue = Math.Max(largestNumValue, numValues[i]);
                                orientation[i] = orientation[i] / numValues[i];
                            }
                        }
                        else
                        {
                            for (int i = 0; i < parts.Count; i++)
                            {
                                for (int j = first; j <= last; j++)
                                {
                                    if (parts[i].Contains(collection.events[j].Point))
                                    {
                                        numValues[i]++;
                                        values[i] += collection.events[j].Value;
                                    }
                                }

                                largestValue = Math.Max(largestValue, values[i]);
                                largestNumValue = Math.Max(largestNumValue, numValues[i]);
                            }
                        }

                        HeatmapValueMin = 0;

                        if (heatmapType == (int)Globals.HeatmapType.Value || heatmapType == (int)Globals.HeatmapType.Value_Bar)
                        {
                            if (collection.IsPercentage)
                            {
                                HeatmapValueMax = 100;
                            }
                            else
                            {
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
                            for (int i = 0; i < parts.Count; i++)
                            {
                                for (int j = first; j <= last; j++)
                                {
                                    if (parts[i].Contains(collection.events[j].Point))
                                    {
                                        numValues[i]++;
                                        orientation[i] += collection.events[j].Orientation;
                                        values[i] += collection.events[j].Value;
                                    }
                                }

                                orientation[i] = orientation[i] / numValues[i];
                            }
                        }
                        else
                        {
                            for (int i = 0; i < parts.Count; i++)
                            {
                                for (int j = first; j <= last; j++)
                                {
                                    if (parts[i].Contains(collection.events[j].Point))
                                    {
                                        numValues[i]++;
                                        values[i] += collection.events[j].Value;
                                    }
                                }
                            }
                        }
                    }

                    float tempColorValue;

                    if (heatmapType == (int)Globals.HeatmapType.Value)
                    {
                        for (int i = 0; i < parts.Count; i++)
                        {
                            if (values[i] > 0)
                            {
                                tempColorValue = (((float)values[i] / numValues[i]) - HeatmapValueMin) / (HeatmapValueMax - HeatmapValueMin);
                                tempColorValue = Math.Max(tempColorValue, 0);
                                tempColorValue = Math.Min(tempColorValue, (float)HeatmapValueMax);

                                tempTelemetryObject = new TelemetryEventGameObject();
                                tempTelemetryObject.SetHeatmapEvent(i, parts[i].center, orientation[i], heatmapColor.GetColorFromRange(tempColorValue), (PrimitiveType)heatmapShape, heatmapSize, values[i]);
                                CreateHeatmapObject(tempTelemetryObject);
                            }
                        }
                    }
                    else if (heatmapType == (int)Globals.HeatmapType.Population)
                    {
                        for (int i = 0; i < parts.Count; i++)
                        {
                            if (numValues[i] > 0)
                            {
                                tempColorValue = (float)(numValues[i] - HeatmapValueMin) / (HeatmapValueMax - HeatmapValueMin);
                                tempColorValue = Math.Max(tempColorValue, 0);
                                tempColorValue = Math.Min(tempColorValue, (float)HeatmapValueMax);

                                tempTelemetryObject = new TelemetryEventGameObject();
                                tempTelemetryObject.SetHeatmapEvent(i, parts[i].center, orientation[i], heatmapColor.GetColorFromRange(tempColorValue), (PrimitiveType)heatmapShape, heatmapSize, numValues[i]);
                                CreateHeatmapObject(tempTelemetryObject);
                            }
                        }
                    }
                    else if (heatmapType == (int)Globals.HeatmapType.Value_Bar)
                    {
                        float tempHeight;

                        for (int i = 0; i < parts.Count; i++)
                        {
                            if (values[i] > 0)
                            {
                                tempColorValue = (((float)values[i] / numValues[i]) - HeatmapValueMin) / (HeatmapValueMax - HeatmapValueMin);
                                tempColorValue = Math.Max(tempColorValue, 0);
                                tempColorValue = Math.Min(tempColorValue, (float)HeatmapValueMax);
                                tempHeight = (((float)values[i] / numValues[i]) / HeatmapValueMax) * heatmapSize;

                                tempTelemetryObject = new TelemetryEventGameObject();
                                tempTelemetryObject.SetHeatmapEvent(i, parts[i].center, Vector3.zero, heatmapColor.GetColorFromRange(tempColorValue), PrimitiveType.Cube, new Vector3(heatmapSize, heatmapSize, tempHeight), values[i]);
                                CreateHeatmapObject(tempTelemetryObject);
                            }
                        }
                    }
                    else if (heatmapType == (int)Globals.HeatmapType.Population_Bar)
                    {
                        float tempHeight;

                        for (int i = 0; i < parts.Count; i++)
                        {
                            if (numValues[i] > 0)
                            {
                                tempColorValue = (float)(numValues[i] - HeatmapValueMin) / (HeatmapValueMax - HeatmapValueMin);
                                tempColorValue = Math.Max(tempColorValue, 0);
                                tempColorValue = Math.Min(tempColorValue, (float)HeatmapValueMax);
                                tempHeight = ((float)numValues[i] / HeatmapValueMax) * heatmapSize;

                                tempTelemetryObject = new TelemetryEventGameObject();
                                tempTelemetryObject.SetHeatmapEvent(i, parts[i].center, Vector3.zero, heatmapColor.GetColorFromRange(tempColorValue), PrimitiveType.Cube, new Vector3(heatmapSize, heatmapSize, tempHeight), numValues[i]);
                                CreateHeatmapObject(tempTelemetryObject);
                            }
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
