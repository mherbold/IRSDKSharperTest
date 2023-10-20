
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using HerboldRacing;

namespace IRSDKSharperTest
{
	internal class DataRecorder
	{
		private const string SessionInfoFilePath = "C:\\Users\\marvi\\Desktop\\SessionInfo.txt";
		private const string TelemetryDataFilePath = "C:\\Users\\marvi\\Desktop\\TelemetryData.txt";

		private const int BufferSize = 1 * 1024 * 1024;

		public event Action<Exception>? OnException = null;

		private bool isStarted = false;
		private bool stopNow = false;

		private bool sessionInfoLoopRunning = false;
		private bool telemetryDataLoopRunning = false;

		private AutoResetEvent? sessionInfoAutoResetEvent = null;
		private AutoResetEvent? telemetryDataAutoResetEvent = null;

		private StreamWriter? sessionInfoStreamWriter = null;
		private StreamWriter? telemetryDataStreamWriter = null;

		private IRacingSdkData? iRacingSdkData = null;

		private readonly List<RetainedTelemetryDatum> retainedTelemetryData = new();

		private readonly IRacingSdkSessionInfo retainedSessionInfo = new();

		private static readonly Dictionary<string, int> throttledTelemetry = new()
		{
			{ "AirDensity", 15 },
			{ "AirPressure", 15 },
			{ "AirTemp", 15 },
			{ "CarIdxRPM", 1 },
			{ "FogLevel", 15 },
			{ "FuelLevel", 1 },
			{ "FuelLevelPct", 1 },
			{ "FuelPress", 1 },
			{ "FuelUsePerHour", 1 },
			{ "ManifoldPress", 1 },
			{ "OilPress", 1 },
			{ "OilTemp", 15 },
			{ "PitOptRepairLeft", 1 },
			{ "PitRepairLeft", 1 },
			{ "RelativeHumidity", 15 },
			{ "SolarAltitude", 15 },
			{ "SolarAzimuth", 15 },
			{ "TrackTempCrew", 1 },
			{ "Voltage", 1 },
			{ "WaterTemp", 15 },
			{ "WindDir", 15 },
			{ "WindVel", 15 },
		};

		// Stuff not replayed so we need them in our telemetry recording -
		//
		// CarIdxPaceFlags
		// CarIdxPaceLine
		// CarIdxPaceRow
		// CarIdxSessionFlags
		// CarLeftRight
		// FastRepairAvailable
		// FastRepairUsed
		// FrontTireSetsAvailable
		// FrontTireSetsUsed
		// LeftTireSetsAvailable
		// LeftTireSetsUsed
		// LFTiresAvailable
		// LFTiresUsed
		// LRTiresAvailable
		// LRTiresUsed
		// PaceMode
		// PitsOpen
		// PitstopActive
		// PlayerCarMyIncidentCount
		// PlayerCarTeamIncidentCount
		// RadioTransmitFrequencyIdx
		// RadioTransmitRadioIdx
		// RearTireSetsAvailable
		// RearTireSetsUsed
		// RFTiresAvailable
		// RFTiresUsed
		// RightTireSetsAvailable
		// RightTireSetsUsed
		// RRTiresAvailable
		// RRTiresUsed
		// SessionFlags
		// TireSetsAvailable
		// TireSetsUsed
		//
		// Stuff that I am not sure about so leaving them in our telemetry recording -
		//
		// CarIdxFastRepairsUsed
		// DCDriversSoFar
		// DCLapStatus
		// DriverMarker
		// ManualBoost
		// ManualNoBoost
		// PlayerCarDryTireSetLimit
		// PlayerCarInPitStall
		// PlayerCarPitSvStatus
		// PlayerCarPowerAdjust
		// PlayerCarTowTime
		// PlayerCarWeightPenalty
		// PushToPass
		// SessionJokerLapsRemain
		// SessionOnJokerLap
		// Skies
		// WeatherType

