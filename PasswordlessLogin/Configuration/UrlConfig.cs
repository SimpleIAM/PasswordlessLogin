namespace SimpleIAM.PasswordlessLogin.Configuration
{
    public class UrlConfig
    {
        public string DefaultRedirect { get; set; } = "/";
        public string MyAccount { get; set; } = "/account";
        public string ForgotPassword { get; set; } = "/forgotpassword";
        public string Register { get; set; } = "/register";
        public string SetPassword { get; set; } = "/account/setpassword";
        public string SignIn { get; set; } = "/signin";
        public string SignInLink { get; set; } = "/signin/{long_code}";
        public string SignOut { get; set; } = "/signout";
    }
}
