// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using System.Net;

namespace SimpleIAM.PasswordlessLogin
{
    public class Response<T>
    {
        public Status Status { get; set; } = new Status();
        public T Result { get; set; }
        public bool HasError => Status.HasError;
        public bool IsOk => Status.IsOk;      
    }

    public static class Response
    {
        public static Response<T> Success<T>(T result, string message = null)
        {
            var response = new Response<T>
            {
                Result = result
            };

            if (message != null)
            {
                response.Status.AddSuccess(message);
            }

            return response;
        }

        public static Response<T> Error<T>(string message, HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        {
            var response = new Response<T>();
            response.Status.AddError(message, statusCode);

            return response;
        }

        public static Response<T> ResultWithStatus<T>(T result, Status status)
        {
            return new Response<T>
            {
                Result = result,
                Status = status
            };
        }

        public static Response<T> StatusOnly<T>(Status status)
        {
            return new Response<T>
            {
                Status = status
            };
        }
    }
}
