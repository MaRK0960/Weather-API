namespace Weather_API
{
    public record WeatherUser
    {
        public Guid id { get; init; }
        public string Email { get; init; }
        public float DeltaTemperature { get; set; }
        public int[] NotificationTime { get; set; }

        public WeatherUser(Guid id, string email, float deltaTemperature, int[] notificationTime)
        {
            this.id = id;
            Email = email;
            DeltaTemperature = deltaTemperature;
            NotificationTime = notificationTime;
        }
    }
}