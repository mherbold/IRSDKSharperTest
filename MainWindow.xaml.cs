
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

using HerboldRacing;

namespace IRSDKSharperTest
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void OnException( Exception exception )
		{
			throw exception;
		}

		private void OnDisconnected()
		{
			Dispatcher.BeginInvoke( () =>
			{
				dataView.InvalidateVisual();

				scrollBar.Maximum = 1;
			} );
		}

		private void OnTelemetryData()
		{
			Dispatcher.BeginInvoke( () =>
			{
				dataView.InvalidateVisual();

				scrollBar.Maximum = dataView.NumLines - 1;
			} );
		}

		private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
		{
			Program.IRSDKSharper?.Stop();
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

		private void Create_Click( object sender, RoutedEventArgs e )
		{
			Program.IRSDKSharper = new IRSDKSharper();

			Program.IRSDKSharper.OnException += OnException;
			Program.IRSDKSharper.OnDisconnected += OnDisconnected;
			Program.IRSDKSharper.OnTelemetryData += OnTelemetryData;

			// Program.IRSDKSharper.EnableImprovedReplay( null );
		}

		private void Start_Click( object sender, RoutedEventArgs e )
		{
			Program.IRSDKSharper?.Start();
		}

		private void Stop_Click( object sender, RoutedEventArgs e )
		{
			Program.IRSDKSharper?.Stop();
		}

		private void Dispose_Click( object sender, RoutedEventArgs e )
		{
			Program.IRSDKSharper = null;
		}
	}
}
