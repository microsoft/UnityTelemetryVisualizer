// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// AnimationController.cs
//
// Controller for animation state
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections.Generic;

namespace GameTelemetry
{
    //Manages animation state based on the time an animation was started
    public class AnimationController
    {
        private Globals.TelemetryAnimationState state;
        private int nextIndexDraw;
        private bool needRefresh;
        private DateTime localStartTime;

        public float AnimSlider;
        public float AnimSliderMax = 1;

        public bool IsHeatmap = false;

        private EventEditorContainer eventContainer;
        public EventEditorContainer EventContainer
        {
            get
            {
                return eventContainer;
            }
            set
            {
                eventContainer = value;
            }
        }

        private int playSpeed;
        public int PlaySpeed
        {
            get
            {
                return playSpeed;
            }

            set
            {
                playSpeed = value;
            }
        }

        public TimeSpan GetTimespan()
        {
            return eventContainer.GetTimespan();
        }

        public Color Color
        {
            get
            {
                return eventContainer.Color;
            }
        }

        public PrimitiveType Type
        {
            get
            {
                return eventContainer.Type;
            }
        }

        public bool ShouldAnimate
        {
            get
            {
                return eventContainer.ShouldAnimate;
            }
        }

        public AnimationController()
        {
            state = Globals.TelemetryAnimationState.Stopped;
            playSpeed = 0;
            nextIndexDraw = 0;
            needRefresh = true;
        }

        public AnimationController(EventEditorContainer newContainer)
        {
            state = Globals.TelemetryAnimationState.Stopped;
            playSpeed = 0;
            nextIndexDraw = 0;
            needRefresh = true;
            eventContainer = newContainer;
        }

        public bool IsReady()
        {
            return eventContainer != null;
        }

        public bool IsPlaying()
        {
            return state == Globals.TelemetryAnimationState.Playing;
        }

        public bool IsPaused()
        {
            return state == Globals.TelemetryAnimationState.Paused;
        }

        public bool IsStopped()
        {
            return state == Globals.TelemetryAnimationState.Stopped;
        }

        public bool NeedRefresh()
        {
            bool ret = needRefresh;
            needRefresh = false;
            return ret;
        }

        public void Play(int speed)
        {
            if (eventContainer == null) return;

            if (state == Globals.TelemetryAnimationState.Playing && playSpeed == speed)
            {
                //Already playing at this speed, so pause
                Pause();
            }
            else if (state == Globals.TelemetryAnimationState.Playing)
            {
                //Need to play at different speed, so reset the start time to when it would have started
                //at the given speed
                localStartTime = DateTime.UtcNow;

                if (nextIndexDraw > 0)
                {
                    localStartTime -= eventContainer.events[nextIndexDraw].Time - eventContainer.events[eventContainer.events.Count - 1].Time;
                }
            }
            else if (state == Globals.TelemetryAnimationState.Stopped)
            {
                //Start playing from the beginning
                needRefresh = true;

                if (speed >= 0)
                {
                    nextIndexDraw = eventContainer.events.Count - 1;
                }
                else
                {
                    nextIndexDraw = 0;
                }

                localStartTime = DateTime.UtcNow;
                state = Globals.TelemetryAnimationState.Playing;
                eventContainer.ShouldAnimate = true;
            }
            else if (state == Globals.TelemetryAnimationState.Paused)
            {
                //Resume playing, so update start time to include the time this was paused
                localStartTime = DateTime.UtcNow;

                if (nextIndexDraw > 0)
                {
                    localStartTime -= eventContainer.events[nextIndexDraw].Time - eventContainer.events[eventContainer.events.Count - 1].Time;
                }

                state = Globals.TelemetryAnimationState.Playing;
                eventContainer.ShouldAnimate = true;
            }

            playSpeed = speed;
        }

        public void Pause()
        {
            state = Globals.TelemetryAnimationState.Paused;
            eventContainer.ShouldAnimate = false;
        }

        public void Stop()
        {
            state = Globals.TelemetryAnimationState.Stopped;
            playSpeed = 0;
            nextIndexDraw = eventContainer.events.Count - 1;
            eventContainer.ShouldAnimate = false;
        }

        public int GetNextIndex()
        {
            return nextIndexDraw;
        }

        public int GetPrevIndex()
        {
            if (nextIndexDraw <= 0)
            {
                return eventContainer.events.Count - 1;
            }

            return nextIndexDraw - 1;
        }

        //Provides an array of pointers to events that are ready to draw for the animation
        public List<TelemetryEvent> GetNextEvents()
        {
            if (nextIndexDraw <= 0)
            {
                Stop();
                eventContainer.ShouldDraw = true;
            }

            List<TelemetryEvent> newArray = new List<TelemetryEvent>();

            if (playSpeed >= 0)
            {
                DateTime tempTime = eventContainer.events[eventContainer.events.Count - 1].Time + new TimeSpan((DateTime.UtcNow - localStartTime).Ticks * playSpeed);

                while (nextIndexDraw >= 0 && eventContainer.events[nextIndexDraw].Time < tempTime)
                {
                    newArray.Add(eventContainer.events[nextIndexDraw]);
                    nextIndexDraw--;
                }

                if (newArray.Count == 0 && nextIndexDraw >= 0)
                {
                    //Skip ahead if the gap between events is too long
                    TimeSpan timeToNext = eventContainer.events[nextIndexDraw].Time - tempTime;
                    if (timeToNext > new TimeSpan(0, 0, 30))
                    {
                        localStartTime -= timeToNext - new TimeSpan(0, 0, 5);
                    }
                }
            }

            return newArray;
        }

