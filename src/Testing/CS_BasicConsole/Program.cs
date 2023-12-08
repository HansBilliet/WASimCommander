/*
This file is part of the WASimCommander project.
https://github.com/mpaperno/WASimCommander

COPYRIGHT: (c) Maxim Paperno; All Rights Reserved.

This file may be used under the terms of the GNU General Public License (GPL)
as published by the Free Software Foundation, either version 3 of the Licenses,
or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

A copy of the GNU GPL is included with this project
and is also available at <http://www.gnu.org/licenses/>.
*/

#define SimCustomEventTests // ADDITIONAL TESTS for Simulator Custom Events

using System;
using System.Threading;
using WASimCommander.CLI;
using WASimCommander.CLI.Enums;
using WASimCommander.CLI.Structs;
using WASimCommander.CLI.Client;

namespace CS_BasicConsole
{
	internal class Program
	{
		// We use unique IDs to identify data subscription requests. I put the data type in the name just to keep them straight.
		private enum Requests : uint {
			REQUEST_ID_1_FLOAT,
			REQUEST_ID_2_STR
		}
		// A wait handle is used in this test/demo to keep the program alive while data processes in the background.
		private static readonly AutoResetEvent dataUpdateEvent = new AutoResetEvent(false);

		static void Main(string[] _)
		{
			Log("Initializing WASimClient...");

			// Create
			WASimClient client = new WASimClient(0xC57E57E9);  // "CSTESTER"  :)

			// Monitor client state changes.
			client.OnClientEvent += ClientStatusHandler;
			// Subscribe to incoming log record events.
			client.OnLogRecordReceived += LogHandler;

			// As a test, set Client's callback logging level to display messages in the console.
			client.setLogLevel(LogLevel.Info, LogFacility.Remote, LogSource.Client);
			// Set client's console log level to None to avoid double logging to our console. (Client also logs to a file by default.)
			client.setLogLevel(LogLevel.None, LogFacility.Console, LogSource.Client);
			// Lets also see some log messages from the server
			client.setLogLevel(LogLevel.Info, LogFacility.Remote, LogSource.Server);

			HR hr;  // store method invocation results for logging

			// Connect to Simulator (SimConnect) using default timeout period and network configuration (local Simulator)
			if ((hr = client.connectSimulator()) != HR.OK) {
				Log("Cannot connect to Simulator, quitting. Error: " + hr.ToString(), "XX");
				client.Dispose();
				return;
			}

			// Ping the WASimCommander server to make sure it's running and get the server version number (returns zero if no response).
			UInt32 version = client.pingServer();
			if (version == 0) {
				Log("Server did not respond to ping, quitting.", "XX");
				client.Dispose();
				return;
			}
			// Decode version number to dotted format and print it
			Log($"Found WASimModule Server v{version >> 24}.{(version >> 16) & 0xFF}.{(version >> 8) & 0xFF}.{version & 0xFF}");

			// Try to connect to the server, using default timeout value.
			if ((hr = client.connectServer()) != HR.OK) {
				Log("Server connection failed, quitting. Error: " + hr.ToString(), "XX");
				client.Dispose();
				return;
			}

			#region SimCustomEventTests - tests for registerCustomEvent and adapted sendKeyEvent code - FlyByWire A32NX addon required 

#if SimCustomEventTests // ADDITIONAL TESTS for Simulator Custom Events START -----------------------------------

			// We're connected with the sim, not with the server
			uint customEventId_SPD_INC = 0; // A32NX.FCU_SPD_INC
			uint customEventId_SPD_DEC = 0; // A32NX.FCU_SPD_DEC
			uint customEventId_APU_TGL = 0; // APU_GENERATOR_SWITCH_TOGGLE
			uint customEventId_INC_SET = 0; // A32NX.FCU_ALT_INCREMENT_SET
			uint dummy;

			// register A32NX.FCU_SPD_INC first time
			hr = client.registerCustomEvent("A32NX.FCU_SPD_INC", out customEventId_SPD_INC);
			Log($"registerCustomEvent A32NX.FCU_SPD_INC with ID {customEventId_SPD_INC} returns {hr}", "<<");

			// register A32NX.FCU_SPD_INC second time
			hr = client.registerCustomEvent("A32NX.FCU_SPD_INC", out dummy);
			Log($"registerCustomEvent A32NX.FCU_SPD_INC with ID {dummy} returns {hr}", "<<");

			// register APU_GENERATOR_SWITCH_TOGGLE second time
			hr = client.registerCustomEvent("APU_GENERATOR_SWITCH_TOGGLE", out customEventId_APU_TGL);
			Log($"registerCustomEvent APU_GENERATOR_SWITCH_TOGGLE with ID {customEventId_APU_TGL} returns {hr}", "<<");

			// Increase SPD by Id - Simulator Custom Event
			hr = client.sendKeyEvent(customEventId_SPD_INC);
			Log($"sendKeyEvent A32NX.FCU_SPD_INC with ID {customEventId_SPD_INC} returns {hr}", "<<");
			// Increase SPD by name - Simulator Custom Event
			hr = client.sendKeyEvent("A32NX.FCU_SPD_INC");
			Log($"sendKeyEvent A32NX.FCU_SPD_INC with ID {customEventId_SPD_INC} returns {hr}", "<<");
			// Toggle APU Generator Switch - Simulator Key Event
			hr = client.sendKeyEvent("APU_GENERATOR_SWITCH_TOGGLE");
			Log($"sendKeyEvent APU_GENERATOR_SWITCH_TOGGLE with ID {customEventId_APU_TGL} returns {hr}", "<<");

			// Send not registered event
			hr = client.sendKeyEvent(customEventId_SPD_INC + 1000);
			Log($"sendKeyEvent UNKNOWN with ID {customEventId_SPD_INC + 1000} returns {hr}", "<<");
			hr = client.sendKeyEvent("UNKNOWN.UNKNOWN");
			Log($"sendKeyEvent UNKNOWN.UNKNOWN with ID ???? returns {hr}", "<<");

			// Disconnect simulator to test pending registrations
			client.disconnectSimulator();

			// register A32NX.FCU_SPD_DEC
			hr = client.registerCustomEvent("A32NX.FCU_SPD_DEC", out customEventId_SPD_DEC);
			Log($"registerCustomEvent A32NX.FCU_SPD_DEC with ID {customEventId_SPD_DEC} returns {hr}", "<<");

			// register A32NX.FCU_ALT_INCREMENT_SET
			hr = client.registerCustomEvent("A32NX.FCU_ALT_INCREMENT_SET", out customEventId_INC_SET);
			Log($"registerCustomEvent A32NX.FCU_ALT_INCREMENT_SET with ID {customEventId_INC_SET} returns {hr}", "<<");

			// trigger all events while simulator is disconnected
			hr = client.sendKeyEvent(customEventId_SPD_INC);
			Log($"sendKeyEvent A32NX.FCU_SPD_INC with ID {customEventId_SPD_INC} returns {hr}", "<<");
			hr = client.sendKeyEvent("A32NX.FCU_SPD_DEC");
			Log($"sendKeyEvent A32NX.FCU_SPD_DEC with ID {customEventId_SPD_DEC} returns {hr}", "<<");
			hr = client.sendKeyEvent(customEventId_INC_SET, 100);
			Log($"sendKeyEvent A32NX.FCU_ALT_INCREMENT_SET=100 with ID {customEventId_INC_SET} returns {hr}", "<<");
			hr = client.sendKeyEvent("A32NX.FCU_ALT_INCREMENT_SET", 1000);
			Log($"sendKeyEvent A32NX.FCU_ALT_INCREMENT_SET=1000 with ID {customEventId_INC_SET} returns {hr}", "<<");

			// Connect to Simulator (SimConnect) using default timeout period and network configuration (local Simulator)
			if ((hr = client.connectSimulator()) != HR.OK)
			{
				Log("Cannot connect to Simulator, quitting. Error: " + hr.ToString(), "XX");
				client.Dispose();
				return;
			}

			// Try to connect to the server, using default timeout value.
			if ((hr = client.connectServer()) != HR.OK)
			{
				Log("Server connection failed, quitting. Error: " + hr.ToString(), "XX");
				client.Dispose();
				return;
			}

			// trigger all events while simulator is connected
			hr = client.sendKeyEvent(customEventId_SPD_INC);
			Log($"sendKeyEvent A32NX.FCU_SPD_INC with ID {customEventId_SPD_INC} returns {hr}", "<<");
			hr = client.sendKeyEvent("A32NX.FCU_SPD_DEC");
			Log($"sendKeyEvent A32NX.FCU_SPD_DEC with ID {customEventId_SPD_DEC} returns {hr}", "<<");
			hr = client.sendKeyEvent(customEventId_INC_SET, 100);
			Log($"sendKeyEvent A32NX.FCU_ALT_INCREMENT_SET=100 with ID {customEventId_INC_SET} returns {hr}", "<<");
			hr = client.sendKeyEvent("A32NX.FCU_ALT_INCREMENT_SET", 1000);
			Log($"sendKeyEvent A32NX.FCU_ALT_INCREMENT_SET=1000 with ID {customEventId_INC_SET} returns {hr}", "<<");

#endif // ADDITIONAL TESTS for Simulator Custom Events END ----------------------------------

			#endregion SimCustomEventTests

			// set up a Simulator Variable for testing.
			const string simVarName = "CG PERCENT";
			const string simVarUnit = "percent";

			// Execute a calculator string with result. We'll try to read the value of the SimVar defined above.
			const string calcCode = $"(A:{simVarName},{simVarUnit})";
			if (client.executeCalculatorCode(calcCode, CalcResultType.Double, out double fResult, out string sResult) == HR.OK)
				Log($"Calculator code '{calcCode}' returned: {fResult} and '{sResult}'", "<<");

			// Get a named Sim Variable value, same one as before, but directly using the Gauge API function aircraft_varget()
			if (client.getVariable(new VariableRequest(simVarName, simVarUnit, 0), out double varResult) == HR.OK)
				Log($"Get SimVar '{simVarName}, {simVarUnit}' returned: {varResult}", "<<");

			// Create and/or Set a Local variable to play with (will be created if it doesn't exist yet, will exist if this program has run during the current simulator session).
			const string variableName = "WASIM_CS_TEST_VAR_1";
			if (client.setOrCreateLocalVariable(variableName, 1.0) == HR.OK)
				Log($"Created/Set local variable {variableName}");

			// We can check that our new variable exists by looking up its ID.
			if (client.lookup(LookupItemType.LocalVariable, variableName, out var localVarId) == HR.OK)
				Log($"Got ID: 0x{localVarId:X} for local variable {variableName}", "<<");
			else
				Log($"Got local variable {variableName} was not found, something went wrong!", "!!");

			// Assuming our variable was created/exists, lets "subscribe" to notifications about when its value changes.
			// First set up the event handler to get the updates.
			client.OnDataReceived += DataSubscriptionHandler;
			// We subscribe to it using a "data request" and set it up to return as a float value, using a predefined value type (we could also use `4` here, for the number of bytes in a float).
			// This should also immediately return the current value, which will be delivered to the DataSubscriptionHandler we assigned earlier.
			if (client.saveDataRequest(new DataRequest((uint)Requests.REQUEST_ID_1_FLOAT, 'L', variableName, ValueTypes.DATA_TYPE_FLOAT)) == HR.OK)
				Log($"Subscribed to value changes for local variable {variableName}.");

			// Now let's change the value of our local variable and watch the updates come in via the subscription.
			for (float i = 1.33f; i < 10.0f; i += 0.89f) {
				if (client.setLocalVariable(variableName, i) == HR.OK)
					Log($"Set local variable {variableName} to {i}", ">>");
				else
					break;
				// wait for updates to process asynchronously and our event handler to get called, or time out
				if (!dataUpdateEvent.WaitOne(1000)) {
					Log("Data subscription update timed out!", "!!");
					break;
				}
			}

			// Test subscribing to a string type value. We'll use the Sim var "TITLE" (airplane name), which can only be retrieved using calculator code.
			// We allocate 32 Bytes here to hold the result and we request this one with an update period of Once, which will return a result right away
			// but will not be scheduled for regular updates. If we wanted to update this value later, we could call the client's `updateDataRequest(requestId)` method.
			// Also we can use the "async" version which doesn't wait for the server to respond before returning. We're going to wait for a result anyway after submitting the request.
			hr = client.saveDataRequestAsync(new DataRequest(
				requestId: (uint)Requests.REQUEST_ID_2_STR,
				resultType: CalcResultType.String,
				calculatorCode: "(A:TITLE, String)",
				valueSize: 32,
				period: UpdatePeriod.Once,
				interval: 0,
				deltaEpsilon: 0.0f)
			);
			if (hr == HR.OK) {
				Log($"Requested TITLE variable.", ">>");
				if (!dataUpdateEvent.WaitOne(1000))
					Log("Data subscription update timed out!", "!!");
			}

			// Test getting a list of our data requests back from the Client.
			Log("Saved Data Requests:", "::");
			var requests = client.dataRequests();
			foreach (DataRequestRecord dr in requests)
				Log(dr.ToString());  // Another convenient ToString() override for logging

			// OK, that was fun... now remove the data subscriptions. We could have done it in the loop above but we're testing things here...
			// The server will also remove all our subscriptions when we disconnect, but it's nice to be polite!
			var requestIds = client.dataRequestIdsList();
			foreach (uint id in requestIds)
				client.removeDataRequest(id);

			// Get a list of all local variables...
			// Connect to the list results Event
			client.OnListResults += ListResultsHandler;
			// Request the list.
			client.list(LookupItemType.LocalVariable);
			// Results will arrive asynchronously, so we wait a bit.
			if (!dataUpdateEvent.WaitOne(3000))
				Log("List results timed out!", "!!");

			// Look up a Key Event ID by name;
			const string eventName = "ATC_MENU_OPEN";  // w/out the "KEY_" prefix.
			if (client.lookup(LookupItemType.KeyEventId, eventName, out var varId) == HR.OK)
				Log($"Got lookup ID: 0x{varId:X} for {eventName}", "<<");

			// Try sending a Command directly to the server and wait for a response (Ack/Nak).
			// In this case we'll use a "SendKey" command to activate a Key Event by the ID which we just looked up
			if (client.sendCommandWithResponse(new Command() { commandId = CommandId.SendKey, uData = (uint)varId }, out var cmdResp) == HR.OK)
				Log($"Got response for SendKey command: {cmdResp}", "<<");

			// Shutdown (really just the Dispose() will close any/all connections anyway, but this is for example).
			client.disconnectServer();
			client.disconnectSimulator();
			// delete the client
			client.Dispose();
		}


