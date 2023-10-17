
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

using HerboldRacing;

namespace IRSDKSharperTest
{
	public partial class MainWindow : Window
	{
		private readonly DataRecorder dataRecorder;
		private readonly IRSDKSharper irsdkSharper;

		public MainWindow()
		{
			InitializeComponent();

			dataRecorder = new DataRecorder();

			dataRecorder.OnException += OnException;

			dataRecorder.Start();

			irsdkSharper = new IRSDKSharper();

			irsdkSharper.OnException += OnException;
			irsdkSharper.OnConnected += OnConnected;
			irsdkSharper.OnDisconnected += OnDisconnected;
			irsdkSharper.OnSessionInfo += OnSessionInfo;
			irsdkSharper.OnTelemetryData += OnTelemetryData;

			irsdkSharper.Start();
		}

		private void OnException( Exception exception )
		{
			throw exception;
		}

		private void OnConnected()
		{
			dataRecorder.SetIRacingSdkData( irsdkSharper.Data );
			dataView.SetIRacingSdkData( irsdkSharper.Data );
		}

		private void OnDisconnected()
		{
			dataRecorder.SetIRacingSdkData( null );
			dataView.SetIRacingSdkData( null );

			Dispatcher.BeginInvoke( () =>
			{
				dataView.InvalidateVisual();

				scrollBar.Maximum = 1;
			} );
		}

		private void OnSessionInfo()
		{
			dataRecorder.OnSessionInfo();
		}

		private void OnTelemetryData()
		{
			dataRecorder.OnTelemetryData();

			Dispatcher.BeginInvoke( () =>
			{
				dataView.InvalidateVisual();

				scrollBar.Maximum = dataView.NumLines - 1;
			} );
		}

		private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
		{
			irsdkSharper.Stop();
			dataRecorder.Stop();
		}

		private void Window_MouseWheel( object sender, System.Windows.Input.MouseWheelEventArgs e )
		{
			var delta = e.Delta / 30.0f;

			if ( delta > 0 )
			{
				delta = Math.Max( 1, delta );
			}
			else
			{
				delta = Math.Min( -1, delta );
			}

			scrollBar.Value -= delta;

			dataView.SetScrollIndex( (int) scrollBar.Value );
		}

		private void ScrollBar_Scroll( object sender, ScrollEventArgs e )
		{
			dataView.SetScrollIndex( (int) e.NewValue );
		}

		private void HeaderDataButton_Click( object sender, RoutedEventArgs e )
		{
			dataView.SetMode( 0 );
		}

		private void TelemetryDataButton_Click( object sender, RoutedEventArgs e )
		{
			dataView.SetMode( 1 );
		}

		private void SessionInfoButton_Click( object sender, RoutedEventArgs e )
		{
			dataView.SetMode( 2 );
		}
	}
}
