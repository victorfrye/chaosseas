namespace VictorFrye.ChaosSeas.SeaConditionsApi;

/// <summary>
/// Represents the state of the sea using the World Meteorological Organization sea state codes.
/// </summary>
public enum SeaState
{
    Calm,
    Slight,
    Moderate,
    Rough,
    VeryRough,
    High,
    Phenomenal
}

/// <summary>
/// Represents a snapshot of current sea conditions at a point in time.
/// </summary>
/// <param name="State">The current sea state classification.</param>
/// <param name="WindSpeedKnots">Wind speed measured in knots.</param>
/// <param name="WaveHeightMeters">Wave height measured in meters.</param>
/// <param name="VisibilityNauticalMiles">Visibility measured in nautical miles.</param>
/// <param name="WaterTemperatureCelsius">Water temperature measured in degrees Celsius.</param>
/// <param name="Description">A descriptive, nautical-themed summary of conditions.</param>
/// <param name="Timestamp">The date and time when the conditions were observed.</param>
public record SeaCondition(
    SeaState State,
    double WindSpeedKnots,
    double WaveHeightMeters,
    double VisibilityNauticalMiles,
    double WaterTemperatureCelsius,
    string Description,
    DateTimeOffset Timestamp);
