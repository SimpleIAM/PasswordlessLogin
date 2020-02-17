// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using SimpleIAM.PasswordlessLogin.Configuration;
using SimpleIAM.PasswordlessLogin.Helpers;
using SimpleIAM.PasswordlessLogin.Models;
using SimpleIAM.PasswordlessLogin.Services;
using SimpleIAM.PasswordlessLogin.Services.EventNotification;
using SimpleIAM.PasswordlessLogin.Services.Message;
using SimpleIAM.PasswordlessLogin.Services.OTC;
using SimpleIAM.PasswordlessLogin.Services.Password;
using SimpleIAM.PasswordlessLogin.Stores;
using StandardResponse;

namespace SimpleIAM.PasswordlessLogin.Orchestrators
{
    public class AuthenticateOrchestrator
    {
        private enum SignInMethod
        {
            Password,
            OneTimeCode,
            Link
        }
        private readonly ILogger _logger;
        private readonly IEventNotificationService _eventNotificationService;
        private readonly IOneTimeCodeService _oneTimeCodeService;
        private readonly IMessageService _messageService;
        private readonly IUserStore _userStore;
        private readonly IPasswordService _passwordService;
        private readonly PasswordlessLoginOptions _options;
        private readonly IUrlService _urlService;
        private readonly HttpContext _httpContext;
        private readonly ITrustedBrowserStore _trustedBrowserStore;
        private readonly ISignInService _signInService;
        private readonly IApplicationService _applicationService;

        public AuthenticateOrchestrator(
            ILogger<AuthenticateOrchestrator> logger,
            IEventNotificationService eventNotificationService,
            IOneTimeCodeService oneTimeCodeService,
            IMessageService messageService,
            IUserStore userStore,
            PasswordlessLoginOptions passwordlessLoginOptions,
            IPasswordService passwordService,
            IUrlService urlService,
            IHttpContextAccessor httpContextAccessor,
            ITrustedBrowserStore trustedBrowserStore,
            ISignInService signInService,
            IApplicationService applicationService)
        {
            _logger = logger;
            _eventNotificationService = eventNotificationService;
            _oneTimeCodeService = oneTimeCodeService;
            _userStore = userStore;
            _messageService = messageService;
            _passwordService = passwordService;
            _options = passwordlessLoginOptions;
            _urlService = urlService;
            _httpContext = httpContextAccessor.HttpContext;
            _trustedBrowserStore = trustedBrowserStore;
            _signInService = signInService;
            _applicationService = applicationService;
        }

        public async Task<WebStatus> RegisterAsync(RegisterInputModel model)
        {
            var genericSuccessMessage = "Please check your email to complete the sign up process.";

            _logger.LogDebug("Begin registration for {0}", model.Email);

            if (!ApplicationIdIsNullOrValid(model.ApplicationId))
            {
                return WebStatus.Error("Invalid application id.", HttpStatusCode.BadRequest);
            }

            if (!await _userStore.UsernameIsAvailable(model.Email))
            {
                _logger.LogDebug("Existing user found.");

                var sendStatus = await _messageService.SendAccountAlreadyExistsMessageAsync(model.ApplicationId, model.Email);

                // Return a generic success message to prevent account discovery. Only the owner of 
                // the email address will get a customized message.
                return sendStatus.IsOk ? WebStatus.Success(genericSuccessMessage) : new WebStatus(sendStatus);
            }

            if (await _oneTimeCodeService.UnexpiredOneTimeCodeExistsAsync(model.Email))
            {
                // Although the username is available, there is a valid one time code
                // that can be used to cancel an email address change, so we can't
                // reuse the address quite yet
                return WebStatus.Error("Email address is temporarily reserved.", HttpStatusCode.Conflict);
            }

            _logger.LogDebug("Email address not used by an existing user. Creating a new user.");
            // consider: in some cases may want to restricting claims to a list predefined by the system administrator

            var status = new WebStatus();

            if (model.Claims == null)
            {
                model.Claims = new Dictionary<string, string>();
            }
            var internalClaims = new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>(
                    PasswordlessLoginConstants.Security.EmailNotConfirmedClaimType, "!")
            };
            var newUser = new User()
            {
                Email = model.Email,
                Claims = model.Claims
                    .Where(x =>
                        !PasswordlessLoginConstants.Security.ForbiddenClaims.Contains(x.Key) &&
                        !PasswordlessLoginConstants.Security.ProtectedClaims.Contains(x.Key))
                    .Union(internalClaims)
                    .Select(x => new UserClaim() { Type = x.Key, Value = x.Value })
            };
            var newUserResponse = await _userStore.AddUserAsync(newUser);
            if (newUserResponse.HasError)
            {
                return new WebStatus(newUserResponse.Status);
            }
            newUser = newUserResponse.Result;
            await _eventNotificationService.NotifyEventAsync(newUser.Email, EventType.Register);

