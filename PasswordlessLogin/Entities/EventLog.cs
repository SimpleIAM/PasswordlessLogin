// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System;

namespace SimpleIAM.PasswordlessLogin.Entities
{
    public class EventLog
    {
        public long Id { get; set; } 
        public DateTime Time { get; set; }
        public string Username { get; set; }
        public string EventType { get; set; }
        public string Details { get; set; }
    }
}
