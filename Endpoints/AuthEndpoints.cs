using System.Security.Claims;
using System.Security.Cryptography;
using GhanaHybridRentalApi.Data;
using GhanaHybridRentalApi.Dtos;
using GhanaHybridRentalApi.Models;
using GhanaHybridRentalApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhanaHybridRentalApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/auth/register", RegisterAsync);
        app.MapPost("/api/v1/auth/login", LoginAsync);
        app.MapPost("/api/v1/auth/phone/send-code", SendPhoneCodeAsync);
        app.MapPost("/api/v1/auth/phone/verify-code", VerifyPhoneCodeAsync);
        
        // Password reset endpoints
        app.MapPost("/api/v1/auth/forgot-password", ForgotPasswordAsync);
        app.MapPost("/api/v1/auth/reset-password", ResetPasswordAsync);
        // Allows guest users (created via booking) to set a password using their phone and receive a JWT.
        // Frontends may call this after creating a guest booking when `account.requirePasswordSetup` is true.
        app.MapPost("/api/v1/auth/set-password", SetPasswordAsync);
        app.MapPost("/api/v1/auth/change-password", ChangePasswordAsync)
            .RequireAuthorization();
        
        // Legacy endpoints (keep for backward compatibility)
        app.MapPost("/api/v1/auth/request-otp", RequestOtpAsync);
        app.MapPost("/api/v1/auth/verify-otp", VerifyOtpAsync);
    }

    // Set password for guest account (claim account)
    private static async Task<IResult> SetPasswordAsync(
        [FromBody] SetPasswordRequest request,
        AppDbContext db,
        PasswordHasher hasher,
        IJwtTokenService jwt)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Phone and password are required" });

        if (request.Password.Length < 8)
            return Results.BadRequest(new { error = "Password must be at least 8 characters" });

        var phone = NormalizePhoneNumber(request.Phone);
        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone);

        if (user is null)
            return Results.BadRequest(new { error = "Account not found" });

        if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            return Results.BadRequest(new { error = "Account already has a password. Please login or use reset-password flow." });

        // Set password and mark phone verified / activate account if pending
        user.PasswordHash = hasher.HashPassword(request.Password);
        user.PhoneVerified = true;
        if (user.Status == "pending")
            user.Status = "active";

        await db.SaveChangesAsync();

        var token = jwt.GenerateToken(user);

        return Results.Ok(new AuthResponse(user.Id, user.Role, token,
            new UserProfile(
                user.Id,
                user.Email,
                user.Phone,
                user.FirstName,
                user.LastName,
                user.Role,
                user.Status,
                user.PhoneVerified
            )
        ));
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterRequest request,
        AppDbContext db,
        PasswordHasher hasher,
        IJwtTokenService jwt,
        ITurnstileService turnstile)
    {
        // 1. Verify Turnstile token first
        if (!string.IsNullOrWhiteSpace(request.TurnstileToken))
        {
            var (isValid, errors) = await turnstile.VerifyTokenAsync(request.TurnstileToken);
            if (!isValid)
            {
                return Results.Json(
                    new { error = "Bot verification failed. Please try again.", turnstileErrors = errors },
                    statusCode: 400
                );
            }
        }

        // 2. Continue with regular registration
        if (string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Password is required" });

        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
            return Results.BadRequest(new { error = "Email or phone is required" });

        var role = request.Role?.ToLowerInvariant();
        if (role != "renter" && role != "owner" && role != "admin")
            return Results.BadRequest(new { error = "Role must be renter, owner, or admin" });

        // Renters must have phone
        if (role == "renter" && string.IsNullOrWhiteSpace(request.Phone))
            return Results.BadRequest(new { error = "Renter must register with a phone number" });

        // Owners/Admins must have email
        if ((role == "owner" || role == "admin") && string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "Owner/Admin must register with an email" });

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existingEmail = await db.Users.AnyAsync(u => u.Email == request.Email);
            if (existingEmail)
                return Results.BadRequest(new { error = "Email already registered" });
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var existingPhone = await db.Users.AnyAsync(u => u.Phone == request.Phone);
            if (existingPhone)
                return Results.BadRequest(new { error = "Phone already registered" });
        }

        var user = new User
        {
            Email = request.Email,
            Phone = request.Phone,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PasswordHash = hasher.HashPassword(request.Password),
            Role = role!,
            // Owners/Admins: pending; Renters: active
            Status = role == "owner" || role == "admin" ? "pending" : "active",
            // For now, treat renter phones as verified (no external OTP)
            PhoneVerified = role == "renter",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);

        if (role == "owner")
        {
            db.OwnerProfiles.Add(new OwnerProfile
            {
                UserId = user.Id,
                OwnerType = string.IsNullOrWhiteSpace(request.CompanyName) ? "individual" : "business",
                CompanyName = request.CompanyName,
                DisplayName = request.CompanyName ?? $"{request.FirstName} {request.LastName}".Trim(),
                // Explicit default values to avoid relying on DB defaults
                CompanyVerificationStatus = "unverified",
                PayoutVerificationStatus = "unverified",
                PayoutPreference = "momo"
            });
        }
        else if (role == "renter")
        {
            db.RenterProfiles.Add(new RenterProfile
            {
                UserId = user.Id,
                VerificationStatus = "basic_verified"
            });
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { error = "Registration failed", details = ex.Message, innerException = ex.InnerException?.Message });
        }

        var token = jwt.GenerateToken(user);

        return Results.Created(
            $"/api/v1/users/{user.Id}",
            new AuthResponse(user.Id, user.Role, token,
                new UserProfile(
                    user.Id,
                    user.Email,
                    user.Phone,
                    user.FirstName,
                    user.LastName,
                    user.Role,
                    user.Status,
                    user.PhoneVerified
                )
            )
        );
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        AppDbContext db,
        PasswordHasher hasher,
        IJwtTokenService jwt,
        ITurnstileService turnstile)
    {
        // 1. Verify Turnstile token first
        if (!string.IsNullOrWhiteSpace(request.TurnstileToken))
        {
            var (isValid, errors) = await turnstile.VerifyTokenAsync(request.TurnstileToken);
            if (!isValid)
            {
                return Results.Json(
                    new { error = "Bot verification failed. Please try again.", turnstileErrors = errors },
                    statusCode: 400
                );
            }
        }

        // 2. Continue with regular login
        var identifier = request.GetIdentifier().Trim();
        
        if (string.IsNullOrWhiteSpace(identifier))
            return Results.BadRequest(new { error = "Email or phone is required" });
            
        if (string.IsNullOrWhiteSpace(request.Password))
            return Results.BadRequest(new { error = "Password is required" });
            
        var isEmail = identifier.Contains("@");

        // Decide lookup based on identifier
        User? user;
        if (isEmail)
        {
            user = await db.Users.FirstOrDefaultAsync(u => u.Email == identifier);
        }
        else
        {
            user = await db.Users.FirstOrDefaultAsync(u => u.Phone == identifier);
        }

        if (user is null)
            return Results.BadRequest(new { error = "Invalid credentials" });

        // Guardrails to enforce UX rules
        if (isEmail && user.Role == "renter")
        {
            return Results.BadRequest(new { error = "Renters must login with phone number" });
        }

        if (!isEmail && (user.Role == "owner" || user.Role == "admin"))
        {
            return Results.BadRequest(new { error = "Owners/Admins must login with email" });
        }

        if (!hasher.VerifyPassword(request.Password, user.PasswordHash))
            return Results.BadRequest(new { error = "Invalid credentials" });

        if (user.Status == "suspended")
            return Results.BadRequest(new { error = "Account suspended" });

        // Owners and admins must be verified (active) before they can login
        if ((user.Role == "owner" || user.Role == "admin") && user.Status != "active")
            return Results.BadRequest(new { error = "Your account is pending verification. Please contact support." });

        var token = jwt.GenerateToken(user);

        return Results.Ok(new AuthResponse(user.Id, user.Role, token,
            new UserProfile(
                user.Id,
                user.Email,
                user.Phone,
                user.FirstName,
                user.LastName,
                user.Role,
                user.Status,
                user.PhoneVerified
            )
        ));
    }

    // Simplified phone authentication - Step 1: Send verification code
    private static async Task<IResult> SendPhoneCodeAsync(
        [FromBody] SendPhoneCodeRequest request,
        AppDbContext db,
        IWhatsAppSender whatsappSender)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return Results.BadRequest(new { error = "Phone number is required" });

        var phone = NormalizePhoneNumber(request.Phone);

        // Generate 6-digit code
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        // Invalidate any previous unused codes for this phone
        var previousCodes = await db.OtpCodes
            .Where(o => o.Phone == phone && !o.Used)
            .ToListAsync();
        
        foreach (var old in previousCodes)
        {
            old.Used = true;
        }

        // Create new OTP
        var otp = new OtpCode
        {
            Phone = phone,
            Code = code,
            Purpose = "login",
            Channel = "whatsapp",
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        };

        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync();

        // Send via WhatsApp
        var message = $"Your Ghana Hybrid Rental verification code is: {code}\n\nThis code expires in 10 minutes.\n\nIf you didn't request this, please ignore.";
        await whatsappSender.SendVerificationCodeAsync(phone, message);

        return Results.Ok(new { 
            success = true, 
            message = "Verification code sent via WhatsApp",
            phone,
            expiresIn = 600 // seconds
        });
    }

    // Simplified phone authentication - Step 2: Verify code and login/register
    private static async Task<IResult> VerifyPhoneCodeAsync(
        [FromBody] VerifyPhoneCodeRequest request,
        AppDbContext db,
        IJwtTokenService jwt)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Code))
            return Results.BadRequest(new { error = "Phone and code are required" });

        var phone = NormalizePhoneNumber(request.Phone);
        var code = request.Code.Trim();

        // Find valid OTP
        var otp = await db.OtpCodes
            .Where(o => o.Phone == phone && o.Code == code && !o.Used && o.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp is null)
            return Results.BadRequest(new { error = "Invalid or expired verification code" });

        // Mark OTP as used
        otp.Used = true;

        // Find or create user
        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone);

        if (user is null)
        {
            // Auto-register new user
            var role = request.Role?.ToLowerInvariant() ?? "renter";
            if (role != "renter" && role != "owner")
                role = "renter"; // Default to renter

            user = new User
            {
                Phone = phone,
                Role = role,
                Status = "active",
                PhoneVerified = true,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            db.Users.Add(user);

            // Create profile based on role
            if (role == "owner")
            {
                db.OwnerProfiles.Add(new OwnerProfile
                {
                    UserId = user.Id,
                    OwnerType = "individual",
                    DisplayName = $"{request.FirstName} {request.LastName}".Trim()
                });
            }
            else
            {
                db.RenterProfiles.Add(new RenterProfile
                {
                    UserId = user.Id,
                    FullName = $"{request.FirstName} {request.LastName}".Trim(),
                    VerificationStatus = "phone_verified"
                });
            }
        }
        else
        {
            // Existing user - verify phone and activate
            user.PhoneVerified = true;
            if (user.Status == "pending")
                user.Status = "active";
        }

        await db.SaveChangesAsync();

        var token = jwt.GenerateToken(user);
        
        return Results.Ok(new AuthResponse(
            user.Id, 
            user.Role, 
            token,
            new UserProfile(
                user.Id,
                user.Email,
                user.Phone,
                user.FirstName,
                user.LastName,
                user.Role,
                user.Status,
                user.PhoneVerified
            )
        ));
    }

    // Helper method to normalize phone numbers
    private static string NormalizePhoneNumber(string phone)
    {
        // Remove spaces, dashes, and other characters
        var normalized = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        
        // Add Ghana country code if missing
        if (!normalized.StartsWith("233") && normalized.StartsWith("0"))
        {
            normalized = "233" + normalized.Substring(1);
        }
        else if (!normalized.StartsWith("233"))
        {
            normalized = "233" + normalized;
        }
        
        return normalized;
    }

    // Keep existing OTP methods for backward compatibility
    private static async Task<IResult> RequestOtpAsync(
        [FromBody] RequestOtpRequest request,
        AppDbContext db,
        IWhatsAppSender whatsappSender,
        IWebHostEnvironment env)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return Results.BadRequest(new { error = "Phone is required" });

        var phone = request.Phone.Trim();
        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? "register" : request.Purpose.Trim().ToLowerInvariant();
        var channel = string.IsNullOrWhiteSpace(request.Channel) ? "whatsapp" : request.Channel.Trim().ToLowerInvariant();

        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        var otp = new OtpCode
        {
            Phone = phone,
            Code = code,
            Purpose = purpose,
            Channel = channel,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
            Used = false
        };

        db.OtpCodes.Add(otp);
        await db.SaveChangesAsync();

        var message = $"Your verification code is {code}. It expires in 10 minutes.";
        await whatsappSender.SendVerificationCodeAsync(phone, message);

        // For dev convenience, return code in response. Remove this in production.
        return Results.Ok(new { success = true, phone, purpose, channel, code });
    }

    private static async Task<IResult> VerifyOtpAsync(
        [FromBody] VerifyOtpRequest request,
        AppDbContext db,
        IJwtTokenService jwt)
    {
        var phone = request.Phone.Trim();
        var code = request.Code.Trim();

        var otp = await db.OtpCodes
            .Where(o => o.Phone == phone && o.Code == code && !o.Used && o.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(o => o.ExpiresAt)
            .FirstOrDefaultAsync();

        if (otp is null)
            return Results.BadRequest(new { error = "Invalid or expired code" });

        otp.Used = true;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Phone == phone);

        if (user is null)
        {
            var role = request.Role?.ToLowerInvariant() ?? "renter";
            if (role != "renter" && role != "owner")
            {
                return Results.BadRequest(new { error = "Role must be renter or owner when auto-creating user" });
            }

            user = new User
            {
                Phone = phone,
                Role = role,
                Status = "active",
                PhoneVerified = true
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            if (role == "owner")
            {
                db.OwnerProfiles.Add(new OwnerProfile
                {
                    UserId = user.Id,
                    OwnerType = "individual",
                    DisplayName = request.FullName
                });
            }
            else if (role == "renter")
            {
                db.RenterProfiles.Add(new RenterProfile
                {
                    UserId = user.Id,
                    FullName = request.FullName,
                    VerificationStatus = "basic_verified"
                });
            }
        }
        else
        {
            user.PhoneVerified = true;
            if (user.Status == "pending")
                user.Status = "active";
        }

        await db.SaveChangesAsync();

        var token = jwt.GenerateToken(user);
        return Results.Ok(new AuthResponse(user.Id, user.Role, token));
    }

    // Password Reset - Step 1: Request reset token
    private static async Task<IResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request,
        AppDbContext db,
        IEmailService emailService)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return Results.BadRequest(new { error = "Email is required" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        
        // For security, always return success even if email doesn't exist
        // This prevents email enumeration attacks
        if (user is null)
            return Results.Ok(new { message = "If an account exists with that email, a password reset link has been sent." });

        // Generate secure reset token
        var resetToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        user.PasswordResetToken = resetToken;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour

        await db.SaveChangesAsync();

        // Get app domain from admin settings (defaults to ryverental.com)
        var domainSetting = await db.GlobalSettings.FirstOrDefaultAsync(s => s.Key == "app_domain");
        var appDomain = "ryverental.com";
        if (domainSetting != null)
        {
            try
            {
                var domainData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(domainSetting.ValueJson);
                if (domainData != null && domainData.ContainsKey("domain"))
                    appDomain = domainData["domain"];
            }
            catch { /* Use default */ }
        }

        // Send email with reset link
        var resetUrl = $"https://{appDomain}/reset-password?token={resetToken}";
        await emailService.SendPasswordResetEmailAsync(user.Email!, resetUrl);

        return Results.Ok(new { 
            message = "If an account exists with that email, a password reset link has been sent."
        });
    }

    // Password Reset - Step 2: Reset password with token
    private static async Task<IResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request,
        AppDbContext db,
        PasswordHasher hasher)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return Results.BadRequest(new { error = "Token and new password are required" });

        if (request.NewPassword.Length < 8)
            return Results.BadRequest(new { error = "Password must be at least 8 characters" });

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token && 
                                     u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user is null)
            return Results.BadRequest(new { error = "Invalid or expired reset token" });

        // Update password
        user.PasswordHash = hasher.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Password reset successfully. You can now login with your new password." });
    }

    // Change Password (for logged-in users)
    private static async Task<IResult> ChangePasswordAsync(
        ClaimsPrincipal principal,
        [FromBody] ChangePasswordRequest request,
        AppDbContext db,
        PasswordHasher hasher)
    {
        var userIdStr = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Results.Unauthorized();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return Results.BadRequest(new { error = "Current password and new password are required" });

        if (request.NewPassword.Length < 8)
            return Results.BadRequest(new { error = "New password must be at least 8 characters" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
            return Results.NotFound(new { error = "User not found" });

        // Verify current password
        if (!hasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return Results.BadRequest(new { error = "Current password is incorrect" });

        // Update to new password
        user.PasswordHash = hasher.HashPassword(request.NewPassword);
        await db.SaveChangesAsync();

        return Results.Ok(new { message = "Password changed successfully" });
    }
}

// DTOs for password reset
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record SetPasswordRequest(string Phone, string Password);

