using Microsoft.AspNetCore.Authentication;
using System.Threading.Tasks;

namespace SimpleIAM.PasswordlessLogin.Services
{
    public interface ISignInService
    {
        Task SignInAsync(string subjectId, string username, AuthenticationProperties authProps);
        Task SignOutAsync(string subjectId, string username);
    }
}
