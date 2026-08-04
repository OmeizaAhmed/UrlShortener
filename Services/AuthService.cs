using Microsoft.AspNetCore.Identity;
using UrlShortener.Models;
using UrlShortener.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Ganss.Xss;

public interface IAuthService
{
    Task<IdentityResult> RegisterUserAsync(RegisterInput input);
    Task<JwtResponse?> LoginUserAsync(string email, string password);
    Task<IdentityResult> CreateRoleAsync(string roleName);
    Task<IdentityResult> AddRoleToUserAsync(string email, string roleName);
    Task<RefreshTokenResponse> HandleRefreshTokenAsync();
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UrlShortenerContext _context;
    private readonly TokenServices _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, RoleManager<IdentityRole> roleManager, UrlShortenerContext context, TokenServices tokenService, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IdentityResult> RegisterUserAsync(RegisterInput input)
    {
        // sanitize input
        var sanitizer = new HtmlSanitizer();
        var email = sanitizer.Sanitize(input.Email.Trim().ToLower());
        var firstName = sanitizer.Sanitize(input.FirstName.Trim());
        var lastName = sanitizer.Sanitize(input.LastName.Trim());
        var password = sanitizer.Sanitize(input.Password.Trim());
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = lastName
            // You can add additional properties like FirstName and LastName if your ApplicationUser class has them
        };

        // Check if the user already exists
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "User with this email already exists." });
        }

        // add default role "user" to the new user
        var roleExists = await _roleManager.RoleExistsAsync("User");
        if (!roleExists)
        {
            await _roleManager.CreateAsync(new IdentityRole("User"));
        }

        var result = await _userManager.CreateAsync(user, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            return result;
        }else
        {
            return IdentityResult.Failed(new IdentityError { Description = "User registration failed. Please check the provided information." });
        }
        
    }

    public async Task<JwtResponse?> LoginUserAsync(string email, string password)
    {
        // sanitize input
        var sanitizer = new HtmlSanitizer();
        email = sanitizer.Sanitize(email.Trim().ToLower());
        password = sanitizer.Sanitize(password.Trim());
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return null;
        var roles = await _userManager.GetRolesAsync(user);

        var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
        if (result.Succeeded)
        {
            return await ProcessTokenAndCookies(new JwtInput
            {
                Id = user.Id,
                Email = user.UserName!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = roles.ToArray()
            });
        }

        return null;
    }
    public async Task<RefreshTokenResponse> HandleRefreshTokenAsync()
    {
        // get refresh token from cookie
        if (_httpContextAccessor?.HttpContext?.Request == null)
         return new RefreshTokenResponse { ErrorDescription = "Invalid request." };
        string? refresh = null;
        _httpContextAccessor?.HttpContext?.Request.Cookies.TryGetValue("X-Refresh-Token", out refresh);
        if (string.IsNullOrEmpty(refresh)) return new RefreshTokenResponse { ErrorDescription = "Refresh token is missing." };
        // get refresh token from database
        var oldRefresh = await _context.Refreshes.FirstOrDefaultAsync(r => r.Token == refresh);
        if (oldRefresh == null) return new RefreshTokenResponse { ErrorDescription = "Refresh token not found." };

        // check it has not yet expire
        if (oldRefresh.ExpireAt < DateTime.UtcNow) return new RefreshTokenResponse { ErrorDescription = "Refresh token has expired." };

        // validate if the user in the refresh is a registered user
        var registeredUser = await _userManager.FindByEmailAsync(oldRefresh.Email);

        if (registeredUser == null) return new RefreshTokenResponse { ErrorDescription = "User not found." };

        var roles = await _userManager.GetRolesAsync(registeredUser);

        // generate new jwt and new refresh
        var jwtResponse = await ProcessTokenAndCookies(new JwtInput
        {
            Id = registeredUser.Id,
            Email = registeredUser.UserName!,
            FirstName = registeredUser.FirstName,
            LastName = registeredUser.LastName,
            Role = roles.ToArray()
        });
        return new RefreshTokenResponse { Jwt = jwtResponse };
    }

    public async Task<IdentityResult> CreateRoleAsync(string roleName)
    {
        var sanitizer = new HtmlSanitizer();
        roleName = sanitizer.Sanitize(roleName.Trim());
        roleName = char.ToUpper(roleName[0]) + roleName.Substring(1).ToLower();
        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (roleExists)
            return IdentityResult.Success;

        var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
        return result;
    }

    public async Task<IdentityResult> AddRoleToUserAsync(string email, string roleName)
    {
        var sanitizer = new HtmlSanitizer();
        email = sanitizer.Sanitize(email.Trim().ToLower());
        roleName = sanitizer.Sanitize(roleName.Trim());
        roleName = char.ToUpper(roleName[0]) + roleName.Substring(1).ToLower();
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "User not found." });

        var result = await _userManager.AddToRoleAsync(user, roleName);

        if (!result.Succeeded)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Failed to add role to user." });
        }
        return result;
    }



    private async Task<JwtResponse> ProcessTokenAndCookies(JwtInput input)
    {
        var token = _tokenService.GenerateToken(input);
        // delete oldtokens
        var oldTokens = await _context.Refreshes.Where(r => r.Email == input.Email).ToListAsync();
        _context.Refreshes.RemoveRange(oldTokens);
        await _context.SaveChangesAsync();

        // generate refresh
        var refresh = _tokenService.GenerateRefreshToken(input.Email);
        _context.Refreshes.Add(new Refresh { Token = refresh.Token, Email = refresh.UserName });
        await _context.SaveChangesAsync();
        _httpContextAccessor?.HttpContext?.Response.Cookies.Append("X-Refresh-Token", refresh.Token, new CookieOptions
        {
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,
            Expires = DateTimeOffset.UtcNow.AddDays(7)

        });

        return new JwtResponse
        {
            Token = token,
            ExpiresIn = Environment.GetEnvironmentVariable("JWT_LIFETIME") ?? "",
            TokenType = "Bearer"
        };
    }

    

 
}
