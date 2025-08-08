using Microsoft.EntityFrameworkCore;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
// Register user
        app.MapPost("/users/register", async (RegisterUserDto dto, AppDbContext db, IConfiguration config) =>
        {
            // duplicate username check
            if (await db.Users.AnyAsync(u => u.Username == dto.Username))
                return Results.BadRequest("Username already taken.");

            var strategy = db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await db.Database.BeginTransactionAsync();

                var isFirstUser = !await db.Users.AnyAsync();

                var user = new User
                {
                    Username = dto.Username,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
                };

                static async Task<Role> GetOrCreateRoleAsync(AppDbContext db, string name)
                {
                    var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == name);
                    if (role is null)
                    {
                        role = new Role { Name = name };
                        db.Roles.Add(role);
                        await db.SaveChangesAsync();
                    }
                    return role;
                }

                var rolesToAssign = new List<string> { "user" };
                if (isFirstUser) rolesToAssign.AddRange(new[] { "mod", "admin" });

                foreach (var rn in rolesToAssign.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    var role = await GetOrCreateRoleAsync(db, rn);
                    user.UserRoles.Add(new UserRole { RoleId = role.Id, Role = role, User = user });
                }

                db.Users.Add(user);
                await db.SaveChangesAsync();

                await tx.CommitAsync();

                // reload with roles for JWT
                var withRoles = await db.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .SingleAsync(u => u.Id == user.Id);

                var token = JwtHelper.GenerateToken(withRoles, config);

                return Results.Created($"/users/{withRoles.Id}", new
                {
                    withRoles.Id,
                    withRoles.Username,
                    Roles = withRoles.UserRoles.Select(ur => ur.Role.Name).ToArray(),
                    Token = token
                });
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
