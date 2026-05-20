using Autopartspro.Domain.Entities;

namespace Autopartspro.Infrastructure.Services;

public static class VehicleDisplayHelper
{
    public static string? FormatNumberPlate(Vehicle? vehicle) =>
        string.IsNullOrWhiteSpace(vehicle?.NumberPlate) ? null : vehicle.NumberPlate.Trim();

    public static string? FormatDescription(Vehicle? vehicle)
    {
        if (vehicle is null) return null;
        var makeModel = $"{vehicle.Make} {vehicle.Model}".Trim();
        var parts = new List<string>();
        if (vehicle.Year > 0) parts.Add(vehicle.Year.ToString());
        if (!string.IsNullOrWhiteSpace(makeModel)) parts.Add(makeModel);
        if (!string.IsNullOrWhiteSpace(vehicle.FuelType.ToString())) parts.Add(vehicle.FuelType.ToString());
        return parts.Count == 0 ? null : string.Join(" ", parts);
    }

    public static string? FormatFull(Vehicle? vehicle)
    {
        var plate = FormatNumberPlate(vehicle);
        var desc = FormatDescription(vehicle);
        if (plate is null && desc is null) return null;
        if (plate is null) return desc;
        if (desc is null) return plate;
        return $"{desc} ({plate})";
    }
}