		private static readonly HashSet<string> ignoredTelemetry = new() {
			"Brake",							// replayed
			"BrakeRaw",							// not replayed but chatty
			"CamCameraNumber",					// live
			"CamCameraState",					// live
			"CamCarIdx",						// live
			"CamGroupNumber",					// live
			"CarIdxBestLapNum",					// replayed
			"CarIdxBestLapTime",				// replayed
			"CarIdxClass",						// replayed
			"CarIdxClassPosition",				// replayed
			"CarIdxEstTime",					// replayed
			"CarIdxF2Time",						// replayed
			"CarIdxGear",						// replayed
			"CarIdxLap",						// replayed
			"CarIdxLapCompleted",				// replayed
			"CarIdxLapDistPct",					// replayed
			"CarIdxLastLapTime",				// replayed
			"CarIdxOnPitRoad",					// replayed
			"CarIdxP2P_Count",
			"CarIdxPosition",					// replayed
			"CarIdxQualTireCompound",			// not replayed but dont need this
			"CarIdxQualTireCompoundLocked",		// not replayed but dont need this
			"CarIdxRPM",						// replayed only for player car but is chatty
			"CarIdxSteer",						// replayed
			"CarIdxTireCompound",				// not replayed but dont need this
			"CarIdxTrackSurface",				// replayed
			"CarIdxTrackSurfaceMaterial",		// replayed
			"ChanAvgLatency",					// not replayed but dont need this
			"ChanClockSkew",					// not replayed but dont need this
			"ChanLatency",						// not replayed but dont need this
			"ChanPartnerQuality",				// not replayed but dont need this
			"ChanQuality",						// not replayed but dont need this
			"Clutch",							// replayed
			"ClutchRaw",						// not replayed but chatty
			"CpuUsageBG",						// live
			"CpuUsageBG",						// live
			"CpuUsageFG",						// live
			"DisplayUnits",						// live
			"Engine0_RPM",						// is not replayed but whats the difference with RPM?
			"EnterExitReset",					// live
			"FrameRate",						// live
			"Gear",								// duplicate of CarIdxGear
			"GpuUsage",							// live
			"IsDiskLoggingActive",				// live
			"IsDiskLoggingEnabled",				// live
			"IsInGarage",						// live
			"IsOnTrack",						// live
			"IsOnTrackCar",						// live
			"IsReplayPlaying",					// live
			"Lap",								// duplicate of CarIdxLap
			"LapBestLap",						// not replayed but dont need this
			"LapBestLapTime",					// not replayed but dont need this
			"LapBestNLapLap",					// not replayed but dont need this
			"LapBestNLapTime",					// not replayed but dont need this
			"LapCompleted",						// duplicate of CarIdxLapCompleted
			"LapCurrentLapTime",				// not replayed but dont need this
			"LapDeltaToBestLap",				// not replayed but dont need this
			"LapDeltaToBestLap_DD",				// not replayed but dont need this
			"LapDeltaToBestLap_OK",				// not replayed but dont need this
			"LapDeltaToOptimalLap",				// not replayed but dont need this
			"LapDeltaToOptimalLap_DD",			// not replayed but dont need this
			"LapDeltaToOptimalLap_OK",			// not replayed but dont need this
			"LapDeltaToSessionBestLap",			// not replayed but dont need this
			"LapDeltaToSessionBestLap_DD",		// not replayed but dont need this
			"LapDeltaToSessionBestLap_OK",		// not replayed but dont need this
			"LapDeltaToSessionLastlLap",		// not replayed but dont need this
			"LapDeltaToSessionLastlLap_DD",		// not replayed but dont need this
			"LapDeltaToSessionLastlLap_OK",		// not replayed but dont need this
			"LapDeltaToSessionOptimalLap",		// not replayed but dont need this
			"LapDeltaToSessionOptimalLap_DD",	// not replayed but dont need this
			"LapDeltaToSessionOptimalLap_OK",	// not replayed but dont need this
			"LapDist",							// replayed
			"LapDistPct",						// duplicate of CarIdxLapDistPct
			"LapLasNLapSeq",					// not replayed but dont need this
			"LapLastLapTime",					// duplicate of CarIdxLastLapTime
			"LapLastNLapTime",					// not replayed but dont need this
			"LatAccel",
			"LatAccel_ST",
			"LFshockDefl",
			"LFshockDefl_ST",					// note - only 60 hz during replays
			"LFshockVel",
			"LFshockVel_ST",					// note - only 60 hz during replays
			"LFSHshockDefl",
			"LFSHshockDefl_ST",
			"LFSHshockVel",
			"LFSHshockVel_ST",
			"LoadNumTextures",					// live
			"LongAccel",
			"LongAccel_ST",
			"LRshockDefl",
			"LRshockDefl_ST",					// note - only 60 hz during replays
			"LRshockVel",
			"LRshockVel_ST",					// note - only 60 hz during replays
			"LRSHshockDefl",
			"LRSHshockDefl_ST",
			"LRSHshockVel",
			"LRSHshockVel_ST",
			"MemPageFaultSec",					// live
			"MemSoftPageFaultSec",				// live
			"OkToReloadTextures",				// live
			"OnPitRoad",						// duplicate of CarIdxOnPitRoad
			"Pitch",							// replayed
			"PitchRate",
			"PitchRate_ST",
			"PlayerCarClass",					// duplicate of CarIdxClass
			"PlayerCarClassPosition",			// duplicate of CarIdxClassPosition
			"PlayerCarIdx",						// replayed
			"PlayerCarPosition",				// duplicate of CarIdxPosition
			"PlayerFastRepairsUsed",			// duplicate of CarIdxFastRepairsUsed
			"PlayerTireCompound",				// duplicate of CarIdxTireCompound
			"PlayerTrackSurface",				// duplicate of CarIdxTrackSurface
			"PlayerTrackSurfaceMaterial",		// duplicate of CarIdxTrackSurfaceMaterial
			"PushToTalk",						// live
			"RaceLaps",							// replayed
			"RadioTransmitCarIdx",				// replayed
			"ReplayFrameNum",					// live
			"ReplayFrameNumEnd",				// live
			"ReplayPlaySpeed",
			"ReplaySessionNum",
			"ReplaySessionTime",
			"RFshockDefl",
			"RFshockDefl_ST",					// note - only 60 hz during replays
			"RFshockVel",
			"RFshockVel_ST",					// note - only 60 hz during replays
			"RFSHshockDefl",
			"RFSHshockDefl_ST",
			"RFSHshockVel",
			"RFSHshockVel_ST",
			"Roll",								// replayed
			"RollRate",
			"RollRate_ST",
			"RPM",								// duplicate of CarIdxRPM
			"RRshockDefl",
			"RRshockDefl_ST",					// note - only 60 hz during replays
			"RRshockVel",
			"RRshockVel_ST",					// note - only 60 hz during replays
			"RRSHshockDefl",
			"RRSHshockDefl_ST",
			"RRSHshockVel",
			"RRSHshockVel_ST",
			"SessionLapsRemain",				// superseded by SessionLapsRemainEx
			"SessionLapsRemainEx",				// replayed
			"SessionLapsTotal",					// replayed
			"SessionNum",						// replayed
			"SessionState",						// replayed
			"SessionTick",						// replayed and meaningless so don't use this
			"SessionTime",						// replayed but note that live = accurate and replay = junk so don't use this
			"SessionTimeOfDay",					// not replayed but this can be calculated from weekend information
			"SessionTimeRemain",				// replayed
			"SessionTimeTotal",					// replayed
			"SessionUniqueID",					// replayed
			"ShiftIndicatorPct",
			"Speed",							// replayed
			"SteeringWheelAngle",				// duplicate of CarIdxSteer
			"SteeringWheelAngleMax",			// live
			"SteeringWheelPctTorque",
			"SteeringWheelPctTorqueSign",
			"SteeringWheelPctTorqueSignStops",	//
			"SteeringWheelTorque",
			"SteeringWheelTorque_ST",
			"Throttle",							// replayed
			"ThrottleRaw",						// not replayed but chatty
			"TrackTemp",						// depreciated, use TrackTempCrew instead
			"VelocityX",
			"VelocityX_ST",
			"VelocityY",
			"VelocityY_ST",
			"VelocityZ",
			"VelocityZ_ST",
			"VertAccel",
			"VertAccel_ST",
			"VidCapActive",						// live
			"VidCapEnabled",					// live
			"Yaw",								// replayed
			"YawNorth",							// replayed
			"YawRate",
			"YawRate_ST",

			// left off at ShiftPowerPct
		};