            if (!string.IsNullOrEmpty(model.Password))
            {
                var setPasswordStatus = await _passwordService.SetPasswordAsync(newUser.SubjectId, model.Password);
                if (setPasswordStatus.IsOk)
                {
                    await _eventNotificationService.NotifyEventAsync(newUser.Email, EventType.SetPassword);
                }
                else
                {
                    status.AddWarning("Password was not set.");
                }
            }

            status.AddSuccess(genericSuccessMessage);
            
            var nextUrl = !string.IsNullOrEmpty(model.NextUrl) ? model.NextUrl : _urlService.GetDefaultRedirectUrl();
            if (model.SetPassword)
            {
                _logger.LogTrace("The user will be asked to set their password after confirming the account.");
                nextUrl = SendToSetPasswordFirst(nextUrl);
            }

            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(model.Email, TimeSpan.FromMinutes(_options.ConfirmAccountLinkValidityMinutes), nextUrl);
            switch (oneTimeCodeResponse.Status.StatusCode)
            {
                case GetOneTimeCodeStatusCode.Success:
                    var result = await _messageService.SendWelcomeMessageAsync(model.ApplicationId, model.Email, 
                        oneTimeCodeResponse.Result.ShortCode, oneTimeCodeResponse.Result.LongCode, model.Claims);
                    SetNonce(oneTimeCodeResponse.Result.ClientNonce);
                    return new WebStatus(status);
                case GetOneTimeCodeStatusCode.TooManyRequests:
                    return WebStatus.Error("Please wait a few minutes and try again.", HttpStatusCode.TooManyRequests);
                case GetOneTimeCodeStatusCode.ServiceFailure:
                default:
                    return ServerError("Hmm, something went wrong. Can you try again?");
            }
        }

