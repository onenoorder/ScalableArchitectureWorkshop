using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using SelfDrivingCar.Application.UI;
using SelfDrivingCar.Car;
using SelfDrivingCar.TomTom;
using SelfDrivingCar.World;

namespace SelfDrivingCar.Application;

public sealed class App : Avalonia.Application
{
	private CarDriver? _carController;
	private MotMotNavigate? _motMotNavigate;
	private Map? _currentMap;
	private MainWindow? _mainWindow;

	public override void Initialize()
	{
		_currentMap = MapGenerator.GenerateMap();
		_motMotNavigate = new MotMotNavigate(_currentMap);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			CreateAndSetupWindow(desktop);
		}

		base.OnFrameworkInitializationCompleted();
	}

	private void CreateAndSetupWindow(IClassicDesktopStyleApplicationLifetime desktop)
	{
		_carController = new CarDriver(_motMotNavigate!);

		// Create and show the main window with the map
		_mainWindow = new MainWindow();
		if (_currentMap != null)
		{
			_mainWindow.SetMap(_currentMap);
		}

		// Pass the car driver to the window
		_mainWindow.SetCarDriver(_carController);

		desktop.MainWindow = _mainWindow;
	}
}
