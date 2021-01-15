namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class MailOptions
    {
        public string FromEmail { get; set; } = "from.address@not.configured";
        public string FromDisplayName { get; set; } = PasswordlessLoginConstants.DefaultDisplayName;
        public SmtpOptions Smtp { get; set; }
    }
}