using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication1.Models
{
    public class Etkinlik
    {
        [Key]
        public int Id { get; set; } // Benzersiz Etkinlik ID

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } // Etkinlik adı

        [MaxLength(500)]
        public string Description { get; set; } // Açıklama

        [Required]
        public DateTime Date { get; set; } // Tarih

        [Required]
        public TimeSpan Duration { get; set; } // Süre

        public string Location { get; set; } // Konum

        public string ImageUrl { get; set; } // Görsel URL'si

        [Required]
        public EventStatus Status { get; set; } = EventStatus.Pending; // Varsayılan: Bekliyor


        [Required]
        public int CreatedByUserId { get; set; } // Etkinliği oluşturan kullanıcı ID

        [ForeignKey("CreatedByUserId")]
        public Kullanici CreatedByUser { get; set; } // Etkinliği oluşturan kullanıcı ile ilişki


        // Many-to-Many Relationship for Categories
        public ICollection<EventCategory> EventCategories { get; set; } = new List<EventCategory>();

        // Participants Relationship
        public ICollection<Participant> Participants { get; set; } = new List<Participant>();

        public ICollection<Mesaj> Messages { get; set; } = new List<Mesaj>(); // Mesajlar
    }

    // Etkinlik durumu enum
    public enum EventStatus
    {
        Pending,    // Onay bekliyor
        Approved,   // Onaylandı
        Rejected    // Reddedildi
    }
}
