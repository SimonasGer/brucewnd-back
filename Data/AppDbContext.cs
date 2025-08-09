using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Comic> Comics => Set<Comic>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ComicTag> ComicTags => Set<ComicTag>();
    // public DbSet<Chapter> Chapters => Set<Chapter>(); // if you have a Chapter entity

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // ---- Users / Roles (many-to-many via UserRole)
        b.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId });

        b.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId);

        b.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId);

        // Optional: sane uniqueness
        b.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();
        b.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        // ---- Comics
        b.Entity<Comic>()
            .Property(c => c.Name)
            .IsRequired();

        b.Entity<Comic>()
            .HasIndex(c => c.Name)
            .IsUnique();

        // Tie Comic to Author (no cascade delete wiping comics if a user is removed)
        b.Entity<Comic>()
            .HasOne(c => c.Author)
            .WithMany(u => u.Comics)             // ensure User has ICollection<Comic> Comics { get; set; }
            .HasForeignKey(c => c.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional default server time (works on SQLite/SQL Server; for PG use NOW())
        b.Entity<Comic>()
            .Property(c => c.CreatedAt)
            .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

        // ---- Tags
        b.Entity<Tag>()
            .Property(t => t.Name)
            .IsRequired();

        b.Entity<Tag>()
            .HasIndex(t => t.Name)
            .IsUnique(); // avoid duplicate tag names

        // Comic <-> Tag (many-to-many via ComicTag)
        b.Entity<ComicTag>()
            .HasKey(ct => new { ct.ComicId, ct.TagId });

        b.Entity<ComicTag>()
            .HasOne(ct => ct.Comic)
            .WithMany(c => c.Tags)
            .HasForeignKey(ct => ct.ComicId)
            .OnDelete(DeleteBehavior.Cascade);

        b.Entity<ComicTag>()
            .HasOne(ct => ct.Tag)
            .WithMany(t => t.Comics)
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent the same tag being attached twice to the same comic (extra safety)
        b.Entity<ComicTag>()
            .HasIndex(ct => new { ct.ComicId, ct.TagId })
            .IsUnique();
            
        // ---- Chapters
        b.Entity<Chapter>(e =>
        {
            e.HasKey(ch => ch.Id);

            e.Property(ch => ch.Title)
                .IsRequired();

            e.Property(ch => ch.ChapterNumber)
                .IsRequired();

            // Optional: DB-side defaults (Postgres)
            e.Property(ch => ch.CreatedAt)
                .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

            e.Property(ch => ch.IsPublished)
                .HasDefaultValue(false);

            // One Comic -> many Chapters
            e.HasOne(ch => ch.Comic)
                .WithMany(c => c.Chapters)
                .HasForeignKey(ch => ch.ComicId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique chapter number per comic (no duplicates like 1 twice)
            e.HasIndex(ch => new { ch.ComicId, ch.ChapterNumber })
                .IsUnique();

            // Optional: keep numbers positive
            e.ToTable(t => t.HasCheckConstraint("CK_Chapter_ChapterNumber_Positive", "\"ChapterNumber\" > 0"));
        });

    }
}