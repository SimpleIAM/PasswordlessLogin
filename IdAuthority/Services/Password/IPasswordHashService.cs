// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

namespace SimpleIAM.IdAuthority.Services.Password
{
    // note: no need for async here for cpu-bound work (unless this is offloaded to separate, specialized hardware)
    public interface IPasswordHashService
    {
        string HashPassword(string password);

        CheckPaswordHashResult CheckPasswordHash(string passwordHash, string password);
    }
}
