using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VideoCallApp.Application.Common;
using VideoCallApp.Application.DTOs.Auth;
using VideoCallApp.Application.Interfaces;
using VideoCallApp.Domain.Entities;

namespace VideoCallApp.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ITokenService tokenService,
        IMapper mapper)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _mapper = mapper;
    }

    public async Task<ApiResponse<AuthResponseDto>> RegisterAsync(RegisterRequestDto request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                "Registration failed",
                "User with this email already exists");
        }

        // Create new user
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                "Registration failed",
                errors);
        }

        // Generate token
        var token = _tokenService.GenerateAccessToken(user);

        var response = _mapper.Map<AuthResponseDto>(user);
        response.Token = token;
        response.Expiration = DateTime.UtcNow.AddHours(24);

        return ApiResponse<AuthResponseDto>.SuccessResponse(
            response,
            "User registered successfully");
    }

    public async Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto request)
    {
        // Find user
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                "Login failed",
                "Invalid email or password");
        }

        // Check password
        var result = await _signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            return ApiResponse<AuthResponseDto>.ErrorResponse(
                "Login failed",
                "Invalid email or password");
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Generate token
        var token = _tokenService.GenerateAccessToken(user);

        var response = _mapper.Map<AuthResponseDto>(user);
        response.Token = token;
        response.Expiration = DateTime.UtcNow.AddHours(24);

        return ApiResponse<AuthResponseDto>.SuccessResponse(
            response,
            "Login successful");
    }
}