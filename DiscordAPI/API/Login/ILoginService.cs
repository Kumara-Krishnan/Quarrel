﻿using Discord_UWP.API.Login.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Discord_UWP.API.Login
{
    public interface ILoginService
    {
        [Post("/auth/login")]
        Task<LoginResult> Login([Body] LoginRequest loginRequest);

        [Post("/auth/mfa/totp")]
        Task<LoginResult> LoginMFA([Body] LoginMFARequest loginRequest);

        [Post("/auth/mfa/sms")]
        Task<LoginResult> LoginSMS([Body] LoginMFARequest loginRequest);

        [Post("/auth/mfa/sms/send")]
        Task<SendSmsResult> SendSMS([Body] SendSmsRequest loginRequest);

    }
}