        public async Task<WebStatus> SendOneTimeCodeAsync(SendCodeInputModel model)
        {
            _logger.LogDebug("Begin send one time code for {0}", model.Username);

            if (!ApplicationIdIsNullOrValid(model.ApplicationId))
            {
                return WebStatus.Error("Invalid application id.", HttpStatusCode.BadRequest);
            }

            // Note: Need to keep messages generic as to not reveal whether an account exists or not.
            var usernameIsValidEmail = EmailAddressChecker.EmailIsValid(model.Username);
            var defaultMessage = usernameIsValidEmail
                ? "Message sent. Please check your email."
                : "We sent a code to the email address asociated with your account (if found). Please check your email.";

            // If the username provide is not an email address or phone number, tell the user "we sent you a code if you have an account"
            var userResponse = await _userStore.GetUserByUsernameAsync(model.Username);
            if (userResponse.HasError)
            {
                _logger.LogDebug("User not found");

                if (!usernameIsValidEmail)
                {
                    _logger.LogError("No valid email address found for user {0}", model.Username);
                    return WebStatus.Success(defaultMessage); // generic message prevent account enumeration
                }

                var result = await _messageService.SendAccountNotFoundMessageAsync(model.ApplicationId, model.Username);
                if (result.HasError)
                {
                    return ServerError(result.Text);
                }
                // the fact that the user doesn't have an account is communicated privately via email
                await _eventNotificationService.NotifyEventAsync(model.Username, EventType.AccountNotFound);
                return WebStatus.Success(defaultMessage); // generic message prevent account enumeration
            }

            var user = userResponse.Result;
            _logger.LogDebug("User found");

            if (!EmailAddressChecker.EmailIsValid(user.Email))
            {
                _logger.LogError("No valid email address found for user {0}", model.Username);
                return WebStatus.Success(defaultMessage); // generic message prevent account enumeration
            }

            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(
                user.Email, 
                TimeSpan.FromMinutes(_options.OneTimeCodeValidityMinutes), 
                model.NextUrl);

            switch (oneTimeCodeResponse.Status.StatusCode)
            {
                case GetOneTimeCodeStatusCode.Success:
                    var status = await _messageService.SendOneTimeCodeAndLinkMessageAsync(model.ApplicationId, 
                        model.Username, oneTimeCodeResponse.Result.ShortCode, oneTimeCodeResponse.Result.LongCode);
                    await _eventNotificationService.NotifyEventAsync(model.Username, EventType.RequestOneTimeCode);
                    if (status.IsOk)
                    {
                        status = Status.Success(defaultMessage);
                    }
                    SetNonce(oneTimeCodeResponse.Result.ClientNonce);
                    return new WebStatus(status);
                case GetOneTimeCodeStatusCode.TooManyRequests:
                    return WebStatus.Error("Please wait a few minutes before requesting a new code.", HttpStatusCode.TooManyRequests);
                case GetOneTimeCodeStatusCode.ServiceFailure:
                default:
                    return ServerError("Hmm, something went wrong. Can you try again?");
            }            
        }

        public async Task<WebStatus> AuthenticateAsync(AuthenticatePasswordInputModel model)
        {
            _logger.LogDebug("Begin authentication for {0}", model.Username);

            var oneTimeCode = model.Password.Replace(" ", "");
            if (oneTimeCode.Length == PasswordlessLoginConstants.OneTimeCode.ShortCodeLength && oneTimeCode.All(Char.IsDigit))
            {
                _logger.LogDebug("Password was a six-digit number");
                var input = new AuthenticateInputModel()
                {
                    Username = model.Username,
                    OneTimeCode = oneTimeCode,
                    StaySignedIn = model.StaySignedIn
                };
                return await AuthenticateCodeAsync(input);
            }
            else
            {
                _logger.LogDebug("Password was not a six-digit number");
                return await AuthenticatePasswordAsync(model);
            }
        }

        public async Task<WebStatus> AuthenticateCodeAsync(AuthenticateInputModel model)
        {
            _logger.LogDebug("Begin one time code authentication for {0}", model.Username);

            model.OneTimeCode = model.OneTimeCode.Replace(" ", "");
            var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(model.Username, model.OneTimeCode, _httpContext.Request.GetClientNonce());
            switch (response.Status.StatusCode)
            {
                case CheckOneTimeCodeStatusCode.VerifiedWithNonce:
                case CheckOneTimeCodeStatusCode.VerifiedWithoutNonce:
                    var nonceWasValid = response.Status.StatusCode == CheckOneTimeCodeStatusCode.VerifiedWithNonce;
                    await _eventNotificationService.NotifyEventAsync(model.Username, EventType.SignInSuccess, SignInType.OneTimeCode.ToString());
                    return await SignInAndRedirectAsync(SignInMethod.OneTimeCode, model.Username, model.StaySignedIn, response.Result.RedirectUrl, nonceWasValid);
                case CheckOneTimeCodeStatusCode.Expired:
                    await _eventNotificationService.NotifyEventAsync(model.Username, EventType.SignInFail, SignInType.OneTimeCode.ToString());
                    return Unauthenticated("Your one time code has expired. Please request a new one.");
                case CheckOneTimeCodeStatusCode.CodeIncorrect:
                case CheckOneTimeCodeStatusCode.NotFound:
                    await _eventNotificationService.NotifyEventAsync(model.Username, EventType.SignInFail, SignInType.OneTimeCode.ToString());
                    return Unauthenticated("Invalid one time code.");
                case CheckOneTimeCodeStatusCode.ShortCodeLocked:
                    return Unauthenticated("The one time code is locked. Please request a new one after a few minutes.");
                case CheckOneTimeCodeStatusCode.ServiceFailure:
                default:
                    return ServerError("Something went wrong.");
            }
        }

