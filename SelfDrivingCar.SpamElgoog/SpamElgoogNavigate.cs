using SelfDrivingCar.World;

namespace SelfDrivingCar.SpamElgoog;

public class SpamElgoogNavigate
{
	private int MIN_STEP_PERCENT = 5;
	private int MAX_STAP_PERCENT = 10;
	private double MAX_BEARING_DEVIATION = 45.0;
	private int[] speeds = [25, 35, 55, 65, 80, 90, 125];



	public List<Road> GenerateRoads(Coordinate start, Coordinate destination)
	{
		Random random = new Random();
		int amountOfRoads = random.Next(MIN_STEP_PERCENT, MAX_STAP_PERCENT);
		Coordinate currentLocation = start;
		List<Road> result = new List<Road>();
		double totalMiles = GeoMath.CalculateDistance(start, destination);
		double distanceToGo = totalMiles;


		for (int i = 0; i < amountOfRoads - 1; i++)
		{

			double distance = random.NextDouble() * ((distanceToGo / 2) - 1.0) + 1.0;
			double bearing = GetRandomizedBearing(GeoMath.CalculateBearing(currentLocation, destination));

			Coordinate currentTarget = GeoMath.CalculateDestinationPoint(currentLocation, bearing, distance);

			Road road = new()
			{
				From = currentLocation,
				SpeedLimit = speeds[random.Next(speeds.Length - 1)],
				To = currentTarget
			};
			result.Add(road);
			currentLocation = currentTarget;
			distanceToGo = GeoMath.CalculateDistance(currentTarget, destination);

		}

		result.Add(new()
		{
			From = currentLocation,
			SpeedLimit = speeds[random.Next(speeds.Length - 1)],
			To = destination
		});

		return result;
	}

	private double GetRandomizedBearing(double bearing)
	{
		double offset = (Random.Shared.NextDouble() * 2 - 1) * MAX_BEARING_DEVIATION;
		double newBearing = bearing + offset;

		newBearing = (newBearing % 360 + 360) % 360;
		return newBearing;
	}
}
