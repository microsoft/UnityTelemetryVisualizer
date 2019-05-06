// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetryQuery.cs
//
// Provides query interface for event lookup
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

using QueryNodeList = System.Collections.Generic.List<GameTelemetry.QueryNode>;

namespace GameTelemetry
{
    //Delegate called with query results
    public delegate void QueryResultHandler(QueryResult result);

    //Query identifier (single comparison or container of nodes)
    public enum QueryNodeType
    {
        Comparison = 0,
        Group = 1
    };

    //Query operator
    public enum QueryOp
    {
        Eq = 0,
        Gt = 1,
        Gte = 2,
        Lt = 3,
        Lte = 4,
        Neq = 5,
        In = 6,
        Btwn = 7,
        And = 8,
        Or = 9,
    };

    //Nodes for query conditions and containers
    public class QueryNode
    {
        public QueryNodeType Type;
        public QueryOp Operator;
        public string Column;
        public QueryNodeList Children;
        public JSONObj Value;
    
        // Standard single value node
        public QueryNode(QueryNodeType type, string column, QueryOp op, JSONObj value)
        {
            this.Type = type;
            this.Column = column;
            this.Operator = op;
            this.Value = value;
            this.Children = new QueryNodeList();
        }

        // Overload for BETWEEN query nodes
        public QueryNode(QueryNodeType type, string column, QueryOp op, JSONObj value1, JSONObj value2)
        {
            // Between and In operators are the only ones that know to look at the values array
            Debug.Assert(op == QueryOp.Btwn || op == QueryOp.In);

            this.Type = type;
            this.Column = column;
            this.Operator = op;
            this.Children = new QueryNodeList();
            this.Value = JSONObj.obj;
            Value.Add(value1);
            Value.Add(value2);
        }

        // Overload for grouping nodes (AND, OR)
        public QueryNode(QueryNodeType type, QueryOp op, QueryNodeList childNodes)
        {
            this.Type = type;
            this.Operator = op;
            this.Column = "";
            this.Value = JSONObj.nullJO;
            this.Children = childNodes;
        }
    }

    //Helper for building query nodes
    class QBuilder
    {
        // Nodes for comparing a column to a single value (e.g. equals, greater than, etc.)
        private QueryNode CreateComparisonNode<T>(string column, QueryOp op, T value)
        {
            return new QueryNode(QueryNodeType.Comparison, column, op, JSONObj.Create(value));
        }

        // Nodes for comparing a column to multiple values (e.g. between)
        private QueryNode CreateComparisonNode<T>(string column, QueryOp op, T value1, T value2)
        {
            return new QueryNode(QueryNodeType.Comparison, column, op, JSONObj.Create(value1), JSONObj.Create(value2));
        }

        // Nodes for grouping multiple nodes together with a common query operator (e.g. and, or)
        private QueryNode CreateGroupNode(QueryOp op, QueryNodeList childNodes)
        {
            return new QueryNode(QueryNodeType.Group, op, childNodes);
        }

        public QueryNode Eq<T>(string column, T value)
        {
            return CreateComparisonNode(column, QueryOp.Eq, value);
        }

        public QueryNode Gt<T>(string column, T value)
        {
            return CreateComparisonNode(column, QueryOp.Gt, value);
        }

        public QueryNode Gte<T>(string column, T value)
        {
            return CreateComparisonNode(column, QueryOp.Gte, value);
        }

        public QueryNode Lt<T>(string column, T value)
        {
            return CreateComparisonNode(column, QueryOp.Lt, value);
        }

        public QueryNode Lte<T>(string column, T value)
        {
            return CreateComparisonNode(column, QueryOp.Lte, value);
        }

        public QueryNode Neq<T>(string column, T value)
        {
            return CreateComparisonNode(column, QueryOp.Neq, value);
        }

        public QueryNode In<T>(string column, List<T> values)
        {
            return CreateComparisonNode(column, QueryOp.In, values);
        }

        public QueryNode Btwn<T>(string column, T lowerBound, T upperBound)
        {
            return CreateComparisonNode(column, QueryOp.Btwn, lowerBound, upperBound);
        }

        public QueryNode And(QueryNodeList childNodes)
        {
            return CreateGroupNode(QueryOp.And, childNodes);
        }

        public QueryNode Or(QueryNodeList childNodes)
        {
            return CreateGroupNode(QueryOp.Or, childNodes);
        }
    }

    //Node to JSON serializer
    public static class QuerySerializer
    {
        public static string SerializeToString(QueryNode node)
        {
            return Serialize(node).ToString();
        }

