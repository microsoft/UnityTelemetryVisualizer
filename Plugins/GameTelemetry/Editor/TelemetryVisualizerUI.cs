// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetryVisualizerUI.cs
//
// Provides interactivity for Telemetry events
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using QueryNodeList = System.Collections.Generic.List<GameTelemetry.QueryNode>;

namespace GameTelemetry
{
    public class GameTelemetryWindow : EditorWindow
    {
        private GameTelemetryRenderer renderer = new GameTelemetryRenderer();
        private bool queryFolddown = true;
        private bool searchFolddown = true;
        private bool vizFolddown = true;

        //Margin sizes
        private static float marginSize = 15;
        private static float textWidth = 130;
        private static float postTextMargin = textWidth + 20;
        private static float heatmapGap = 3;
        private static Color lineColor = new Color(.08f, .08f, .08f);

        //Query
        private List<QuerySetting> queryCollection = new List<QuerySetting>();
        private bool isWaiting = false;
        private QueryExecutor executor;
        private List<EventEditorContainer> queryEventCollection = new List<EventEditorContainer>();
        private List<EventEditorContainer> filterCollection = new List<EventEditorContainer>();

        //Search
        private List<string> eventNames = new List<string>();
        private string searchText = "";
        private string eventCount = "Found 0 events";

        //Animation
        private int vizSelectedEvent;
        private float heatmapSize = 2;
        private string animMaxTime = "0:00";
        private AnimationController animController = new AnimationController();

        //Heatmap
        private int vizSubSelectedEvent;
        private List<string> subEventNames = new List<string>();
        private int heatmapType = 0;
        private int heatmapShape = 3;
        private HeatmapColors heatmapColor = new HeatmapColors();
        private bool colorSelect = false;
        private bool useOrientation = false;

        //Set window defaults
        private void OnEnable()
        {
            //Generate blank clause
            if (queryCollection.Count == 0)
            {
                AddClause(-1);
            }
        }

