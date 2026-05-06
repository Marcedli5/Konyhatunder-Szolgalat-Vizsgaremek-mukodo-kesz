using System.Globalization;
using System.Security.Claims;
using Konyhatunder_Szolgalat_Vizsgaremek.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend_ASP.Services
{
    public class LegacyUserLinkService
    {
        public const string LegacyUserIdClaimType = "legacy_user_id";

        private readonly UserManager<IdentityUser> _userManager;
        private readonly VizsgaremekEtlapContext _legacyContext;

        public LegacyUserLinkService(UserManager<IdentityUser> userManager, VizsgaremekEtlapContext legacyContext)
        {
            _userManager = userManager;
            _legacyContext = legacyContext;
        }

        public async Task<IdentityUser?> GetCurrentIdentityUserAsync(ClaimsPrincipal principal)
        {
            return await _userManager.GetUserAsync(principal);
        }

        public async Task<ulong> GetRequiredLegacyUserIdAsync(ClaimsPrincipal principal)
        {
            var identityUser = await GetCurrentIdentityUserAsync(principal)
                ?? throw new InvalidOperationException("A felhasználó nincs bejelentkezve.");

            return await EnsureLegacyUserAsync(identityUser);
        }

        public async Task<ulong> EnsureLegacyUserAsync(
            IdentityUser identityUser,
            string? preferredName = null,
            string? phone = null,
            string? address = null)
        {
            var currentClaims = await _userManager.GetClaimsAsync(identityUser);
            var legacyClaim = currentClaims.FirstOrDefault(claim => claim.Type == LegacyUserIdClaimType);
            if (legacyClaim is not null &&
                ulong.TryParse(legacyClaim.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLegacyUserId))
            {
                var existingLinkedUser = await _legacyContext.Users.FirstOrDefaultAsync(user => user.Id == parsedLegacyUserId);
                if (existingLinkedUser is not null)
                {
                    return existingLinkedUser.Id;
                }
            }

            var email = identityUser.Email ?? throw new InvalidOperationException("A felhasználóhoz nincs e-mail cím.");
            var legacyUser = await _legacyContext.Users.FirstOrDefaultAsync(user => user.Email == email);

            if (legacyUser is null)
            {
                legacyUser = new User
                {
                    FullName = preferredName ?? identityUser.UserName ?? email.Split('@')[0],
                    Email = email,
                    Phone = NormalizeOptional(phone ?? identityUser.PhoneNumber),
                    Address = NormalizeOptional(address),
                    PasswordHash = "identity-managed",
                    IsActive = true
                };

                _legacyContext.Users.Add(legacyUser);
                await _legacyContext.SaveChangesAsync();
            }
            else
            {
                legacyUser.FullName = preferredName ?? legacyUser.FullName;
                legacyUser.Email = email;
                legacyUser.Phone = NormalizeOptional(phone ?? identityUser.PhoneNumber) ?? legacyUser.Phone;
                legacyUser.Address = NormalizeOptional(address) ?? legacyUser.Address;
                legacyUser.IsActive = true;
                await _legacyContext.SaveChangesAsync();
            }

            await SaveLegacyUserClaimAsync(identityUser, legacyUser.Id, legacyClaim);
            return legacyUser.Id;
        }

        public async Task UpdateLegacyProfileAsync(IdentityUser identityUser, string username)
        {
            var legacyUserId = await EnsureLegacyUserAsync(identityUser, username);
            var legacyUser = await _legacyContext.Users.FirstAsync(user => user.Id == legacyUserId);
            legacyUser.FullName = username;
            legacyUser.Email = identityUser.Email ?? legacyUser.Email;
            legacyUser.Phone = identityUser.PhoneNumber ?? legacyUser.Phone;
            legacyUser.IsActive = true;
            await _legacyContext.SaveChangesAsync();
        }

        private static string? NormalizeOptional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task SaveLegacyUserClaimAsync(IdentityUser identityUser, ulong legacyUserId, Claim? existingClaim)
        {
            var newClaim = new Claim(LegacyUserIdClaimType, legacyUserId.ToString(CultureInfo.InvariantCulture));

            if (existingClaim is null)
            {
                await _userManager.AddClaimAsync(identityUser, newClaim);
                return;
            }

            if (existingClaim.Value != newClaim.Value)
            {
                await _userManager.ReplaceClaimAsync(identityUser, existingClaim, newClaim);
            }
        }
    }
}