		public void Start()
		{
			Debug.WriteLine( "Recorder starting..." );

			if ( isStarted )
			{
				throw new Exception( "Recorder has already been started." );
			}

			Task.Run( SessionInfoLoop );
			Task.Run( TelemetryDataLoop );

			isStarted = true;

			Debug.WriteLine( "Recorder started." );
		}

		public void Stop()
		{
			Debug.WriteLine( "Recorder stopping..." );

			if ( !isStarted )
			{
				throw new Exception( "Recorder has not been started." );
			}

			Debug.WriteLine( "Setting stopNow = true." );

			stopNow = true;

			if ( sessionInfoLoopRunning )
			{
				Debug.WriteLine( "Waiting for session info loop to stop..." );

				sessionInfoAutoResetEvent?.Set();

				while ( sessionInfoLoopRunning )
				{
					Thread.Sleep( 0 );
				}
			}

			if ( telemetryDataLoopRunning )
			{
				Debug.WriteLine( "Waiting for telemetry data loop to stop..." );

				telemetryDataAutoResetEvent?.Set();

				while ( telemetryDataLoopRunning )
				{
					Thread.Sleep( 0 );
				}
			}

			sessionInfoAutoResetEvent = null;
			telemetryDataAutoResetEvent = null;

			isStarted = false;

			Debug.WriteLine( "Recorder stopped." );
		}