        public async Task<WebStatus> AuthenticatePasswordAsync(AuthenticatePasswordInputModel model)
        {
            _logger.LogDebug("Begin password authentication for {0}", model.Username);

            var genericErrorMessage = "The username or password wasn't right.";
            var userResponse = await _userStore.GetUserByUsernameAsync(model.Username);
            if (userResponse.HasError)
            {
                _logger.LogDebug("User not found: {0}", model.Username);
                return Unauthenticated(genericErrorMessage);
            }
            else
            {
                var user = userResponse.Result;
                var checkPasswordStatus = await _passwordService.CheckPasswordAsync(user.SubjectId, model.Password);
                _logger.LogDebug(checkPasswordStatus.Text);
                if(checkPasswordStatus.IsOk)
                {
                    await _eventNotificationService.NotifyEventAsync(model.Username, EventType.SignInSuccess, SignInType.Password.ToString());
                    return await SignInAndRedirectAsync(SignInMethod.Password, model.Username, model.StaySignedIn, model.NextUrl, null);
                }
                switch(checkPasswordStatus.StatusCode)
                {
                    case CheckPasswordStatusCode.TemporarilyLocked:
                        return Unauthenticated("Your password is temporarily locked. Use a one time code to sign in.");
                    case CheckPasswordStatusCode.PasswordIncorrect:
                    case CheckPasswordStatusCode.NotFound:
                        await _eventNotificationService.NotifyEventAsync(model.Username, EventType.SignInFail, SignInType.Password.ToString());
                        return Unauthenticated(genericErrorMessage);
                    default:
                        return ServerError("Hmm. Something went wrong. Please try again.");
                }
            }
        }

        public async Task<WebStatus> AuthenticateLongCodeAsync(string longCode)
        {
            _logger.LogDebug("Begin long code (one time link) authentication");

            var response = await _oneTimeCodeService.CheckOneTimeCodeAsync(longCode, _httpContext.Request.GetClientNonce());
            switch (response.Status.StatusCode)
            {
                case CheckOneTimeCodeStatusCode.VerifiedWithoutNonce:
                case CheckOneTimeCodeStatusCode.VerifiedWithNonce:
                    await _eventNotificationService.NotifyEventAsync(response.Result.SentTo, EventType.SignInSuccess, SignInType.LongCode.ToString());
                    var nonceWasValid = response.Status.StatusCode == CheckOneTimeCodeStatusCode.VerifiedWithNonce;
                    return await SignInAndRedirectAsync(SignInMethod.Link, response.Result.SentTo, null, response.Result.RedirectUrl, nonceWasValid);
                case CheckOneTimeCodeStatusCode.Expired:
                    await _eventNotificationService.NotifyEventAsync(response.Result.SentTo, EventType.SignInFail, SignInType.LongCode.ToString());
                    return Unauthenticated("The sign in link expired.");
                case CheckOneTimeCodeStatusCode.CodeIncorrect:
                    return WebStatus.Error("Not found.", HttpStatusCode.NotFound);
                case CheckOneTimeCodeStatusCode.NotFound:
                    return Unauthenticated("The sign in link is invalid.");
                case CheckOneTimeCodeStatusCode.ServiceFailure:
                default:
                    return ServerError("Something went wrong.");
            }
        }

