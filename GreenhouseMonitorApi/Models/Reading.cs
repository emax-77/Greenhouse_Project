namespace GreenhouseMonitorApi.Models;

public class Reading
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }
}