		public void SetIRacingSdkData( IRacingSdkData? iRacingSdkData )
		{
			this.iRacingSdkData = iRacingSdkData;
		}

		public void OnSessionInfo()
		{
			sessionInfoAutoResetEvent?.Set();
		}

		public void OnTelemetryData()
		{
			telemetryDataAutoResetEvent?.Set();
		}

		private void SessionInfoLoop()
		{
			Debug.WriteLine( "Session info loop started." );

			try
			{
				sessionInfoStreamWriter = new StreamWriter( SessionInfoFilePath, false, Encoding.UTF8, BufferSize );

				sessionInfoAutoResetEvent = new AutoResetEvent( false );

				sessionInfoLoopRunning = true;

				while ( !stopNow )
				{
					Debug.WriteLine( "Waiting for session info event." );

					sessionInfoAutoResetEvent?.WaitOne();

					if ( stopNow )
					{
						break;
					}

					RecordSessionInfo();
				}
			}
			catch ( Exception exception )
			{
				Debug.WriteLine( "Session info loop exception caught." );

				OnException?.Invoke( exception );
			}
			finally
			{
				sessionInfoStreamWriter?.Close();

				sessionInfoStreamWriter = null;

				sessionInfoLoopRunning = false;
			}

			Debug.WriteLine( "Session info loop stopped." );
		}

		private void TelemetryDataLoop()
		{
			Debug.WriteLine( "Telemetry data loop started." );

			try
			{
				telemetryDataStreamWriter = new StreamWriter( TelemetryDataFilePath, false, Encoding.UTF8, BufferSize );

				telemetryDataAutoResetEvent = new AutoResetEvent( false );

				telemetryDataLoopRunning = true;

				while ( !stopNow )
				{
					Debug.WriteLine( "Waiting for telemetry data event." );

					telemetryDataAutoResetEvent?.WaitOne();

					if ( stopNow )
					{
						break;
					}

					RecordTelemetryData();
				}
			}
			catch ( Exception exception )
			{
				Debug.WriteLine( "Telemetry data loop exception caught." );

				OnException?.Invoke( exception );
			}
			finally
			{
				telemetryDataStreamWriter?.Close();

				telemetryDataStreamWriter = null;

				telemetryDataLoopRunning = false;
			}

			Debug.WriteLine( "Telemetry data loop stopped." );
		}

