using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Models
{
    public class Context : DbContext
    {
        private readonly IConfiguration _configuration;

        public Context(DbContextOptions<Context> options, IConfiguration configuration)
            : base(options)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }
        }

        // DbSet Properties (Tablolar)
        public DbSet<Kullanici> Kullanicilar { get; set; } // Kullanıcılar
        public DbSet<Etkinlik> Etkinlikler { get; set; } // Etkinlikler
        public DbSet<Kategori> Kategoriler { get; set; } // Kategoriler
        public DbSet<UserInterest> KullaniciKategorileri { get; set; } // Kullanıcı-Kategori Many-to-Many
        public DbSet<EventCategory> EtkinlikKategorileri { get; set; } // Etkinlik-Kategori Many-to-Many
        public DbSet<Participant> Katilimlar { get; set; } // Katılımlar
        public DbSet<Mesaj> Mesajlar { get; set; } // Mesajlar
        public DbSet<Score> Puanlar { get; set; } // Puanlar

        

        // OnModelCreating: Veritabanı ilişkilerinin yapılandırılması
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Etkinlik>()
                .HasOne(e => e.CreatedByUser) // İlişkili kullanıcı
                .WithMany() // Kullanıcı birden fazla etkinlik oluşturabilir
                .HasForeignKey(e => e.CreatedByUserId) // Yabancı anahtar
        .OnDelete(DeleteBehavior.Restrict); // Kullanıcı silinirse etkinlikler etkilenmez


            // Kullanıcı-Kategori Many-to-Many İlişkisi
            modelBuilder.Entity<UserInterest>()
                .HasKey(uc => new { uc.KullaniciId, uc.KategoriId }); // Composite Primary Key

            modelBuilder.Entity<UserInterest>()
                .HasOne(uc => uc.Kullanici)
                .WithMany(u => u.UserInterests)
                .HasForeignKey(uc => uc.KullaniciId);

            modelBuilder.Entity<UserInterest>()
                .HasOne(uc => uc.Kategori)
                .WithMany(c => c.UserInterests)
                .HasForeignKey(uc => uc.KategoriId);

            // Etkinlik-Kategori Many-to-Many İlişkisi
            modelBuilder.Entity<EventCategory>()
                .HasKey(ec => new { ec.EtkinlikId, ec.KategoriId }); // Composite Primary Key

            modelBuilder.Entity<EventCategory>()
                .HasOne(ec => ec.Etkinlik)
                .WithMany(e => e.EventCategories)
                .HasForeignKey(ec => ec.EtkinlikId);

            modelBuilder.Entity<EventCategory>()
                .HasOne(ec => ec.Kategori)
                .WithMany(c => c.EventCategories)
                .HasForeignKey(ec => ec.KategoriId);

            // Diğer ilişkiler (opsiyonel, örn: mesajlar ve katılımlar)
            // Kullanıcı - Mesaj İlişkisi
            modelBuilder.Entity<Mesaj>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId);

            modelBuilder.Entity<Mesaj>()
                .HasOne(m => m.Etkinlik)
                .WithMany(e => e.Messages)
                .HasForeignKey(m => m.EtkinlikId);
        }
    }
}