        public async Task<WebStatus> SendPasswordResetMessageAsync(SendPasswordResetMessageInputModel model)
        {
            _logger.LogDebug("Begin send password reset message for {0}", model.Username);

            if (!ApplicationIdIsNullOrValid(model.ApplicationId))
            {
                return WebStatus.Error("Invalid application id.", HttpStatusCode.BadRequest);
            }

            if (!await _userStore.UserExists(model.Username))
            {
                _logger.LogInformation("User not found: {0}", model.Username);
                // if valid email or phone number, send a message inviting them to register
                if (model.Username.Contains("@"))
                {
                    var result = await _messageService.SendAccountNotFoundMessageAsync(model.ApplicationId, model.Username);
                    if (result.HasError)
                    {
                        return ServerError(result.Text);
                    }
                    await _eventNotificationService.NotifyEventAsync(model.Username, EventType.AccountNotFound);
                }
                else
                {
                    _logger.LogInformation("Account not found message was not sent because provided username is not an email address: {0}", model.Username);
                }
                return WebStatus.Success("Check your email for password reset instructions.");
            }
            var nextUrl = SendToSetPasswordFirst(!string.IsNullOrEmpty(model.NextUrl) ? model.NextUrl : _urlService.GetDefaultRedirectUrl());
            var oneTimeCodeResponse = await _oneTimeCodeService.GetOneTimeCodeAsync(
                model.Username, 
                TimeSpan.FromMinutes(_options.OneTimeCodeValidityMinutes), 
                nextUrl);
            if (oneTimeCodeResponse.IsOk)
            {
                var status = await _messageService.SendPasswordResetMessageAsync(model.ApplicationId, model.Username, 
                    oneTimeCodeResponse.Result.ShortCode, oneTimeCodeResponse.Result.LongCode);
                await _eventNotificationService.NotifyEventAsync(model.Username, EventType.RequestPasswordReset);
                if(status.IsOk) 
                {
                    status = Status.Success("Check your email for password reset instructions.");
                }
                SetNonce(oneTimeCodeResponse.Result.ClientNonce);
                return new WebStatus(status);
            }
            _logger.LogError("Password reset message was not be sent due to error encountered while generating a one time link");
            return ServerError("Hmm. Something went wrong. Please try again.");
        }

        public async Task<WebStatus> SignOutAsync()
        {
            await _signInService.SignOutAsync();

            return WebStatus.Success();
        }

        private void SetNonce(string clientNonce)
        {
            if (clientNonce != null)
            {
                _logger.LogDebug("Saving client nonce in a browser cookie");
                _httpContext.Response.SetClientNonce(clientNonce, _options.OneTimeCodeValidityMinutes);
            }
        }

