using Microsoft.AspNetCore.Identity;
using Taskverse.Data.DataAccess;

if (args.Length < 4)
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project API/Tools/Taskverse.PasswordHashGenerator -- \"Full Name\" \"email@domain.com\" \"Role\" \"PlainTextPassword\"");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  dotnet run --project API/Tools/Taskverse.PasswordHashGenerator -- \"Platform Super Admin\" \"superadmin@yourdomain.com\" \"SuperAdmin\" \"YourStrongPassword123!\"");
    return 1;
}

var fullName = args[0].Trim();
var email = args[1].Trim().ToLowerInvariant();
var role = args[2].Trim();
var password = args[3];

var user = new User
{
    Id = Guid.NewGuid(),
    FullName = fullName,
    Email = email,
    Role = role
};

var hasher = new PasswordHasher<User>();
var hash = hasher.HashPassword(user, password);

Console.WriteLine("Seed values:");
Console.WriteLine($"  id: {user.Id}");
Console.WriteLine($"  full_name: {user.FullName}");
Console.WriteLine($"  email: {user.Email}");
Console.WriteLine($"  role: {user.Role}");
Console.WriteLine($"  password_hash: {hash}");

return 0;
