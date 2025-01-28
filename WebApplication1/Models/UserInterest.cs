using System.ComponentModel.DataAnnotations;
using WebApplication1.Models;

public class UserInterest
{
    [Key]
    public int Id { get; set; }

    public int KullaniciId { get; set; } // Foreign Key (Kullanıcı)
    public Kullanici Kullanici { get; set; }

    public int KategoriId { get; set; } // Foreign Key (Kategori)
    public Kategori Kategori { get; set; }
}
