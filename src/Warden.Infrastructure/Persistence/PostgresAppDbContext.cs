using Microsoft.EntityFrameworkCore;

namespace Warden.Infrastructure.Persistence;

public class PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : AppDbContext(options);
