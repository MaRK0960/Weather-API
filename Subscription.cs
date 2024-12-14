namespace Weather_API
{
    public record Subscription(string Email, float DeltaTemperature, TimeOnly[]? NotificationTime);
}