        public static JSONObj Serialize(QueryNode node)
        {
            JSONObj masterObj = new JSONObj(JSONObj.ObjectType.OBJECT);

            masterObj.AddField("type", node.Type.ToString().ToLower());
            masterObj.AddField("op", node.Operator.ToString().ToLower());

            switch (node.Type)
            {
                case QueryNodeType.Group:
                    JSONObj innerObj = new JSONObj(JSONObj.ObjectType.ARRAY);

                    foreach (QueryNode Item in node.Children)
                    {
                        innerObj.Add(Serialize(Item));
                    }

                    masterObj.AddField("children", innerObj);
                    break;
                case QueryNodeType.Comparison:
                    masterObj.AddField("column", node.Column);
                    masterObj.AddField("value", node.Value);
                    break;
            }

            string test = masterObj.ToString();
            
            return masterObj;
        }
    }

    //Result data structure for query request
    public class QueryResult
    {
        public struct QueryResultHeader
        {
            public bool Success;
            public int Count;
            public long QueryTime;
        }

        public QueryResultHeader Header;

        public List<QueryEvent> Events;

        public QueryResult(string response)
        {
            Header = new QueryResultHeader();
            Events = new List<QueryEvent>();
            Parse(response);
        }

        public void Parse(string response)
        {
            JSONObj rootObject = new JSONObj(response);
            JSONObj headerObject = rootObject.GetField("header");
            JSONObj resultsObject = rootObject.GetField("results");

            if (headerObject != null)
            {
                headerObject.GetField(ref this.Header.Success, "success");
                headerObject.GetField(ref this.Header.Count, "count");
                headerObject.GetField(ref this.Header.QueryTime, "queryTime");
            }

            if (resultsObject != null)
            {
                foreach (JSONObj Event in resultsObject.list)
                {
                    this.Events.Add(new QueryEvent(Event));
                }
            }
        }
    }

    //Query execution
    public class QueryExecutor : MonoBehaviour
    {
        private int ConfiguredtakeLimit;

        public QueryExecutor()
        {
            ConfiguredtakeLimit = TelemetrySettings.QueryTakeLimit;
        }

        public void ExecuteCustomQuery(string queryText, QueryResultHandler handlerFunc)
        {
            ExecuteCustomQuery(queryText, handlerFunc, -1);
        }

        public void ExecuteCustomQuery(string queryText, QueryResultHandler handlerFunc, int takeLimit)
        {
            StartCoroutine(RunCustomQuery(queryText, handlerFunc, takeLimit));
        }

        System.Collections.IEnumerator RunCustomQuery(string queryText, QueryResultHandler handlerFunc, int takeLimit)
        {
            if (takeLimit < 0)
            {
                takeLimit = ConfiguredtakeLimit;
            }

            using (UnityWebRequest Request = CreateRequest(handlerFunc, takeLimit))
            {
                Request.method = UnityWebRequest.kHttpVerbPOST;

                var bytes = System.Text.Encoding.UTF8.GetBytes(queryText);
                Request.uploadHandler = new UploadHandlerRaw(bytes);
                Request.downloadHandler = new DownloadHandlerBuffer();

                yield return Request.SendWebRequest();

                if (Request.isNetworkError || Request.isHttpError)
                {
                    Debug.Log(Request.error);
                }
                else if(handlerFunc != null)
                {
                    QueryResult Result = new QueryResult(Request.downloadHandler.text);
                    handlerFunc(Result);
                }
            }
        }

        public void ExecuteCustomQuery(QueryResultHandler handlerFunc)
        {
            ExecuteDefaultQuery(handlerFunc, -1);
        }

        public void ExecuteDefaultQuery(QueryResultHandler handlerFunc, int takeLimit)
        {
            StartCoroutine(RunDefaultQuery(handlerFunc, takeLimit));
        }

        System.Collections.IEnumerator RunDefaultQuery(QueryResultHandler handlerFunc, int takeLimit)
        {
            if (takeLimit < 0)
            {
                takeLimit = ConfiguredtakeLimit;
            }

            using (UnityWebRequest request = CreateRequest(handlerFunc, takeLimit))
            {
                request.method = UnityWebRequest.kHttpVerbGET;

                request.downloadHandler = new DownloadHandlerBuffer();

                yield return request.SendWebRequest();

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.Log(request.error);
                }
                else if (handlerFunc != null)
                {
                    QueryResult Result = new QueryResult(request.downloadHandler.text);
                    handlerFunc(Result);
                }
            }
        }

        private UnityWebRequest CreateRequest(QueryResultHandler handlerFunc, int takeLimit)
        {
            UnityWebRequest wr = new UnityWebRequest();
            wr.url = TelemetrySettings.QueryUrl + "?take=" + takeLimit;

            if (TelemetrySettings.AuthenticationKey.Length > 0)
            {
                wr.SetRequestHeader("x-functions-key", TelemetrySettings.AuthenticationKey);
            }

            wr.SetRequestHeader("Content-Type", "application/json");
            wr.SetRequestHeader("x-ms-payload-type", "batch");
            wr.SetRequestHeader("User-Agent", "X-UnityEngine-Agent");
            wr.timeout = 30;
            return wr;
        }
    }
}
