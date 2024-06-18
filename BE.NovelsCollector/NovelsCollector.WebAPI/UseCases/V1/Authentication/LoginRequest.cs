﻿using System.ComponentModel.DataAnnotations;

namespace NovelsCollector.WebAPI.UseCases.V1.Authentication
{
    public class LoginRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}