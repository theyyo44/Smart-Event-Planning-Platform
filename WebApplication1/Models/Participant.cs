using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class Participant
    {
        [Key]
        public int Id { get; set; } // Benzersiz Katılım ID

        public int KullaniciId { get; set; } // Foreign Key (Kullanıcı)
        public Kullanici Kullanici { get; set; } // Navigasyon

        public int EtkinlikId { get; set; } // Foreign Key (Etkinlik)
        public Etkinlik Etkinlik { get; set; } // Navigasyon

        public DateTime ParticipationDate { get; set; } // Katılım Tarihi
    }
}
