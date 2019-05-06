// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetryManager.cs
//
// System for submitting telemetry events
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace GameTelemetry
{
    public sealed class TelemetryManager
    {
        // Singleton instance
        private static TelemetryManager instance = new TelemetryManager();
        public static TelemetryManager Instance
        {
            get
            {
                return instance;
            }
        }

        // Common properties which are sent with all events
        private TelemetryBuilder commonProperties;
        private TelemetryWorker worker;

        private long seqNum = 0;

        private bool hasInit = false;

        private TelemetryManager()
        {
            this.commonProperties = new TelemetryBuilder();
        }

        // Singleton for common properties
        // Intialization of the singleton fromm the provided configuration
        public void Initialize(GameObject host)
        {
            if (hasInit)
            {
                instance.Shutdown();
            }
            else
            {
                //Build type
                instance.commonProperties.SetProperty(QueryIds.BuildType, Application.buildGUID);

                //Platform
                instance.commonProperties.SetProperty(QueryIds.Platform, Application.platform.ToString());

                //Client ID
                instance.commonProperties.SetProperty(QueryIds.ClientId, SystemInfo.deviceUniqueIdentifier);

                //Session ID
                instance.commonProperties.SetProperty(QueryIds.SessionId, System.Guid.NewGuid().ToString());

                //Build ID
                instance.commonProperties.SetProperty(QueryIds.BuildId, Application.version);

                //Process ID
                instance.commonProperties.SetProperty(QueryIds.ProcessId, System.Diagnostics.Process.GetCurrentProcess().Id.ToString());

                //User ID
                instance.commonProperties.SetProperty(QueryIds.UserId, System.Environment.UserName);
            }

            worker = host.AddComponent<TelemetryWorker>();
            worker.StartThread(TelemetrySettings.IngestUrl, TelemetrySettings.SendInterval, TelemetrySettings.TickInterval, TelemetrySettings.MaxBufferSize, TelemetrySettings.AuthenticationKey);
            hasInit = true;
        }

        // Set a new client id - Usually set to the platform client id
        // If setting this again after initialization, consider flushing existing buffered telemetry or they may be incorrectly associated with the new id
        public void SetClientId(string inClientId)
        {
            instance.commonProperties.SetProperty(Telemetry.ClientId(inClientId));
        }

        // Set a new session id - Usually set when the game would like to differentiate between play sessions (such as changing users)
        // If setting this more than once, consider flushing existing buffered telemetry or they may be incorrectly associated with the new id
        public void SetSessionId(string inSessionId)
        {
            instance.commonProperties.SetProperty(Telemetry.SessionId(inSessionId));
        }

        // Set a common property which will be included in all telemetry sent
        public void SetCommonProperty<T>(string name, T value)
        {
            instance.commonProperties.SetProperty(name, value);
        }

        // Set a common property which will be included in all telemetry sent
        public void SetCommonProperty(TelemetryProperty property)
        {
            instance.commonProperties.SetProperty(property);
        }

        // Set a common property which will be included in all telemetry sent
        public TelemetryProperties GetCommonProperties()
        {
            return instance.commonProperties;
        }

        // Get the currently set client id
        public static string GetClientId()
        {
            return instance.commonProperties[QueryIds.ClientId].ToString();
        }

        // Get the currently set session id
        public static string GetSessionId()
        {
            return instance.commonProperties[QueryIds.SessionId].ToString();
        }

        // Records an event and places it in the buffer to be sent
        public void Record(string name, string category, string version, TelemetryBuilder propertiesBuilder)
        {
            if (hasInit)
            {
                TelemetryBuilder Evt = propertiesBuilder;

                Evt.SetProperty(Telemetry.ClientTimestamp());
                Evt.SetProperty(Telemetry.EventName(name));
                Evt.SetProperty(Telemetry.Category(category));
                Evt.SetProperty(Telemetry.Version(version));

                Evt.SetProperty("seq", System.Threading.Interlocked.Increment(ref seqNum));

                worker.Enqueue(Evt);
            }
            else
            {
                Debug.Log("Cannot record event because the telemetry subsystem has not been initialized.");
            }
        }

        // Flushes any pending telemetry and shuts down the singleton
        public void Shutdown()
        {
            if (hasInit)
            {
                worker.Exit();
            }
        }
    }

    // Payload used by worker thread
    public class TelemetryBatchPayload
    {
        public string Payload;
        private JSONObj headerObject;
        private System.Collections.Generic.List<JSONObj> eventList;

        public TelemetryBatchPayload(TelemetryProperties common)
        {
            headerObject = new JSONObj(JSONObj.ObjectType.OBJECT);
            eventList = new System.Collections.Generic.List<JSONObj>();

            foreach (var Prop in common)
            {
                headerObject.AddField(Prop.Key, Prop.Value);
            }
        }

        public void AddTelemetry(TelemetryBuilder inEvent)
        {
            JSONObj eventObject = new JSONObj(JSONObj.ObjectType.OBJECT);

            foreach (var Prop in inEvent)
            {
                eventObject.AddField(Prop.Key, Prop.Value);
            }

            eventList.Add(eventObject);
        }

        public string Finalize()
        {
            JSONObj finalObject = new JSONObj(JSONObj.ObjectType.OBJECT);
            JSONObj eventListObject = new JSONObj(JSONObj.ObjectType.OBJECT);

            foreach(var telEvent in eventList)
            {
                eventListObject.Add(telEvent);
            }

            finalObject.AddField("header", headerObject);
            finalObject.AddField("events", eventListObject);

            Payload = finalObject.Print();
            return Payload;
        }
    }

    // Worker thread for sending telemetry
    public class TelemetryWorker : MonoBehaviour
    {
        private float sendInterval;
        private float tickInterval;
        private float currentTime;

        private int pendingBufferSize;
        private bool shouldRun;
        private bool isComplete;
        private string ingestUrl;
        private string authenticationKey;

        private System.Collections.Generic.Queue<TelemetryBuilder> pending;

        public TelemetryWorker()
        {
            this.shouldRun = false;
            this.isComplete = false;
        }

        public void StartThread(string ingestUrl, float sendInterval = 30, float tickInterval = 1, int pendingBufferSize = 127, string authKey = "")
        {
            this.ingestUrl = ingestUrl;
            this.sendInterval = sendInterval;
            this.tickInterval = tickInterval;
            this.authenticationKey = authKey;
            
            this.shouldRun = true;
            this.pending = new System.Collections.Generic.Queue<TelemetryBuilder>();
            this.pendingBufferSize = pendingBufferSize;
            currentTime = 0;

            StartCoroutine(Execute());
        }

        IEnumerator Execute()
        {
            while (shouldRun)
            {
                while(currentTime < sendInterval)
                {
                    if (pending.Count >= pendingBufferSize) break;

                    yield return new WaitForSeconds(tickInterval);
                    currentTime += tickInterval;
                }

                if (pending.Count > 0)
                {
                    TelemetryProperties commonProperties = TelemetryManager.Instance.GetCommonProperties();
                    yield return StartCoroutine(SendTelemetry(commonProperties));
                }

                currentTime = 0;
            }
        }

        public void Stop()
        {
            shouldRun = false;
            isComplete = true;
        }

        public void Exit()
        {
            if (!isComplete)
            {
                Stop();
            }
        }

        public void Enqueue(TelemetryBuilder Properties)
        {
            if(pending != null)
            {
                pending.Enqueue(Properties);
            }
        }

        IEnumerator SendTelemetry(TelemetryProperties commonProperties)
        {
            TelemetryBatchPayload BatchPayload = new TelemetryBatchPayload(commonProperties);

            while (pending.Count > 0)
            {
                BatchPayload.AddTelemetry(pending.Dequeue());
            }

            string payload = BatchPayload.Finalize();

            using (UnityWebRequest wr = new UnityWebRequest())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
                wr.url = ingestUrl;
                wr.method = UnityWebRequest.kHttpVerbPOST;
                UploadHandler uploader = new UploadHandlerRaw(bytes);
                wr.uploadHandler = uploader;

                if (authenticationKey.Length > 0)
                {
                    wr.SetRequestHeader("x-functions-key", authenticationKey);
                }

                wr.SetRequestHeader("Content-Type", "application/json");
                wr.SetRequestHeader("x-ms-payload-type", "batch");
                wr.timeout = 30;

                yield return wr.SendWebRequest();
                if (wr.isNetworkError || wr.isHttpError)
                {
                    Debug.Log(wr.error);
                }
            }
        }
    }
}