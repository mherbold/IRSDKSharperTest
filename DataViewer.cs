
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using HerboldRacing;

namespace IRSDKSharperTest
{
	public class DataViewer : Control
	{
		public int NumLines { get; private set; } = 0;
		public double RenderTime { get; private set; } = 0;

		private int scrollIndex = 0;
		private int mode = 0;

		private readonly CultureInfo cultureInfo = CultureInfo.GetCultureInfo( "en-us" );
		private readonly Typeface typeface = new( "Courier New" );

		static DataViewer()
		{
			DefaultStyleKeyProperty.OverrideMetadata( typeof( DataViewer ), new FrameworkPropertyMetadata( typeof( DataViewer ) ) );
		}

		public void SetScrollIndex( int scrollIndex )
		{
			this.scrollIndex = scrollIndex;
		}

		public void SetMode( int mode )
		{
			this.mode = mode;
		}

		protected override void OnRender( DrawingContext drawingContext )
		{
			var stopWatch = Stopwatch.StartNew();

			base.OnRender( drawingContext );

			switch ( mode )
			{
				case 0: DrawHeaderData( drawingContext ); break;
				case 1: DrawSessionInfo( drawingContext ); break;
				case 2: DrawTelemetryData( drawingContext ); break;
			}

			stopWatch.Stop();

			RenderTime = (double) stopWatch.ElapsedTicks / (double) Stopwatch.Frequency;
		}

