using Microsoft.EntityFrameworkCore;

namespace Warden.Infrastructure.Persistence;

public class SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : AppDbContext(options);