        private async Task<WebStatus> SignInAndRedirectAsync(SignInMethod method, string username, bool? staySignedIn, string nextUrl, bool? nonceWasValid)
        {
            _logger.LogTrace("Begining sign in and redirect logic for {0}", username);
            /*
            Do they have a partial sign in, and this was the second credential (code + password) or (password + code) [= 2 of 3]
                Yes, skip to SIGN IN
            Is this an trusted browser? [= 2 of 3]
                Yes, skip to SIGN IN
            Do they have NO trusted browsers? [= new account or someone who deleted all trusted browsers, 1 of 1|2]
                Yes, skip to SIGN IN
            Is 2FA enabled? (they have a password set and have chosen to require it when trusting a new browser)
                Yes. Do partial sign in and prompt for the relevant second factor (automatically mailed code or password)
            Used password to sign in? [= 1 of 2]
                Yes, skip to SIGN IN
            NonceWasValid [= 1 of 1|2]
                No, back to  sign in screen [prevents someone who passively observing code in transit/storage from using it undetected]
            (local) SIGN IN
            Is this a new browser?
                Yes, redirect to TRUST NEW BROWSER
            Is their account set to prompt them to choose a password
                Yes, redirect to SET PASSWORD
            Is their account set to prompt them to enable 2FA?
                Yes, redirect to ENABLE 2FA
            Has the application they are signing in to (if any) have required claims that have not been set?
                Yes, redirect to COMPLETE PROFILE
            Is their account set to prompt them to see/acknowledge any other screen?
                Yes, redirect SOMEWHERE
            FULL SIGN IN AND REDIRECT back to app (or to default post login page, apps page if none)

            */
            //do 2FA and figure out new browser, etc here

            var userResponse = await _userStore.GetUserByUsernameAsync(username, true);
            if (userResponse.HasError)
            {
                // this could only happen if an account was removed, but a method of signing in remained
                _logger.LogError("Strangely, there is no account for {0} anymore", username);
                return Unauthenticated("Account not found");
            }

            var user = userResponse.Result;
            var addTrustForThisBrowser = false;
            var browserIsTrusted = await _trustedBrowserStore
                .GetTrustedBrowserAsync(user.SubjectId, _httpContext.Request.GetBrowserId()) != null;

            var authMethod = (method == SignInMethod.Password) ? "pwd" : "otp";
            var authMethods = new string[] { authMethod };
            if(browserIsTrusted)
            {
                authMethods.Append("cookie");
            }
            if (_httpContext.User.Identity.IsAuthenticated && _httpContext.User.GetSubjectId() == user.SubjectId)
            {
                var previousAuthMethods = _httpContext.User.GetClaimValues("amr");
                if (previousAuthMethods.Any() && !previousAuthMethods.Contains(authMethod))
                {
                    // If already signed in and now the user has authenticated with another 
                    // type of credential, this is now a multi-factor session
                    authMethods = authMethods.Append("mfa").ToArray();
                    authMethods = previousAuthMethods.Union(authMethods).ToArray();
                    if (!browserIsTrusted)
                    {
                        _logger.LogInformation("Trusting browser because {0} performed multi-factor authentication", username);
                        addTrustForThisBrowser = true;
                    }
                }
            }

            var anyTrustedBrowsers = true;
            if (!browserIsTrusted && !addTrustForThisBrowser)
            {
                _logger.LogDebug("Browser user is signing in from is not trusted.");
                var trustedBrowsersResponse = await _trustedBrowserStore.GetTrustedBrowserAsync(user.SubjectId);
                anyTrustedBrowsers = trustedBrowsersResponse.Result?.Any() ?? false;
                if (anyTrustedBrowsers)
                {
                    _logger.LogDebug("User has other browsers that are trusted.");
                    // todo: if SecurityLevel == High OR first thing was a password, do a partial sign in and prompt for second thing

                    if ((method == SignInMethod.OneTimeCode || method == SignInMethod.Link) && _options.NonceRequiredOnUntrustedBrowser && nonceWasValid == false)
                    {
                        _logger.LogWarning("Client nonce was missing or invalid. Perhaps the one time code has " +
                            "been intercepted and an unathorized party is trying to user it. Authentication blocked.");
                        return Unauthenticated("Your one time code has expired. Please request a new one.");
                    }
                }
                else if (method == SignInMethod.Link || method == SignInMethod.OneTimeCode)
                {
                    // If no trusted browsers, trust the first browser that is used to verify a one time code
                    _logger.LogInformation("Trusting first browser used by {0}", username);
                    addTrustForThisBrowser = true;
                }
                else { 
                    _logger.LogDebug("User does not have any browsers that are trusted.");
                }
            }

            if (_options.AutoTrustBrowsers && addTrustForThisBrowser)
            {
                var description = _httpContext.Request.Headers["User-Agent"];
                var trustBrowserResponse = await TrustBrowserAsync(user.SubjectId, description);
                if (trustBrowserResponse.IsOk)
                {
                    browserIsTrusted = true;
                    anyTrustedBrowsers = true;
                }
            }

            var authProps = new AuthenticationProperties {
                AllowRefresh = true,
                IsPersistent = false,
            };
            if (staySignedIn == true || (method == SignInMethod.Link && browserIsTrusted))
            {
                _logger.LogTrace("Using maximum session length of {0} minutes", _options.MaxSessionLengthMinutes);
                authProps.IsPersistent = true;
                authProps.ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(_options.MaxSessionLengthMinutes));
            }
            else
            {
                _logger.LogTrace("Using default session length of {0} minutes", _options.DefaultSessionLengthMinutes);
            }