		private void DrawHeaderData( DrawingContext drawingContext )
		{
			var irsdk = Program.IRSDKSharper;

			if ( irsdk == null )
			{
				return;
			}

			if ( irsdk.IsConnected )
			{
				var dictionary = new Dictionary<string, int>()
				{
					{ "Version", irsdk.Data.Version },
					{ "Status", irsdk.Data.Status },
					{ "TickRate", irsdk.Data.TickRate },
					{ "SessionInfoUpdate", irsdk.Data.SessionInfoUpdate },
					{ "SessionInfoLength", irsdk.Data.SessionInfoLength },
					{ "SessionInfoOffset", irsdk.Data.SessionInfoOffset },
					{ "VarCount", irsdk.Data.VarCount },
					{ "VarHeaderOffset", irsdk.Data.VarHeaderOffset },
					{ "BufferCount", irsdk.Data.BufferCount },
					{ "BufferLength", irsdk.Data.BufferLength },
					{ "TickCount", irsdk.Data.TickCount },
					{ "Offset", irsdk.Data.Offset },
					{ "FramesDropped", irsdk.Data.FramesDropped }
				};

				var point = new Point( 10, 10 );
				var lineIndex = 0;

				foreach ( var keyValuePair in dictionary )
				{
					if ( ( lineIndex & 1 ) == 1 )
					{
						drawingContext.DrawRectangle( Brushes.AliceBlue, null, new Rect( 0, point.Y, ActualWidth, 20 ) );
					}

					var formattedText = new FormattedText( keyValuePair.Key, cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
					{
						LineHeight = 20
					};

					drawingContext.DrawText( formattedText, point );

					point.X += 150;

					formattedText = new FormattedText( keyValuePair.Value.ToString(), cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
					{
						LineHeight = 20
					};

					drawingContext.DrawText( formattedText, point );

					point.X = 10;
					point.Y += 20;

					lineIndex++;
				}

				NumLines = lineIndex;
			}
			else
			{
				drawingContext.DrawRectangle( Brushes.DarkGray, null, new Rect( 0, 0, ActualWidth, ActualHeight ) );
			}
		}

		private void DrawSessionInfo( DrawingContext drawingContext )
		{
			var irsdk = Program.IRSDKSharper;

			if ( irsdk == null )
			{
				return;
			}

			var sessionInfo = irsdk.Data.SessionInfo;

			if ( irsdk.IsConnected && ( sessionInfo != null ) )
			{
				var point = new Point( 10, 10 );
				var lineIndex = 0;
				var stopDrawing = false;

				foreach ( var propertyInfo in sessionInfo.GetType().GetProperties() )
				{
					DrawSessionInfo( drawingContext, propertyInfo.Name, propertyInfo.GetValue( sessionInfo ), 0, ref point, ref lineIndex, ref stopDrawing );
				}

				NumLines = lineIndex;
			}
			else
			{
				drawingContext.DrawRectangle( Brushes.DarkGray, null, new Rect( 0, 0, ActualWidth, ActualHeight ) );
			}
		}

		private void DrawSessionInfo( DrawingContext drawingContext, string propertyName, object? valueAsObject, int indent, ref Point point, ref int lineIndex, ref bool stopDrawing )
		{
			var isSimpleValue = ( ( valueAsObject is null ) || ( valueAsObject is string ) || ( valueAsObject is int ) || ( valueAsObject is float ) || ( valueAsObject is double ) );

			if ( ( lineIndex >= scrollIndex ) && !stopDrawing )
			{
				if ( ( lineIndex & 1 ) == 1 )
				{
					drawingContext.DrawRectangle( Brushes.AliceBlue, null, new Rect( 0, point.Y, ActualWidth, 20 ) );
				}

				point.X = 10 + indent * 50;

				var formattedText = new FormattedText( propertyName, cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
				{
					LineHeight = 20
				};

				drawingContext.DrawText( formattedText, point );

				if ( valueAsObject is null )
				{
					point.X = 260 + indent * 50;

					formattedText = new FormattedText( "(null)", cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
					{
						LineHeight = 20
					};

					drawingContext.DrawText( formattedText, point );
				}
				else if ( isSimpleValue )
				{
					point.X = 260 + indent * 50;

					formattedText = new FormattedText( valueAsObject.ToString(), cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
					{
						LineHeight = 20
					};

					drawingContext.DrawText( formattedText, point );
				}

				point.Y += 20;

				if ( ( point.Y + 20 ) > ActualHeight )
				{
					stopDrawing = true;
				}
			}

			lineIndex++;

			if ( !isSimpleValue )
			{
				if ( valueAsObject is IList list )
				{
					var index = 0;

					foreach ( var item in list )
					{
						DrawSessionInfo( drawingContext, index.ToString(), item, indent + 1, ref point, ref lineIndex, ref stopDrawing );

						index++;
					}
				}
				else
				{
#pragma warning disable CS8602
					foreach ( var propertyInfo in valueAsObject.GetType().GetProperties() )
					{
						DrawSessionInfo( drawingContext, propertyInfo.Name, propertyInfo.GetValue( valueAsObject ), indent + 1, ref point, ref lineIndex, ref stopDrawing );
					}
#pragma warning restore CS8602
				}
			}
		}

		private void DrawTelemetryData( DrawingContext drawingContext )
		{
			var irsdk = Program.IRSDKSharper;

			if ( irsdk == null )
			{
				return;
			}

			if ( irsdk.IsConnected )
			{
				var point = new Point( 10, 10 );
				var lineIndex = 0;
				var stopDrawing = false;

				foreach ( var keyValuePair in irsdk.Data.TelemetryDataProperties )
				{
					for ( var valueIndex = 0; valueIndex < keyValuePair.Value.Count; valueIndex++ )
					{
						if ( ( lineIndex >= scrollIndex ) && !stopDrawing )
						{
							if ( ( lineIndex & 1 ) == 1 )
							{
								drawingContext.DrawRectangle( Brushes.AliceBlue, null, new Rect( 0, point.Y, ActualWidth, 20 ) );
							}

							var offset = keyValuePair.Value.Offset + valueIndex * IRacingSdkConst.VarTypeBytes[ (int) keyValuePair.Value.VarType ];

							var formattedText = new FormattedText( offset.ToString(), cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
							{
								LineHeight = 20
							};

							drawingContext.DrawText( formattedText, point );

							point.X += 40;

							formattedText = new FormattedText( keyValuePair.Value.Name, cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
							{
								LineHeight = 20
							};

							drawingContext.DrawText( formattedText, point );

							point.X += 230;

							if ( keyValuePair.Value.Count > 1 )
							{
								formattedText = new FormattedText( valueIndex.ToString(), cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
								{
									LineHeight = 20
								};

								drawingContext.DrawText( formattedText, point );
							}

							point.X += 30;

							var valueAsString = string.Empty;
							var bitsAsString = string.Empty;
							var brush = Brushes.Black;

							switch ( keyValuePair.Value.Unit )
							{
								case "irsdk_TrkLoc":
									valueAsString = GetString<IRacingSdkEnum.TrkLoc>( keyValuePair.Value, valueIndex );
									break;

								case "irsdk_TrkSurf":
									valueAsString = GetString<IRacingSdkEnum.TrkSurf>( keyValuePair.Value, valueIndex );
									break;

								case "irsdk_SessionState":
									valueAsString = GetString<IRacingSdkEnum.SessionState>( keyValuePair.Value, valueIndex );
									break;

								case "irsdk_CarLeftRight":
									valueAsString = GetString<IRacingSdkEnum.CarLeftRight>( keyValuePair.Value, valueIndex );
									break;

								case "irsdk_PitSvStatus":
									valueAsString = GetString<IRacingSdkEnum.PitSvStatus>( keyValuePair.Value, valueIndex );
									break;

								case "irsdk_PaceMode":
									valueAsString = GetString<IRacingSdkEnum.PaceMode>( keyValuePair.Value, valueIndex );
									break;

								default:

									switch ( keyValuePair.Value.VarType )
									{
										case IRacingSdkEnum.VarType.Char:
											valueAsString = $"         {irsdk.Data.GetChar( keyValuePair.Value, valueIndex )}";
											break;

										case IRacingSdkEnum.VarType.Bool:
											var valueAsBool = irsdk.Data.GetBool( keyValuePair.Value, valueIndex );
											valueAsString = valueAsBool ? "         T" : "         F";
											brush = valueAsBool ? Brushes.Green : Brushes.Red;
											break;

										case IRacingSdkEnum.VarType.Int:
											valueAsString = $"{irsdk.Data.GetInt( keyValuePair.Value, valueIndex ),10:N0}";
											break;

										case IRacingSdkEnum.VarType.BitField:
											valueAsString = $"0x{irsdk.Data.GetBitField( keyValuePair.Value, valueIndex ):X8}";

											switch ( keyValuePair.Value.Unit )
											{
												case "irsdk_EngineWarnings":
													bitsAsString = GetString<IRacingSdkEnum.EngineWarnings>( keyValuePair.Value, valueIndex );
													break;

												case "irsdk_Flags":
													bitsAsString = GetString<IRacingSdkEnum.Flags>( keyValuePair.Value, valueIndex );
													break;

												case "irsdk_CameraState":
													bitsAsString = GetString<IRacingSdkEnum.CameraState>( keyValuePair.Value, valueIndex );
													break;

												case "irsdk_PitSvFlags":
													bitsAsString = GetString<IRacingSdkEnum.PitSvFlags>( keyValuePair.Value, valueIndex );
													break;

												case "irsdk_PaceFlags":
													bitsAsString = GetString<IRacingSdkEnum.PaceFlags>( keyValuePair.Value, valueIndex );
													break;
											}

											break;

										case IRacingSdkEnum.VarType.Float:
											valueAsString = $"{irsdk.Data.GetFloat( keyValuePair.Value, valueIndex ),15:N4}";
											break;

										case IRacingSdkEnum.VarType.Double:
											valueAsString = $"{irsdk.Data.GetDouble( keyValuePair.Value, valueIndex ),15:N4}";
											break;
									}

									break;
							}

							formattedText = new FormattedText( valueAsString, cultureInfo, FlowDirection.LeftToRight, typeface, 12, brush, 1.25f )
							{
								LineHeight = 20
							};

							drawingContext.DrawText( formattedText, point );

							point.X += 150;

							formattedText = new FormattedText( keyValuePair.Value.Unit, cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
							{
								LineHeight = 20
							};

							drawingContext.DrawText( formattedText, point );

							point.X += 160;

							var desc = keyValuePair.Value.Desc;
							var originalDescLength = desc.Length;

							if ( bitsAsString != string.Empty )
							{
								desc += $" ({bitsAsString})";
							}

							formattedText = new FormattedText( desc, cultureInfo, FlowDirection.LeftToRight, typeface, 12, Brushes.Black, 1.25f )
							{
								LineHeight = 20
							};

							if ( bitsAsString != string.Empty )
							{
								formattedText.SetForegroundBrush( Brushes.OrangeRed, originalDescLength, desc.Length - originalDescLength );
							}

							drawingContext.DrawText( formattedText, point );

							point.X = 10;
							point.Y += 20;

							if ( ( point.Y + 20 ) > ActualHeight )
							{
								stopDrawing = true;
							}
						}

						lineIndex++;
					}
				}

				NumLines = lineIndex;
			}
			else
			{
				drawingContext.DrawRectangle( Brushes.DarkGray, null, new Rect( 0, 0, ActualWidth, ActualHeight ) );
			}
		}

		private static string GetString<T>( IRacingSdkDatum var, int index ) where T : Enum
		{
			var irsdk = Program.IRSDKSharper;

			if ( irsdk == null )
			{
				return "";
			}

			if ( var.VarType == IRacingSdkEnum.VarType.Int )
			{
				var enumValue = (T) (object) irsdk.Data.GetInt( var, index );

				return enumValue.ToString();
			}
			else
			{
				var bits = irsdk.Data.GetBitField( var, index );

				var bitsString = string.Empty;

				foreach ( uint bitMask in Enum.GetValues( typeof( T ) ) )
				{
					if ( ( bits & bitMask ) != 0 )
					{
						if ( bitsString != string.Empty )
						{
							bitsString += " | ";
						}

						bitsString += Enum.GetName( typeof( T ), bitMask );
					}
				}

				return bitsString;
			}
		}
	}
}
