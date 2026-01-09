using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading.Tasks;
using SelfDrivingCar.Application.UI;
using SelfDrivingCar.Car;
using SelfDrivingCar.World;

namespace SelfDrivingCar.Application.UI;

public partial class MainWindow : Window
{
	private MapCanvas? _mapCanvas;
	private TextBlock? _nodeCountText;
	private TextBlock? _roadCountText;
	private TextBlock? _statusText;
	private Button? _startDrivingButton;
	private CarDriver? _carDriver;
	private WorldNavigate? _worldNavigate;
	private bool _isCurrentlyDriving;
	private Node? _currentStartNode;
	private Node? _currentEndNode;

	public MainWindow()
	{
		InitializeComponent();

		// Find controls after initialization
		_mapCanvas = this.FindControl<MapCanvas>("MapCanvas");
		_nodeCountText = this.FindControl<TextBlock>("NodeCountText");
		_roadCountText = this.FindControl<TextBlock>("RoadCountText");
		_statusText = this.FindControl<TextBlock>("StatusText");
		_startDrivingButton = this.FindControl<Button>("StartDrivingButton");

		// Subscribe to selection changes
		if (_mapCanvas != null)
		{
			_mapCanvas.SelectionChanged += OnNodeSelectionChanged;
		}

		_isCurrentlyDriving = false;
	}

	public void SetMap(Map map)
	{
		_mapCanvas?.SetMap(map);
		_worldNavigate = new WorldNavigate(map);
		UpdateStatistics(map);
	}

	public void SetCarDriver(CarDriver carDriver)
	{
		_carDriver = carDriver;
	}

	private void UpdateStatistics(Map map)
	{
		if (_nodeCountText != null)
		{
			_nodeCountText.Text = map.Nodes.Count.ToString();
		}

		int roadCount = 0;
		foreach (var node in map.Nodes)
		{
			roadCount += map.GetConnections(node).Count;
		}

		if (_roadCountText != null)
		{
			_roadCountText.Text = roadCount.ToString();
		}
	}

	private void OnNodeSelectionChanged(object? sender, NodeSelectionChangedEventArgs e)
	{
		// Track the selected nodes
		_currentStartNode = e.StartNode;
		_currentEndNode = e.EndNode;

		// Update button enable state
		bool hasRoute = e.StartNode != null && e.EndNode != null && !_isCurrentlyDriving;
		if (_startDrivingButton != null)
		{
			_startDrivingButton.IsEnabled = hasRoute;
		}

		// Update status display
		if (_statusText != null)
		{
			if (e.StartNode != null && e.EndNode != null)
			{
				_statusText.Text = $"Start: {e.StartNode.Name} → End: {e.EndNode.Name} | Press 'Start Driving' to begin";

				// Calculate ground truth route from World
				if (_worldNavigate != null)
				{
					var worldRoute = _worldNavigate.FindRoute(e.StartNode, e.EndNode);
					if (worldRoute != null && worldRoute.Count > 0)
					{
						// Display the route on the map
						_mapCanvas?.SetPlannedRoute(worldRoute);

						// Internally calculate MotMot route for driving
						if (_carDriver != null)
						{
							var motMotRoute = _carDriver.CalculateRoute(e.StartNode, e.EndNode);
							if (motMotRoute != null && motMotRoute.Count > 0)
							{
								_carDriver.UpdateRoute(motMotRoute);
							}
						}
					}
					else
					{
						_statusText.Text = "No route found between selected nodes";
						_mapCanvas?.SetPlannedRoute(null);
						if (_startDrivingButton != null) _startDrivingButton.IsEnabled = false;
					}
				}
			}
			else if (e.StartNode != null)
			{
				_statusText.Text = $"Start: {e.StartNode.Name} | Click right to select end";
				_mapCanvas?.SetPlannedRoute(null);
			}
			else if (e.EndNode != null)
			{
				_statusText.Text = $"End: {e.EndNode.Name} | Click left to select start";
				_mapCanvas?.SetPlannedRoute(null);
			}
			else
			{
				_statusText.Text = "Select start (left-click) and end (right-click) nodes";
				_mapCanvas?.SetPlannedRoute(null);
			}
		}
	}

	private void OnStartDrivingClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
	{
		if (_carDriver == null || _mapCanvas == null || _currentStartNode == null || _currentEndNode == null)
		{
			return;
		}

		_isCurrentlyDriving = true;
		if (_startDrivingButton != null)
		{
			_startDrivingButton.IsEnabled = false;
		}

		// Show car on map
		_mapCanvas.ShowCar(true);
		if (_statusText != null)
		{
			_statusText.Text = "Driving...";
		}

		Console.WriteLine("=== Starting Driving ===");
		Console.WriteLine($"Start Node: {_currentStartNode.Name}");
		Console.WriteLine($"End Node: {_currentEndNode.Name}");

		// Create a timer to update car position on the UI thread
		var timer = new DispatcherTimer();
		timer.Interval = TimeSpan.FromMilliseconds(50); // 20 FPS
		int updateCount = 0;
		var startTime = DateTime.Now;
		timer.Tick += (s, args) =>
		{
			if (_carDriver != null && _mapCanvas != null)
			{
				// Update car position from CarDriver
				_mapCanvas.UpdateCarPosition(_carDriver.CurrentPosition, _carDriver.CurrentBearing);
				_mapCanvas.InvalidateVisual();
				updateCount++;

				if (updateCount % 20 == 0) // Log every second (20 updates * 50ms = 1000ms)
				{
					var elapsed = DateTime.Now - startTime;
					Console.WriteLine($"[{elapsed.TotalSeconds:F1}s] Pos=({_carDriver.CurrentPosition.Latitude:F6},{_carDriver.CurrentPosition.Longitude:F6}) Bearing={_carDriver.CurrentBearing:F1}° Speed={_carDriver.CurrentSpeed:F1}km/h Road#{_carDriver.CurrentRoadIndex}");
				}

				// Stop when driving is complete
				if (!_carDriver.IsActive)
				{
					var totalTime = DateTime.Now - startTime;
					timer.Stop();
					_isCurrentlyDriving = false;
					_mapCanvas.ShowCar(false);
					if (_statusText != null)
					{
						_statusText.Text = "Drive completed! Select new start/end nodes to plan another route";
					}
					Console.WriteLine($"=== Driving Completed === (Duration: {totalTime.TotalSeconds:F1}s, Total updates: {updateCount})");
				}
			}
		};

		timer.Start();

		// Run StartDriving on a background thread
		Task.Run(() =>
		{
			try
			{
				Console.WriteLine("Calling CarDriver.StartDriving() on background thread");
				_carDriver.StartDriving(_currentStartNode, _currentEndNode);
				Console.WriteLine("StartDriving completed");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during driving: {ex}");
				Dispatcher.UIThread.Post(() =>
				{
					timer.Stop();
					_isCurrentlyDriving = false;
					_mapCanvas.ShowCar(false);
					if (_statusText != null)
					{
						_statusText.Text = $"Error: {ex.Message}";
					}
				});
			}
		});
	}
}
