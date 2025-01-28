using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Kullanici
    {
        [Key]
        public int Id { get; set; } // Benzersiz Kullanıcı ID

        [Required]
        [MaxLength(50)]
        public string UserName { get; set; } // Kullanıcı adı

        [Required]
        [MaxLength(256)]
        public string Password { get; set; } // Şifre (Hashlenmiş)

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } // E-posta adresi

        public string Location { get; set; } // Konum

        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } // Ad

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } // Soyad

        [Required]
        public DateTime BirthDate { get; set; } // Doğum tarihi

        [Required]
        [MaxLength(10)]
        public string Gender { get; set; } // Cinsiyet (örn: Erkek, Kadın)

        [MaxLength(15)]
        public string PhoneNumber { get; set; } // Telefon numarası

        public string ProfilePicture { get; set; } // Profil fotoğrafı yolu veya URL'si

        // Rol alanı (Admin: 1, Kullanıcı: 0)
        [Required]
        public RoleType Role { get; set; } = RoleType.User;

        public ICollection<Score> Scores { get; set; } = new List<Score>();

        // Many-to-Many Relationship for Interests
        public ICollection<UserInterest> UserInterests { get; set; } = new List<UserInterest>();
        public ICollection<Mesaj> SentMessages { get; set; } = new List<Mesaj>(); // Gönderilen mesajlar

    }
    public enum RoleType
    {
        Admin = 1,
        User = 0
    }
}
