using Microsoft.EntityFrameworkCore;

namespace Warden.Infrastructure.Persistence;

public class MariaDbAppDbContext(DbContextOptions<MariaDbAppDbContext> options) : AppDbContext(options);
