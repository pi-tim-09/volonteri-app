using Microsoft.AspNetCore.Identity;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Password helper service for secure password hashing and verification
    /// Uses ASP.NET Core Identity's PasswordHasher which implements PBKDF2 with salt
    /// Follows Single Responsibility Principle - handles only password operations
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Hash a password using PBKDF2 with salt
        /// </summary>
        string HashPassword(string password);

        /// <summary>
        /// Verify a password against a hash
        /// </summary>
        bool VerifyPassword(string hashedPassword, string providedPassword);
    }

    /// <summary>
    /// Implementation of password hashing service
    /// Uses Microsoft.AspNetCore.Identity.PasswordHasher for secure PBKDF2 hashing
    /// </summary>
    public class PasswordHasherService : IPasswordHasher
    {
        private readonly PasswordHasher<User> _passwordHasher;

        public PasswordHasherService()
        {
            _passwordHasher = new PasswordHasher<User>();
        }

        public string HashPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException("Password cannot be null or empty", nameof(password));
            }

            // Hash password using PBKDF2 with automatic salt generation
            // PasswordHasher<User> uses a dummy user object for hashing
            return _passwordHasher.HashPassword(null!, password);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword))
            {
                throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));
            }

            if (string.IsNullOrWhiteSpace(providedPassword))
            {
                throw new ArgumentException("Provided password cannot be null or empty", nameof(providedPassword));
            }

            var result = _passwordHasher.VerifyHashedPassword(null!, hashedPassword, providedPassword);
            return result == PasswordVerificationResult.Success || 
                   result == PasswordVerificationResult.SuccessRehashNeeded;
        }
    }
}