        [MenuItem("Window/Game Telemetry")]
        public static void ShowTelemetryViewer()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(GameTelemetryWindow), false, "Game Telemetry");
        }

        private void Update()
        {
            if(subEventNames.Count > vizSubSelectedEvent && vizSubSelectedEvent > -1)
            {
                renderer.Tick(filterCollection, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, subEventNames[vizSubSelectedEvent], ref animController);
            }
            else
            {
                renderer.Tick(filterCollection, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, "", ref animController);
            }
        }

        void OnGUI()
        {
            GUILayout.Space(10);

            //Query section
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Height(1)), lineColor);
            queryFolddown = EditorGUILayout.Foldout(queryFolddown, "Event Settings");
            if(queryFolddown)
            {
                //GUILayout.Label("Create clauses to define what events are retrieved from the server. The local results are filtered in the search area", EditorStyles.wordWrappedLabel);
                EditorGUILayout.HelpBox("Create clauses to define what events are retrieved from the server. The local results are filtered in the search area", MessageType.Info);

                GenerateClauseList();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+ Add New Clause"))
                {
                    AddClause(-1);
                }

                if (isWaiting)
                {
                    GUILayout.Button("Running...");
                }
                else
                {
                    if (GUILayout.Button("Submit"))
                    {
                        SubmitQuery();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            //Search section
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Height(1)), lineColor);
            searchFolddown = EditorGUILayout.Foldout(searchFolddown, "Event Search");
            if (searchFolddown)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize * 2);
                searchText = EditorGUILayout.TextField("", searchText);
                if (GUILayout.Button("Search", GUILayout.Width(80)))
                {
                    FilterEvents();
                }
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize * 2);
                GUILayout.Label(eventCount, EditorStyles.label);
                EditorGUILayout.EndHorizontal();

                GenerateEventGroup();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize * 2);
                if (GUILayout.Button("Select All"))
                {
                    for (int i = 0; i < filterCollection.Count; i++)
                    {
                        filterCollection[i].ShouldDraw = true;
                    }
                }
                if (GUILayout.Button("Select None"))
                {
                    for (int i = 0; i < filterCollection.Count; i++)
                    {
                        filterCollection[i].ShouldDraw = false;
                    }
                }
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();
            }

            //Viz tools section
            EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Height(1)), lineColor);
            vizFolddown = EditorGUILayout.Foldout(vizFolddown, "Visualization Tools");
            if (vizFolddown)
            {
                GUILayout.Space(10);
                //Event group selection
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize);

                EditorGUILayout.LabelField("Event Type", GUILayout.Width(textWidth));
                int currSelectedEvent = EditorGUILayout.Popup("", vizSelectedEvent, eventNames.ToArray());
                if(vizSelectedEvent != currSelectedEvent)
                {
                    vizSelectedEvent = currSelectedEvent;

                    for (int i = 0; i < queryEventCollection.Count; i++)
                    {
                        if (queryEventCollection[i].Name == eventNames[vizSelectedEvent])
                        {
                            animController.EventContainer = queryEventCollection[i];
                            subEventNames = new List<string>(queryEventCollection[i].attributeNames);
                            break;
                        }
                    }

                    TimeSpan tempSpan = animController.GetTimespan();

                    if(tempSpan.Seconds >= 10)
                    {
                        animMaxTime = $"{(int)tempSpan.TotalMinutes}:{tempSpan.Seconds}";
                    }
                    else
                    {
                        animMaxTime = $"{(int)tempSpan.TotalMinutes}:0{tempSpan.Seconds}";
                    }

                    animController.AnimSliderMax = (float)tempSpan.TotalSeconds;

                    renderer.HeatmapValueMin = -1;
                    renderer.HeatmapValueMax = -1;
                }
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                //Animation
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(postTextMargin);

                float newSlider = GUILayout.HorizontalSlider(animController.AnimSlider, 0, animController.AnimSliderMax);
                if(animController.AnimSlider != newSlider)
                {
                    animController.AnimSlider = newSlider;
                    if(animController.IsReady())
                    {
                        animController.SetPlaybackTime(animController.AnimSlider / animController.AnimSliderMax);
                    }
                }
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(postTextMargin);
                EditorGUILayout.LabelField("0:00");
                GUIStyle rightAlign = new GUIStyle(GUI.skin.label);
                rightAlign.alignment = TextAnchor.MiddleRight;
                EditorGUILayout.LabelField(animMaxTime, rightAlign);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(postTextMargin);

                if (GUILayout.Button("<<<"))
                {
                    if (animController.IsReady()) PlayAnimation(-4);
                }
                if (GUILayout.Button("<<"))
                {
                    if (animController.IsReady()) PlayAnimation(-2);
                }
                if (GUILayout.Button("||"))
                {
                    if (animController.IsReady()) PlayAnimation(0);
                }
                if (GUILayout.Button(">"))
                {
                    PlayAnimation(1);
                }
                if (GUILayout.Button(">>"))
                {
                    if (animController.IsReady()) PlayAnimation(2);
                }
                if (GUILayout.Button(">>>"))
                {
                    if (animController.IsReady()) PlayAnimation(4);
                }
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);

                EditorGUI.DrawRect(EditorGUILayout.GetControlRect(GUILayout.Height(1)), lineColor);

                //Heatmap
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize);
                GUILayout.Label("Heatmap Settings");
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(heatmapGap);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize);
                EditorGUILayout.LabelField("Value", GUILayout.Width(textWidth));
                int currSelectedSubEvent = EditorGUILayout.Popup("", vizSubSelectedEvent, subEventNames.ToArray());
                if (vizSubSelectedEvent != currSelectedSubEvent)
                {
                    vizSubSelectedEvent = currSelectedSubEvent;
                    renderer.HeatmapValueMin = -1;
                    renderer.HeatmapValueMax = -1;
                }
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(heatmapGap);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize);
                EditorGUILayout.LabelField("Type", GUILayout.Width(textWidth));
                heatmapType = EditorGUILayout.Popup("", heatmapType, Globals.HeatmapTypeString);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(heatmapGap);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize);
                EditorGUILayout.LabelField("Shape", GUILayout.Width(textWidth));
                heatmapShape = EditorGUILayout.Popup("", heatmapShape, Globals.ShapeStrings);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(heatmapGap);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize);
                EditorGUILayout.LabelField("Shape Size", GUILayout.Width(textWidth));
                heatmapSize = EditorGUILayout.FloatField(heatmapSize);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(heatmapGap);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize);
                EditorGUILayout.LabelField("Color Range", GUILayout.Width(textWidth));
                heatmapColor.LowColor = EditorGUILayout.ColorField(new GUIContent(), heatmapColor.LowColor, false, true, false, GUILayout.Height(35));
                GUILayout.Space(marginSize);
                heatmapColor.HighColor = EditorGUILayout.ColorField(new GUIContent(), heatmapColor.HighColor, false, true, false, GUILayout.Height(35));
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(postTextMargin);
                GUILayout.Label("Minimum", EditorStyles.label);
                GUILayout.Space(marginSize);
                GUILayout.Label("Maximum", EditorStyles.label);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(heatmapGap);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize);
                EditorGUILayout.LabelField("Value Range", GUILayout.Width(textWidth));
                renderer.HeatmapValueMin = EditorGUILayout.DoubleField(renderer.HeatmapValueMin);
                GUILayout.Space(marginSize);
                renderer.HeatmapValueMax = EditorGUILayout.DoubleField(renderer.HeatmapValueMax);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(postTextMargin);
                GUILayout.Label("Minimum", EditorStyles.label);
                GUILayout.Space(marginSize);
                GUILayout.Label("Maximum", EditorStyles.label);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(marginSize);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(postTextMargin);
                useOrientation = EditorGUILayout.ToggleLeft("Use Orientation", useOrientation);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(postTextMargin);
                animController.IsHeatmap = EditorGUILayout.ToggleLeft("Apply to Animation", animController.IsHeatmap);
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(marginSize);

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(postTextMargin);
                if (GUILayout.Button("Generate"))
                {
                    if (queryEventCollection.Count == 0 || vizSelectedEvent < 0) return;

                    EventEditorContainer collection = new EventEditorContainer();

                    for (int i = 0; i < queryEventCollection.Count; i++)
                    {
                        queryEventCollection[i].ShouldAnimate = false;
                        queryEventCollection[i].ShouldDraw = false;

                        if (queryEventCollection[i].Name == eventNames[vizSelectedEvent])
                        {
                            collection = queryEventCollection[i];
                        }
                    }

                    if (vizSubSelectedEvent > 0 && vizSubSelectedEvent > subEventNames.Count)
                    {
                        renderer.GenerateHeatmap(collection, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, subEventNames[vizSubSelectedEvent]);
                    }
                    else
                    {
                        renderer.GenerateHeatmap(collection, heatmapSize, heatmapColor, heatmapType, heatmapShape, useOrientation, "");
                    }
                }
                GUILayout.Space(marginSize);
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);
        }

        // Creates list of query clauses
        void GenerateClauseList()
        {
            GUIStyle buttonStyle = EditorStyles.miniButton;
            buttonStyle.stretchWidth = false;

            GUILayout.BeginScrollView(new Vector2(), false, true, GUILayout.Height(200));

            for (int i = 0; i < queryCollection.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                // Plus
                if (GUILayout.Button(new GUIContent("+", "Add New Clause"), buttonStyle))
                {
                    AddClause(i);
                }

                if (i > 0)
                {
                    // And/Or
                    queryCollection[i].isAnd = Convert.ToBoolean(EditorGUILayout.Popup("", Convert.ToInt32(queryCollection[i].isAnd), Globals.AndOrStrings, GUILayout.Width(90)));
                }
                else
                {
                    GUILayout.Space(94);
                }

                // Category
                if(queryCollection[i].Field == Globals.QueryField.Other)
                {
                    queryCollection[i].OtherField = EditorGUILayout.TextField("", queryCollection[i].OtherField);
                }
                else
                {
                    queryCollection[i].Field = (Globals.QueryField)EditorGUILayout.Popup("", (int)queryCollection[i].Field, Globals.QueryFieldStrings, GUILayout.Width(100));
                }

                // ==
                queryCollection[i].Operator = (Globals.QueryOperator)EditorGUILayout.Popup("", (int)queryCollection[i].Operator, Globals.QueryOperatorStrings, GUILayout.Width(100));

                // Textbox
                queryCollection[i].Value = EditorGUILayout.TextField("", queryCollection[i].Value);

                // X
                if (GUILayout.Button("X", buttonStyle))
                {
                    RemoveClause(i);
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        // Add a clause to the group
        public void AddClause(int index)
        {
            if (index == -1)
            {
                queryCollection.Add(new QuerySetting());
            }
            else
            {
                queryCollection.Insert(index, new QuerySetting());
            }
        }

        // Remove clause from group
        public void RemoveClause(int index)
        {
            queryCollection.RemoveAt(index);
            
            if (queryCollection.Count == 0)
            {
                AddClause(-1);
            }
        }

        //Translates query UI in to query nodes for execution
        public void SubmitQuery()
        {
            QueryNodeList andNodes = new QueryNodeList();
            QueryNodeList orNodes = new QueryNodeList();

            queryCollection[0].isAnd = true;

            for (int i = 0; i < queryCollection.Count; i++)
            {
                QueryOp op = QueryOp.Eq;
                switch ((Globals.QueryOperator)queryCollection[i].Operator)
                {
                    case Globals.QueryOperator.Not_Equal:
                        op = QueryOp.Neq;
                        break;
                    case Globals.QueryOperator.GreaterThan:
                        op = QueryOp.Gt;
                        break;
                    case Globals.QueryOperator.LessThan:
                        op = QueryOp.Lt;
                        break;
                    case Globals.QueryOperator.GreaterThanOrEqual:
                        op = QueryOp.Gte;
                        break;
                    case Globals.QueryOperator.LessThanOrEqual:
                        op = QueryOp.Lte;
                        break;
                }

                JSONObj finalValue;
                float tempValue;

                if (queryCollection[i].Value.ToLower() == "true")
                {
                    finalValue = new JSONObj(true);
                }
                else if (queryCollection[i].Value.ToLower() == "false")
                {
                    finalValue = new JSONObj(false);
                }
                else if (float.TryParse(queryCollection[i].Value, out tempValue))
                {
                    finalValue = new JSONObj(tempValue);
                }
                else
                {
                    finalValue = new JSONObj($"\"{queryCollection[i].Value}\"");
                }

                if (queryCollection[i].isAnd)
                {
                    if(queryCollection[i].Field == Globals.QueryField.Other)
                    {
                        andNodes.Add(new QueryNode(QueryNodeType.Comparison, queryCollection[i].OtherField, op, finalValue));
                    }
                    else
                    {
                        andNodes.Add(new QueryNode(QueryNodeType.Comparison, Globals.QueryExpectedStrings[(int)queryCollection[i].Field], op, new JSONObj($"\"{queryCollection[i].Value}\"")));
                    }
                }
                else
                {
                    if (queryCollection[i].Field == Globals.QueryField.Other)
                    {
                        orNodes.Add(new QueryNode(QueryNodeType.Comparison, queryCollection[i].OtherField, op, finalValue));
                    }
                    else
                    {
                        orNodes.Add(new QueryNode(QueryNodeType.Comparison, Globals.QueryExpectedStrings[(int)queryCollection[i].Field], op, new JSONObj($"\"{queryCollection[i].Value}\"")));
                    }
                }
            }

            if (queryCollection.Count == 1)
            {
                CollectEvents(andNodes[0]);
            }
            else
            {
                if (orNodes.Count == 1)
                {
                    andNodes.Add(orNodes[0]);
                    CollectEvents(new QueryNode(QueryNodeType.Group, QueryOp.Or, andNodes));
                }
                else
                {
                    if (orNodes.Count > 1)
                    {
                        andNodes.Add(new QueryNode(QueryNodeType.Group, QueryOp.Or, orNodes));
                    }

                    CollectEvents(new QueryNode(QueryNodeType.Group, QueryOp.And, andNodes));
                }
            }
        }

        //Call to execute query
        public void CollectEvents(QueryNode query)
        {
            if (!isWaiting)
            {
                executor = renderer.Host.AddComponent<QueryExecutor>();
                executor.ExecuteCustomQuery(QuerySerializer.Serialize(query).ToString(), QueryResults);
                isWaiting = true;
            }
        }

        //Called when the query request completes.  Converts results to a local collection
        public void QueryResults(QueryResult results)
        {
            queryEventCollection.Clear();
            eventNames.Clear();
            subEventNames.Clear();

            string currentName;

            foreach (var newEvent in results.Events)
            {
                int currentIdx = -1;
                currentName = newEvent.Name;

                for (int i = 0; i < queryEventCollection.Count; i++)
                {
                    if (queryEventCollection[i].Name == currentName)
                    {
                        currentIdx = i;
                        break;
                    }
                }

                if (currentIdx == -1)
                {
                    queryEventCollection.Add(new EventEditorContainer(currentName, queryEventCollection.Count));
                    currentIdx = queryEventCollection.Count - 1;
                }

                queryEventCollection[currentIdx].AddEvent(newEvent);
            }

            foreach(var events in queryEventCollection)
            {
                eventNames.Add(events.Name);
            }

            vizSelectedEvent = -1;
            vizSubSelectedEvent = -1;
            searchFolddown = true;
            FilterEvents();

            if(eventNames.Count > 0)
            {
                vizFolddown = true;
            }

            isWaiting = false;
            DestroyImmediate(executor);
        }

        // Creates list of event groups
        void GenerateEventGroup()
        {
            GUILayout.BeginScrollView(Vector2.zero, false, true, GUILayout.MaxHeight(150));

            for (int i = 0; i < filterCollection.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(marginSize * 2);

                // Checkbox
                bool tempDraw = EditorGUILayout.ToggleLeft(filterCollection[i].Name, filterCollection[i].ShouldDraw);
                if (tempDraw != filterCollection[i].ShouldDraw)
                {
                    filterCollection[i].ShouldDraw = tempDraw;
                    renderer.TriggerTelemetryUpdate();
                }

                GUILayout.Space(marginSize);

                // Shape
                PrimitiveType tempShape = (PrimitiveType)EditorGUILayout.Popup("", (int)filterCollection[i].Type, Globals.ShapeStrings);
                if(tempShape != filterCollection[i].Type)
                {
                    filterCollection[i].Type = tempShape;
                    renderer.TriggerTelemetryUpdate();
                }

                // Color
                Color tempColor = EditorGUILayout.ColorField(new GUIContent(), filterCollection[i].Color, false, true, false);

                if (tempColor != filterCollection[i].Color)
                {
                    if (!colorSelect)
                    {
                        filterCollection[i].Color = tempColor;
                        renderer.TriggerTelemetryUpdate();
                        colorSelect = true;
                    }
                    else
                    {
                        colorSelect = false;
                    }
                }

                EditorGUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        //Trim the event group list based on search parameters
        public void FilterEvents()
        {
            int count = 0;
            filterCollection.Clear();

            if (searchText == string.Empty)
            {
                for (int i = 0; i < queryEventCollection.Count; i++)
                {
                    filterCollection.Add(queryEventCollection[i]);
                    count += queryEventCollection[i].events.Count;
                }
            }
            else
            {
                for (int i = 0; i < queryEventCollection.Count; i++)
                {
                    if (queryEventCollection[i].Name.Contains(searchText))
                    {
                        filterCollection.Add(queryEventCollection[i]);
                        count += queryEventCollection[i].events.Count;
                    }
                }
            }

            eventCount = $"Found {count} events";
            renderer.TriggerTelemetryUpdate();
        }


        // Starts animation
        public void PlayAnimation(int speed)
        {
            for (int i = 0; i < queryEventCollection.Count; i++)
            {
                queryEventCollection[i].ShouldDraw = false;
            }

            animController.Play(speed);
        }
    }
}