		private void RecordSessionInfo()
		{
			Debug.WriteLine( "Recording session info..." );

			if ( ( iRacingSdkData != null ) && ( iRacingSdkData.SessionInfo != null ) && ( sessionInfoStreamWriter != null ) )
			{
				var stringBuilder = new StringBuilder( BufferSize );

				foreach ( var propertyInfo in iRacingSdkData.SessionInfo.GetType().GetProperties() )
				{
					var updatedObject = propertyInfo.GetValue( iRacingSdkData.SessionInfo );

					if ( updatedObject != null )
					{
						var retainedObject = propertyInfo.GetValue( retainedSessionInfo );

						if ( retainedObject == null )
						{
							var type = updatedObject.GetType();

							retainedObject = Activator.CreateInstance( type ) ?? throw new Exception( $"Could not create new insteance of type {type}!" );

							propertyInfo.SetValue( retainedSessionInfo, retainedObject );
						}

						RecordSessionInfo( propertyInfo.Name, retainedObject, updatedObject, stringBuilder );
					}
				}

				if ( stringBuilder.Length > 0 )
				{
					var sessionNum = iRacingSdkData.GetInt( "SessionNum" );
					var sessionTime = iRacingSdkData.GetDouble( "SessionTime" );

					sessionInfoStreamWriter.WriteLine( "" );
					sessionInfoStreamWriter.WriteLine( $"SessionTime = {sessionNum}:{sessionTime:0.0000}" );
					sessionInfoStreamWriter.Write( stringBuilder );
				}
			}
		}

		private void RecordSessionInfo( string propertyName, object retainedObject, object updatedObject, StringBuilder stringBuilder )
		{
			foreach ( var propertyInfo in updatedObject.GetType().GetProperties() )
			{
				var updatedValue = propertyInfo.GetValue( updatedObject );
				var retainedValue = propertyInfo.GetValue( retainedObject );

				var isSimpleValue = ( ( updatedValue is null ) || ( updatedValue is string ) || ( updatedValue is int ) || ( updatedValue is float ) || ( updatedValue is double ) );

				if ( isSimpleValue )
				{
					if ( ( ( updatedValue is null ) && ( retainedValue is not null ) ) || ( ( updatedValue is not null ) && !updatedValue.Equals( retainedValue ) ) )
					{
						propertyInfo.SetValue( retainedObject, updatedValue );

						stringBuilder.AppendLine( $"{propertyName}.{propertyInfo.Name} = {updatedValue}" );
					}
				}
				else
				{
					if ( updatedValue is IList updatedList )
					{
						var elementType = propertyInfo.PropertyType.GenericTypeArguments[ 0 ] ?? throw new Exception( "List element type could not be determined!" );

						if ( retainedValue is not IList retainedList )
						{
							var constructedListType = typeof( List<> ).MakeGenericType( elementType );

							retainedList = Activator.CreateInstance( constructedListType ) as IList ?? throw new Exception( "Failed to create new list!" );

							propertyInfo.SetValue( retainedObject, retainedList );
						}

						var index = 0;

						foreach ( var updatedItem in updatedList )
						{
							var retainedItem = ( index < retainedList.Count ) ? retainedList[ index ] : null;

							if ( retainedItem == null )
							{
								retainedItem = Activator.CreateInstance( elementType ) ?? throw new Exception( "Failed to create list item!" );

								retainedList.Add( retainedItem );
							}

							RecordSessionInfo( $"{propertyName}.{propertyInfo.Name}[{index}]", retainedItem, updatedItem, stringBuilder );

							index++;
						}
					}
					else
					{
						if ( retainedValue == null )
						{
							retainedValue = Activator.CreateInstance( propertyInfo.PropertyType ) ?? throw new Exception( "Failed to create object!" );

							propertyInfo.SetValue( retainedObject, retainedValue );
						}

						RecordSessionInfo( $"{propertyName}.{propertyInfo.Name}", retainedValue, updatedValue, stringBuilder );
					}
				}
			}
		}

