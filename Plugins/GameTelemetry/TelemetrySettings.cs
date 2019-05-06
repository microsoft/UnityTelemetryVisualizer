// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
//--------------------------------------------------------------------------------------
// TelemetrySettings.cs
//
// Telemetry settings
//
// Advanced Technology Group (ATG)
// Copyright (C) Microsoft Corporation. All rights reserved.
//--------------------------------------------------------------------------------------

namespace GameTelemetry
{
	public static class TelemetrySettings
	{
		public static string QueryUrl = "<PLACE QUERY URL HERE>";
		public static string IngestUrl = "<PLACE INGEST URL HERE>";
		public static float SendInterval = 30;
		public static float TickInterval = 1;
		public static int MaxBufferSize = 128;
		public static int QueryTakeLimit = 10000;
		public static string AuthenticationKey = "<PLACE KEY HERE>";
	}
}