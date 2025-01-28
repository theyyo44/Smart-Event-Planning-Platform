using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Score
    {
        [Key]
        public int Id { get; set; } // Benzersiz Puan ID

        public int KullaniciId { get; set; } // Foreign Key (Kullanıcı)
        public Kullanici Kullanici { get; set; } // Navigasyon

        public int Value { get; set; } // Kazanılan Puan

        public DateTime EarnedDate { get; set; } // Puan Kazanım Tarihi
    }
}
