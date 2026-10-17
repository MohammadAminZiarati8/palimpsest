using Microsoft.EntityFrameworkCore;

namespace Game.Server.Data;

// Player state tables (players, champions, ledger, ...) arrive with M3; see docs/tech/backend.md.
public sealed class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options);
