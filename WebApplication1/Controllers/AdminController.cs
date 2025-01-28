using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class AdminController : Controller
    {

        private readonly Context _context;

        public AdminController(Context context)
        {
            _context = context;
        }

        // ADMİN ANA SAYFA
        public IActionResult Index()
        {
            // Kullanıcı oturum kontrolü
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            // Eğer oturum yoksa veya admin değilse giriş sayfasına yönlendir
            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }
            // Bekleyen etkinlikleri al
            var pendingEvents = _context.Etkinlikler.Where(e => e.Status == EventStatus.Pending).ToList();
            ViewData["PendingEvents"] = pendingEvents;

            ViewData["Title"] = "Admin Dashboard";
            return View();
        }


        //ONAYLAMA
        [HttpPost]
        public IActionResult ApproveEvent(int eventId)
        {
            // Etkinliği ID'ye göre bul
            var eventItem = _context.Etkinlikler.FirstOrDefault(e => e.Id == eventId);
            if (eventItem != null)
            {
                eventItem.Status = EventStatus.Approved; // Durumu "Onaylandı" olarak güncelle
                _context.SaveChanges(); // Değişiklikleri veritabanına kaydet
            }

            // Onay bekleyen etkinlikler sayfasına geri dön
            return RedirectToAction("Index");
        }


        //REDDETME
        [HttpPost]
        public IActionResult RejectEvent(int eventId)
        {
            // Etkinliği ID'ye göre bul
            var eventItem = _context.Etkinlikler.FirstOrDefault(e => e.Id == eventId);
            if (eventItem != null)
            {
                _context.Etkinlikler.Remove(eventItem); // Etkinliği sil
                _context.SaveChanges(); // Değişiklikleri veritabanına kaydet 
            }

            // Onay bekleyen etkinlikler sayfasına geri dön
            return RedirectToAction("Index");
        }


        //ADMİN USER KISMI

        public IActionResult Users()
        {
            // Kullanıcı oturum kontrolü
            var userId = HttpContext.Session.GetString("UserId");
            var userRole = HttpContext.Session.GetString("UserRole");

            // Eğer oturum yoksa veya admin değilse giriş sayfasına yönlendir
            if (string.IsNullOrEmpty(userId) || userRole != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }
            // Veritabanından kullanıcı listesini çek
            var users = _context.Kullanicilar.ToList();

            ViewData["Title"] = "Kullanıcı Yönetimi";
            return View(users);
        }
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Kullanicilar.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _context.Kullanicilar.Remove(user);
                _context.SaveChanges();
            }

            return RedirectToAction("Users");
        }


//*------------------------------------------- Kullanıcı Güncellme İşlemi-------------------------------------------------------------------------------------------------*//
        [HttpGet]
        public IActionResult EditUser(int id)
        {
            var user = _context.Kullanicilar.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }
        [HttpPost]
        public IActionResult EditUser(Kullanici updatedUser)
        {
            var user = _context.Kullanicilar.FirstOrDefault(u => u.Id == updatedUser.Id);
            if (user == null)
            {
                return NotFound();
            }

            user.UserName = updatedUser.UserName;
            user.Email = updatedUser.Email;
            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.BirthDate = updatedUser.BirthDate;
            user.Gender = updatedUser.Gender;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Location = updatedUser.Location;
            user.Role = updatedUser.Role; // Rolü güncelle

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Kullanıcı bilgileri başarıyla güncellendi.";
            return RedirectToAction("Users");
        }
//*--------------------------------------Etkinlikler--------------------------------------------------------------------------------------------*//
        public IActionResult Events()
        {
            // Onaylanmış etkinlikleri getir
            var etkinlikler = _context.Etkinlikler
                              .Include(e => e.EventCategories)
                              .ThenInclude(ec => ec.Kategori)
                              .Where(e => e.Status == EventStatus.Approved) // Onaylanmış etkinlikler
                              .ToList();
            return View(etkinlikler);
        }

        [HttpPost]
        public IActionResult DeleteEvent(int id)
        {
            var eventItem = _context.Etkinlikler.FirstOrDefault(e => e.Id == id);
            if (eventItem != null)
            {
                _context.Etkinlikler.Remove(eventItem);
                _context.SaveChanges();
            }
            return RedirectToAction("Events");
        }





        //*------------------------------------------- Etkinlik Güncellme İşlemi-------------------------------------------------------------------------------------------------*//
        [HttpGet]
        public IActionResult EditEvent(int id)
        {
            var etkinlik = _context.Etkinlikler.Include(e => e.EventCategories).FirstOrDefault(e => e.Id == id);
            if (etkinlik == null)
            {
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                return RedirectToAction("Index");
            }

            // Kategorileri yükle
            ViewBag.Categories = _context.Kategoriler.ToList();

            return View(etkinlik);
        }

        [HttpPost]
        public IActionResult EditEvent(Etkinlik model, IFormFile Image, int selectedCategoryId)
        {
            var etkinlik = _context.Etkinlikler.Include(e => e.EventCategories).FirstOrDefault(e => e.Id == model.Id);
            if (etkinlik == null)
            {
                TempData["ErrorMessage"] = "Etkinlik bulunamadı.";
                return RedirectToAction("Index");
            }

            // Etkinlik bilgilerini güncelle
            etkinlik.Name = model.Name;
            etkinlik.Description = model.Description;
            etkinlik.Date = model.Date;
            etkinlik.Duration = model.Duration;
            etkinlik.Location = model.Location;

            // Resim dosyasını güncelle
            if (Image != null && Image.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + "_" + Image.FileName;
                var filePath = Path.Combine("wwwroot/images/", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    Image.CopyTo(stream);
                }

                etkinlik.ImageUrl = "/images/" + fileName;
            }

            // Kategori güncellemesi
            etkinlik.EventCategories.Clear();
            etkinlik.EventCategories.Add(new EventCategory
            {
                EtkinlikId = etkinlik.Id,
                KategoriId = selectedCategoryId
            });

            _context.SaveChanges();

            TempData["SuccessMessage"] = "Etkinlik başarıyla güncellendi.";
            return RedirectToAction("Index");
        }


    }
}