		// This is an event handler for printing Client and Server log messages
		static void LogHandler(LogRecord lr, LogSource src)
		{
			Log($"{src} Log: {lr}", "@@");  // LogRecord has a convenience ToString() override
		}

		// Event handler to print the current Client status.
		static void ClientStatusHandler(ClientEvent ev)
		{
			Log($"Client event {ev.eventType} - \"{ev.message}\"; Client status: {ev.status}", "^^");
		}

		// Event handler for showing listing results (eg. local vars list)
		static void ListResultsHandler(ListResult lr)
		{
			Log($"Got {lr.list.Count} results for list type {lr.listType}. (Uncomment next line in ListResultsHandler() to print them.)");
			//Log(lr.ToString());  // To print all the items just use the ToString() override.
			// signal completion
			dataUpdateEvent.Set();
		}

		// Event handler to process data value subscription updates.
		static void DataSubscriptionHandler(DataRequestRecord dr)
		{
			Console.Write($"[{DateTime.Now.ToString("mm:ss.fff")}] << Got Data for request {(Requests)dr.requestId} \"{dr.nameOrCode}\" with Value: ");
			// Convert the received data into a value using DataRequestRecord's tryConvert() methods.
			// This could be more efficient in a "real" application, but it's good enough for our tests with only 2 value types.
			if (dr.tryConvert(out float fVal))
				Console.WriteLine($"(float) {fVal}");
			else if (dr.tryConvert(out string sVal)) {
				Console.WriteLine($"(string) \"{sVal}\"");
			}
			else
				Log("Could not convert result data to value!", "!!");
			// signal completion
			dataUpdateEvent.Set();
		}

		static void Log(string msg, string prfx = "=:")
		{
			Console.WriteLine("[{0}] {1} {2}", DateTime.Now.ToString("mm:ss.fff"), prfx, msg);
		}

	}

}
