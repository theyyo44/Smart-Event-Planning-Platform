using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Mesaj
    {
        [Key]
        public int Id { get; set; } // Benzersiz Mesaj ID

        public int SenderId { get; set; } // Foreign Key (Gönderen Kullanıcı)
        public Kullanici Sender { get; set; } // Navigasyon

        public int EtkinlikId { get; set; } // Foreign Key (Etkinlik))
        public Etkinlik Etkinlik { get; set; } // İlgili Etkinlik Navigasyonu

        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } // Mesaj metni

        [Required]
        public DateTime SentDate { get; set; } // Gönderim tarihi
    }
}