            if(method == SignInMethod.Link || method == SignInMethod.OneTimeCode)
            {
                // remove email unconfirmed claim, if present
                var removeClaims = new Dictionary<string, string>()
                {
                    { PasswordlessLoginConstants.Security.EmailNotConfirmedClaimType, null }
                }.ToLookup(x => x.Key, x => x.Value);
                await _userStore.PatchUserAsync(user.SubjectId, removeClaims, true);
            }

            _logger.LogDebug("Signing user in: {0}", user.Email);
            _logger.LogTrace("SubjectId: {0}", user.SubjectId);

            var userName = _options.IdpUserNameClaim == "email"
                ? user.Email
                : user.Claims.FirstOrDefault(x => x.Type == _options.IdpUserNameClaim)?.Value;

            userName = userName ?? user.Email;

            var userClaims = user.Claims
                .Where(c => _options.IdpUserClaims.Contains(c.Type))
                .Select(c => new Claim(c.Type, c.Value))
                .ToArray();

            if(_options.IdpUserClaims.Contains("email") && !userClaims.Any(c => c.Type == "email"))
            {
                userClaims = userClaims.Append(new Claim("email", user.Email)).ToArray();
            }

            await _signInService.SignInAsync(user.SubjectId, userName, authMethods, authProps, userClaims);

            nextUrl = ValidatedNextUrl(nextUrl);
            _logger.LogDebug("Redirecting user to: {0}", nextUrl);
            return WebStatus.Redirect(nextUrl);
        }

        public async Task<Response<string, WebStatus>> TrustBrowserAsync(string subjectId, string browserDescription = null)
        {
            _logger.LogDebug("Registering current browser as a trusted browser.");
            var browserId = _httpContext.Request.GetBrowserId();
            // todo: Review. Accepting an existing browser id opens an attack vector for pre-installing
            // cookies in a browser or via a malicious browser extension. May want to have a per-user
            // browser id that is stored in a BrowserId_[UniqueUserSuffix] cookie
            if (browserId == null || !(new Regex(@"^[0-9]{10,30}$").IsMatch(browserId)))
            {
                var rngProvider = new RNGCryptoServiceProvider();
                var byteArray = new byte[8];

                rngProvider.GetBytes(byteArray);
                var browserIdUInt = BitConverter.ToUInt64(byteArray, 0);
                browserId = browserIdUInt.ToString();
            }

            var response = await _trustedBrowserStore.AddTrustedBrowserAsync(subjectId, browserId, browserDescription);
            if(response.HasError)
            {
                return new Response<string, WebStatus>(new WebStatus(response.Status));
            }

            _httpContext.Response.SetBrowserId(browserId);

            return Response.Success<string, WebStatus>(browserId, "Trusted browser added.");
        }


        private string ValidatedNextUrl(string nextUrl)
        {
            if (_urlService.IsAllowedRedirectUrl(nextUrl))
            {
                return nextUrl;
            }
            _logger.LogWarning("Next url was not valid: '{0}'. Using default redirect url instead.", nextUrl);
            return _urlService.GetDefaultRedirectUrl();
        }

        private string SendToSetPasswordFirst(string nextUrl)
        {
            var setPasswordUrl = _urlService.GetSetPasswordUrl();
            return $"{setPasswordUrl}?nextUrl={nextUrl}";
        }

        private bool ApplicationIdIsNullOrValid(string applicationId)
        {
            // Duplicate code in UserOrchestrator
            if(applicationId == null)
            {
                return true;
            }
            if (!_applicationService.ApplicationExists(applicationId))
            {
                _logger.LogError("Invalid application id '{0}'", applicationId);
                return false;
            }
            return true;
        }

        private WebStatus Unauthenticated(string message = null)
        {
            return WebStatus.Error(message, HttpStatusCode.Unauthorized);
        }

        private WebStatus ServerError(string message = null)
        {
            return WebStatus.Error(message, HttpStatusCode.InternalServerError);
        }
    }
}