using Microsoft.EntityFrameworkCore;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        // Register user
        app.MapPost("/users/register", async (RegisterUserDto dto, AppDbContext db, IConfiguration config) =>
        {
            var exists = await db.Users.AnyAsync(u => u.Username == dto.Username);
            if (exists)
                return Results.BadRequest("Username already taken.");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = hashedPassword
            };

            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == "user")
                ?? new Role { Name = "user" };

            user.UserRoles.Add(new UserRole { Role = role });

            db.Users.Add(user);
            await db.SaveChangesAsync();

            var token = JwtHelper.GenerateToken(user, config);

            return Results.Created($"/users/{user.Id}", new
            {
                user.Id,
                user.Username,
                Roles = new[] { "user" },
                Token = token
            });
        });

        // Login user
        app.MapPost("/users/login", async (LoginUserDto dto, AppDbContext db, IConfiguration config) =>
        {
            var user = await db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == dto.Username);

            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Results.BadRequest("Invalid username or password.");

            var token = JwtHelper.GenerateToken(user, config);

            return Results.Ok(new
            {
                user.Id,
                user.Username,
                Roles = user.UserRoles.Select(ur => ur.Role.Name),
                Token = token
            });
        });

        // Get all users
        app.MapGet("/users", async (AppDbContext db) =>
        {
            var users = await db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList()
                })
                .ToListAsync();

            return Results.Ok(users);
        });

        // Get specific user
        app.MapGet("/users/{id}", async (int id, AppDbContext db) =>
        {
            var user = await db.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return Results.NotFound();

            var dto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Roles = [.. user.UserRoles.Select(ur => ur.Role.Name)]
            };

            return Results.Ok(dto);
        });
    }
}
