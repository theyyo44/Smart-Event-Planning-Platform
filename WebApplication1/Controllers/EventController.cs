using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace WebApplication1.Controllers
{
    public class EventController : Controller
    {
        private readonly Context _context;

        public EventController(Context context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AddEvent()
        {

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var userName = HttpContext.Session.GetString("UserName") ?? "Kullanıcı";

            ViewData["UserProfilePicture"] = HttpContext.Session.GetString("UserProfilePicture") ?? "~/images/profile.png";

            ViewData["UserName"] = userName;
            ViewData["UserId"] = userId;

            // Kategorileri ViewBag ile gönder
            ViewBag.Categories = _context.Kategoriler.ToList();
            return View();
        }



//*--------------------------------------------------Yeni Etkinlik Ekleme -----------------------------------------------------------------------------------*//
        [HttpPost]
        public IActionResult AddEvent(string Name, string Description, DateTime Date, int DurationHours, int DurationMinutes, string Address, int selectedCategoryId, IFormFile Image)
        {
            // Kullanıcı ID'sini oturumdan al
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Lütfen giriş yapın.";
                return RedirectToAction("Login", "Home");
            }

            // Seçilen kategori kontrolü
            var selectedCategory = _context.Kategoriler.FirstOrDefault(c => c.Id == selectedCategoryId);
            if (selectedCategory == null)
            {
                TempData["ErrorMessage"] = "Geçerli bir kategori seçin.";
                return RedirectToAction("AddEvent");
            }
            // Süreyi TimeSpan'e dönüştür
            var duration = new TimeSpan(DurationHours, DurationMinutes, 0);
            if (duration.TotalHours >= 24)
            {
                TempData["ErrorMessage"] = "Süre 24 saatten büyük olamaz.";
                return RedirectToAction("AddEvent");
            }


            // Resim dosyasını kaydet
            string imageUrl = null;
            if (Image != null && Image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
                var filePath = Path.Combine("wwwroot/images/", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    Image.CopyTo(stream);
                }

                imageUrl = "/images/" + fileName; // Resim URL'si
            }

            // Yeni etkinlik oluştur
            var newEvent = new Etkinlik
            {
                Name = Name,
                Description = Description,
                Date = Date,
                Duration = duration,
                Location = Address,
                ImageUrl = imageUrl, // Resim URL'sini ata
                Status = EventStatus.Pending, // Varsayılan durum
                CreatedByUserId = int.Parse(userId) // Etkinliği oluşturan kullanıcı
            };

            // Kategoriyi ilişkilendir
            newEvent.EventCategories.Add(new EventCategory
            {
                Etkinlik = newEvent,
                Kategori = selectedCategory
            });

            // Veritabanına kaydet
            _context.Etkinlikler.Add(newEvent);


            // Kullanıcıya puan ekle
            var score = new Score
            {
                KullaniciId = int.Parse(userId),
                Value = 15, // Etkinlik oluşturma puanı
                EarnedDate = DateTime.Now
            };
            _context.Puanlar.Add(score);


            _context.SaveChanges();

            TempData["SuccessMessage"] = "Etkinlik başarıyla eklendi ve onay bekliyor.";
            return RedirectToAction("Index", "User");
        }

 //*--------------------------------------------------Map Üzerinden Etkinlikleri Gösterme-----------------------------------------------------------------------------------*//
        public IActionResult Map()
        {
            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "Misafir Kullanıcı";
            ViewData["UserProfilePicture"] = HttpContext.Session.GetString("UserProfilePicture") ?? "~/images/profile.png";

            // Tüm onaylanmış etkinlikleri al
            var etkinlikler = _context.Etkinlikler
                .Where(e => e.Status == EventStatus.Approved)
                .Select(e => new
                {
                    e.Id,
                    e.Name,
                    e.Location,
                    e.Date,
                    e.Duration,
                    e.Description,
                    e.ImageUrl
                })
                .ToList();

            // JSON olarak View'e gönder
            ViewData["EtkinliklerJson"] = System.Text.Json.JsonSerializer.Serialize(etkinlikler);
            return View();
        }



//*--------------------------------------------------Etkinlik Detay ve Mesajlasşma-----------------------------------------------------------------------------------*//

        public IActionResult Details(int id)
        {
            


            var etkinlik = _context.Etkinlikler
                                    .Include(e => e.Messages)
                                    .ThenInclude(m => m.Sender)
                                    .FirstOrDefault(e => e.Id == id);

            if (etkinlik == null)
            {
                return View();
            }


            return View(etkinlik);
        }


        // Yorum Ekleme
        [HttpPost]
        public IActionResult AddComment(int etkinlikId, string content)
        {
            // Kullanıcı oturum kontrolü
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Account");
            }

            // Yeni yorum oluşturma
            var yeniYorum = new Mesaj
            {
                EtkinlikId = etkinlikId,
                SenderId = int.Parse(userId),
                Content = content,
                SentDate = DateTime.Now
            };

            _context.Mesajlar.Add(yeniYorum);
            _context.SaveChanges();

            return RedirectToAction("Details", new { id = etkinlikId });
        }





        //*--------------------------------------------Etkinlik Katılma-----------------------------------------------------------------------------------------------------------------*//
        [HttpPost]
        public IActionResult JoinEvent(int EtkinlikId)
        {
            // Kullanıcının oturum bilgileri
            var userId = HttpContext.Session.GetString("UserId");
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Etkinliğe katılmak için giriş yapmalısınız.";
                return RedirectToAction("Index", "Home");
            }

            var userIdInt = int.Parse(userId);

            // Katılmak istenilen etkinlik
            var newEvent = _context.Etkinlikler.FirstOrDefault(e => e.Id == EtkinlikId);
            if (newEvent == null)
            {
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                return RedirectToAction("Index", "User");
            }

            // Kullanıcının daha önce katıldığı etkinlikler
            var userEvents = _context.Katilimlar
                                     .Where(p => p.KullaniciId == userIdInt)
                                     .Select(p => p.Etkinlik)
                                     .ToList();

            // Yeni etkinliğin başlangıç ve bitiş zaman ayarı
            var newEventStart = newEvent.Date;
            var newEventEnd = newEvent.Date.Add(newEvent.Duration);

            // Zaman çakışması kontrolü
            foreach (var userEvent in userEvents)
            {
                var userEventStart = userEvent.Date;
                var userEventEnd = userEvent.Date.Add(userEvent.Duration);

                if (newEventStart < userEventEnd && newEventEnd > userEventStart)
                {
                    TempData["ErrorMessage"] = $"Etkinlik çakışması: \"{userEvent.Name}\" adlı etkinlikle aynı zaman diliminde.";
                    return RedirectToAction("Details", new { id = EtkinlikId });
                }
            }

            // Çakışma yoksa etkinliğe katıl
            _context.Katilimlar.Add(new Participant
            {
                KullaniciId = userIdInt,
                EtkinlikId = EtkinlikId,
                ParticipationDate = DateTime.Now
            });

            if (!_context.Katilimlar.Any(p => p.KullaniciId == userIdInt && p.EtkinlikId == EtkinlikId))
            {
                var bonusScore = new Score
                {
                    KullaniciId = userIdInt,
                    Value = 20,
                    EarnedDate = DateTime.Now
                };
                _context.Puanlar.Add(bonusScore);
            }

            // Katılan Kullanıcıya puan ekle
            var score = new Score
            {
                KullaniciId = userIdInt,
                Value = 10, // Etkinliğe katılma puanı
                EarnedDate = DateTime.Now
            };
            _context.Puanlar.Add(score);

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Etkinliğe başarıyla katıldınız.";
            return RedirectToAction("Index", "User");
        }




        //*------------------------------------------- Etkinlik Güncellme İşlemi-------------------------------------------------------------------------------------------------*//

        [HttpGet]
        public IActionResult UpdateEvent(int id)
        {
            var etkinlik = _context.Etkinlikler.Include(e => e.EventCategories)
                                               .FirstOrDefault(e => e.Id == id);

            if (etkinlik == null)
            {
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                return RedirectToAction("MyEvent", "User");
            }

            // Kategorileri gönder
            ViewBag.Categories = _context.Kategoriler.ToList();

            return View(etkinlik);
        }


        [HttpPost]
        public IActionResult UpdateEvent(int id, Etkinlik model, IFormFile Image, int selectedCategoryId, int DurationHours, int DurationMinutes)
        {
            var etkinlik = _context.Etkinlikler.Include(e => e.EventCategories)
                                               .FirstOrDefault(e => e.Id == id);

            if (etkinlik == null)
            {
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                return RedirectToAction("MyEvent", "User");
            }

            // Etkinlik bilgilerini güncelle
            etkinlik.Name = model.Name;
            etkinlik.Description = model.Description;
            etkinlik.Date = model.Date;
            etkinlik.Duration = new TimeSpan(DurationHours, DurationMinutes, 0);
            etkinlik.Location = model.Location;

            // Yeni resim eklenmişse kaydet
            if (Image != null && Image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
                var filePath = Path.Combine("wwwroot/images/", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    Image.CopyTo(stream);
                }

                etkinlik.ImageUrl = "/images/" + fileName; // Yeni resim yolu
            }

            // Kategori güncellemesi
            etkinlik.EventCategories.Clear();
            etkinlik.EventCategories.Add(new EventCategory { KategoriId = selectedCategoryId, EtkinlikId = id });

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Etkinlik başarıyla güncellendi!";
            return RedirectToAction("MyEvent", "User");
        }



    }
}