        //Provides the index of the next event to draw
        public int GetNextEventCount()
        {
            if (nextIndexDraw <= 0)
            {
                Stop();
            }

            bool newEvents = false;

            if (playSpeed >= 0)
            {
                DateTime tempTime = eventContainer.events[eventContainer.events.Count - 1].Time + new TimeSpan((DateTime.UtcNow - localStartTime).Ticks * playSpeed);

                while (nextIndexDraw >= 0 && eventContainer.events[nextIndexDraw].Time < tempTime)
                {
                    newEvents = true;
                    nextIndexDraw--;
                }

                if (!newEvents && nextIndexDraw >= 0)
                {
                    //Skip ahead if the gap between events is too long
                    TimeSpan timeToNext = eventContainer.events[nextIndexDraw].Time - tempTime;
                    if (timeToNext > new TimeSpan(0, 0, 30))
                    {
                        localStartTime -= timeToNext - new TimeSpan(0, 0, 5);
                    }
                }

                if (nextIndexDraw < 0)
                {
                    nextIndexDraw = 0;
                }
            }

            return nextIndexDraw;
        }

        //Provides an array of pointers to events that are ready to draw for the animation (in reverse)
        public List<TelemetryEvent> GetPrevEvents()
        {
            List<TelemetryEvent> newArray = new List<TelemetryEvent>();

            if (playSpeed < 0)
            {
                if (nextIndexDraw >= eventContainer.events.Count - 1)
                {
                    Stop();
                    eventContainer.ShouldDraw = false;
                }

                DateTime tempTime = eventContainer.events[0].Time + new TimeSpan((DateTime.UtcNow - localStartTime).Ticks * playSpeed);

                while (nextIndexDraw < eventContainer.events.Count && eventContainer.events[nextIndexDraw].Time > tempTime)
                {
                    newArray.Add(eventContainer.events[nextIndexDraw]);
                    nextIndexDraw++;
                }

                if (newArray.Count == 0 && nextIndexDraw < eventContainer.events.Count)
                {
                    //Skip ahead if the gap between events is too long
                    TimeSpan timeToNext = eventContainer.events[nextIndexDraw].Time - tempTime;
                    if (timeToNext > new TimeSpan(0, 0, 30))
                    {
                        localStartTime += timeToNext - new TimeSpan(0, 0, 5);
                    }
                }
            }

            return newArray;
        }

        //Provides the index of the next event to draw (in reverse)
        public int GetPrevEventCount()
        {
            if (playSpeed < 0)
            {
                if (nextIndexDraw >= eventContainer.events.Count - 1)
                {
                    Stop();
                    //eventContainer.SetShouldDraw(false);
                }

                DateTime tempTime = eventContainer.events[0].Time + new TimeSpan((DateTime.UtcNow - localStartTime).Ticks * playSpeed);
                bool newEvents = false;

                while (nextIndexDraw < eventContainer.events.Count && eventContainer.events[nextIndexDraw].Time > tempTime)
                {
                    newEvents = true;
                    nextIndexDraw++;
                }

                if (!newEvents && nextIndexDraw < eventContainer.events.Count)
                {
                    //Skip ahead if the gap between events is too long
                    TimeSpan timeToNext = eventContainer.events[nextIndexDraw].Time - tempTime;
                    if (timeToNext > new TimeSpan(0, 0, 30))
                    {
                        localStartTime += timeToNext - new TimeSpan(0, 0, 5);
                    }
                }

                if (nextIndexDraw >= eventContainer.events.Count)
                {
                    nextIndexDraw = eventContainer.events.Count - 1;
                }
            }

            return nextIndexDraw;
        }

        //Set a time for playback  (0 to 1)
        public void SetPlaybackTime(float scale)
        {
            int newIndexDraw = GetEventIndexForTimeScale(scale);

            localStartTime = DateTime.UtcNow;

            if (newIndexDraw > 0 && newIndexDraw < eventContainer.events.Count - 1)
            {
                localStartTime -= eventContainer.events[newIndexDraw].Time - eventContainer.events[eventContainer.events.Count - 1].Time;
            }

            needRefresh = true;
        }

        public TimeSpan GetCurrentPlayTime()
        {
            if (playSpeed >= 0)
            {
                return new TimeSpan((DateTime.UtcNow - localStartTime).Ticks * playSpeed);
            }
            else
            {
                return eventContainer.GetTimespan() + new TimeSpan((DateTime.UtcNow - localStartTime).Ticks * playSpeed);
            }
        }

        public float GetTimeScaleFromTime()
        {
            return eventContainer.GetTimeScaleFromTime(new TimeSpan((DateTime.UtcNow - localStartTime).Ticks * playSpeed));
        }

        public float GetEventTimeScale(int index)
        {
            return eventContainer.GetEventTimeScale(index);
        }

        public int GetEventIndexForTimeScale(float scale)
        {
            return eventContainer.GetEventIndexForTimeScale(scale);
        }
    };
}