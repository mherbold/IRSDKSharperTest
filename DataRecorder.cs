
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

		private static readonly HashSet<string> ignoredTelemetryNames = new() {
			"Brake",
			"CamCameraNumber",
			"CarIdxEstTime",
			"CarIdxLapDistPct",
			"CarIdxRPM",
			"CarIdxSteer",
			"CarIdxTrackSurfaceMaterial",
			"CpuUsageBG",
			"CpuUsageBG",
			"CpuUsageFG",
			"FrameRate",
			"GpuUsage",
			"LapDist",
			"LapDistPct",
			"LatAccel",
			"LatAccel_ST",
			"LongAccel",
			"LongAccel_ST",
			"MemPageFaultSec",
			"MemSoftPageFaultSec",
			"Pitch",
			"PitchRate",
			"PitchRate_ST",
			"PlayerTrackSurfaceMaterial",
			"ReplayFrameNum",
			"ReplayFrameNumEnd",
			"ReplaySessionTime",
			"Roll",
			"RollRate",
			"RollRate_ST",
			"RPM",
			"SessionTick",
			"SessionTime",
			"SessionTimeRemain",
			"ShiftIndicatorPct",
			"Speed",
			"SteeringWheelAngle",
			"Throttle",
			"VelocityX",
			"VelocityX_ST",
			"VelocityY",
			"VelocityY_ST",
			"VelocityZ",
			"VelocityZ_ST",
			"VertAccel",
			"VertAccel_ST",
			"Yaw",
			"YawNorth",
			"YawRate",
			"YawRate_ST",
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
			if ( ( iRacingSdkData != null ) && ( iRacingSdkData.TelemetryData != null ) && ( telemetryDataStreamWriter != null ) )
			{
				if ( retainedTelemetryData.Count == 0 )
				{
					foreach ( var keyValuePair in iRacingSdkData.TelemetryData )
					{
						retainedTelemetryData.Add( new RetainedTelemetryDatum( keyValuePair.Value ) );
					}
				}

				var stringBuilder = new StringBuilder( BufferSize );

				foreach ( var retainedTelemetryDatum in retainedTelemetryData )
				{
					if ( !retainedTelemetryDatum.ignored )
					{
						var updateCounterIncremeneted = false;

						for ( var valueIndex = 0; valueIndex < retainedTelemetryDatum.iRacingSdkDatum.Count; valueIndex++ )
						{
							object updatedValue = iRacingSdkData.GetValue( retainedTelemetryDatum.iRacingSdkDatum.Name, valueIndex );
							object retainedValue = retainedTelemetryDatum.retainedValue[ valueIndex ];

							if ( !updatedValue.Equals( retainedValue ) )
							{
								if ( !updateCounterIncremeneted )
								{
									updateCounterIncremeneted = true;

									retainedTelemetryDatum.updateCounter++;
								}

								retainedTelemetryDatum.retainedValue[ valueIndex ] = updatedValue;

								stringBuilder.AppendLine( $"{retainedTelemetryDatum.iRacingSdkDatum.Name}[{valueIndex}] = {updatedValue}" );
							}
						}

						if ( updateCounterIncremeneted )
						{
							if ( retainedTelemetryDatum.updateCounter == 10 )
							{
								retainedTelemetryDatum.ignored = true;
							}
						}
						else
						{
							retainedTelemetryDatum.updateCounter = 0;
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
			public int updateCounter;
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

				updateCounter = 0;

				ignored = ignoredTelemetryNames.Contains( iRacingSdkDatum.Name );
			}
		}
	}
}