		private void RecordTelemetryData()
		{
			if ( ( iRacingSdkData != null ) && ( iRacingSdkData.TelemetryDataProperties != null ) && ( telemetryDataStreamWriter != null ) )
			{
				if ( retainedTelemetryData.Count == 0 )
				{
					foreach ( var keyValuePair in iRacingSdkData.TelemetryDataProperties )
					{
						retainedTelemetryData.Add( new RetainedTelemetryDatum( keyValuePair.Value ) );
					}
				}

				var stringBuilder = new StringBuilder( BufferSize );

				foreach ( var retainedTelemetryDatum in retainedTelemetryData )
				{
					if ( !retainedTelemetryDatum.ignored )
					{
						if ( ( retainedTelemetryDatum.lastUpdatedTickCount + retainedTelemetryDatum.updateFrequencyInSeconds * iRacingSdkData.TickRate ) <= iRacingSdkData.TickCount )
						{
							for ( var valueIndex = 0; valueIndex < retainedTelemetryDatum.iRacingSdkDatum.Count; valueIndex++ )
							{
								object updatedValue = iRacingSdkData.GetValue( retainedTelemetryDatum.iRacingSdkDatum.Name, valueIndex );
								object retainedValue = retainedTelemetryDatum.retainedValue[ valueIndex ];

								if ( !updatedValue.Equals( retainedValue ) )
								{
									retainedTelemetryDatum.retainedValue[ valueIndex ] = updatedValue;
									retainedTelemetryDatum.lastUpdatedTickCount = iRacingSdkData.TickCount;

									stringBuilder.AppendLine( $"{retainedTelemetryDatum.iRacingSdkDatum.Name}[{valueIndex}] = {updatedValue}" );
								}
							}
						}
					}
				}

				if ( stringBuilder.Length > 0 )
				{
					var sessionNum = iRacingSdkData.GetInt( "SessionNum" );
					var sessionTime = iRacingSdkData.GetDouble( "SessionTime" );

					telemetryDataStreamWriter.WriteLine( "" );
					telemetryDataStreamWriter.WriteLine( $"SessionTime = {sessionNum}:{sessionTime:0.0000}" );
					telemetryDataStreamWriter.Write( stringBuilder );
				}
			}
		}

		internal class RetainedTelemetryDatum
		{
			public IRacingSdkDatum iRacingSdkDatum;
			public object[] retainedValue;
			public int lastUpdatedTickCount;
			public int updateFrequencyInSeconds;
			public bool ignored;

			public RetainedTelemetryDatum( IRacingSdkDatum iRacingSdkDatum )
			{
				this.iRacingSdkDatum = iRacingSdkDatum;

				Type type = typeof( char );

				switch ( iRacingSdkDatum.VarType )
				{
					case IRacingSdkEnum.VarType.Char: type = typeof( char ); break;
					case IRacingSdkEnum.VarType.Bool: type = typeof( bool ); break;
					case IRacingSdkEnum.VarType.Int: type = typeof( int ); break;
					case IRacingSdkEnum.VarType.BitField: type = typeof( uint ); break;
					case IRacingSdkEnum.VarType.Float: type = typeof( float ); break;
					case IRacingSdkEnum.VarType.Double: type = typeof( double ); break;
				}

				retainedValue = new object[ iRacingSdkDatum.Count ];

				for ( var index = 0; index < iRacingSdkDatum.Count; index++ )
				{
					retainedValue[ index ] = Activator.CreateInstance( type ) ?? throw new Exception( $"Could not create instance of {type}!" );
				}

				lastUpdatedTickCount = int.MinValue;

				updateFrequencyInSeconds = throttledTelemetry.GetValueOrDefault( iRacingSdkDatum.Name, 0 );

				ignored = ignoredTelemetry.Contains( iRacingSdkDatum.Name );
			}
		}
	}
}
