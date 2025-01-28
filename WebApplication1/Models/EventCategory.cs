using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

public class EventCategory
{
    [Key]
    public int Id { get; set; }

    public int EtkinlikId { get; set; } // Foreign Key (Etkinlik)
    public Etkinlik Etkinlik { get; set; }

    public int KategoriId { get; set; } // Foreign Key (Kategori)
    public Kategori Kategori { get; set; }
}
