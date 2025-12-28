namespace WorkoutCoachV2.Web.Services
{
    // Config-model voor SMTP mail: host/poort/SSL + login + afzendergegevens .
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "";
        public int SmtpPort { get; set; } = 587;
        public bool UseSsl { get; set; } = true;

        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";

        public string FromAddress { get; set; } = "";
        public string FromName { get; set; } = "WorkoutCoach";
    }
}
