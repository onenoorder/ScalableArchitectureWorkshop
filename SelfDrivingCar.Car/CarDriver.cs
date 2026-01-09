using SelfDrivingCar.TomTom;
using SelfDrivingCar.World;

namespace SelfDrivingCar.Car;

public class CarDriver
{
	public Coordinate CurrentPosition;
	public bool IsActive = false;
	public double CurrentSpeed = 0;
	public double CurrentBearing = 0;
	public List<Road>? CurrentRoute = new List<Road>();
	public int CurrentRoadIndex = 0;

	private Node currentStart;
	private Node currentDestination;
	private MotMotNavigate navigation;

	public CarDriver(MotMotNavigate navigation)
	{
		this.navigation = navigation;
	}

	public List<Road>? CalculateRoute(Node start, Node destination)
	{
		return navigation.Navigate(start, destination);
	}

	public void UpdateRoute(List<Road> route)
	{
		CurrentRoute = route;
		CurrentRoadIndex = 0;
	}

	public void StartDriving(Node start, Node destination, CancellationToken cancellationToken = default)
	{
		Console.WriteLine("🚗 Starting self-driving car...");
		Console.WriteLine($"📍 Starting at ({start.Coordinate.Latitude:F2}, {start.Coordinate.Longitude:F2})");
		Console.WriteLine();
		currentStart = start;
		currentDestination = destination;
		CurrentPosition = start.Coordinate;

		IsActive = true;

		List<Road>? route = navigation.Navigate(start, destination);
		CurrentRoute = route;
		CurrentRoadIndex = 0;
		int roadNum = 0;
		foreach (Road road in route)
		{
			Console.WriteLine($"🛣️  Road {roadNum}: Distance={road.Distance:F2}km, Bearing={road.Bearing:F1}°, SpeedLimit={road.SpeedLimit}km/h");
			if (!TravelAlongRoad(road))
			{
				break;
			}

			CurrentRoadIndex++;
			roadNum++;
		}

		IsActive = false;
	}

	public void Reset()
	{
		CurrentPosition = currentStart.Coordinate;
		CurrentSpeed = 0;
		CurrentRoadIndex = 0;
	}

	private bool TravelAlongRoad(Road road,
	  CancellationToken cancellationToken = default)
	{
		double traveledDistance = 0;
		CurrentSpeed = road.SpeedLimit;
		double distance = road.Distance;
		double bearing = road.Bearing;

		CurrentBearing = bearing;

		while (traveledDistance < distance)
		{
			// this is to instantly stop when cancellation is requested rather than finishing the current road
			if (cancellationToken.IsCancellationRequested)
			{
				IsActive = false;
				return false;
			}

			// Animation speed scaling:
			// Default realistic speed would be: (speed_km_h / 3600) * 0.05 km per 50ms frame
			// To make animation faster/visible, we multiply the speed by a factor
			// speedScaleFactor = 2400 means routes complete in ~12-15 seconds for typical distances
			// Adjust this constant to make animation faster (higher) or slower (lower)
			const double speedScaleFactor = 2400.0;

			// Calculate distance to travel in this 50ms frame
			// Formula: (speed_km_h * speedScaleFactor / 3600.0) * time_seconds
			// Simplifies to: speed_km_h * speedScaleFactor / 72000
			double distanceToTravel = (CurrentSpeed * speedScaleFactor) / 72000.0;

			double remainingDistance = distance - traveledDistance;

			// Check if this is the last segment of the road
			bool isLastSegment = (remainingDistance - distanceToTravel) <= 0.0001;

			// Don't travel past the end of the road
			if (distanceToTravel > remainingDistance)
			{
				distanceToTravel = remainingDistance;
			}

			CurrentPosition = WorldMaths.CalculateDestinationPoint(CurrentPosition, bearing, distanceToTravel);
			traveledDistance += distanceToTravel;

			// Prevent drift accumulation on road transitions by snapping to road bearing
			// This ensures the bearing is always exactly the road bearing, not drifting due to float precision
			Thread.Sleep(50);
		}

		return true;
	}
}
