using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;

namespace WebApplication1.Controllers
{
    public class UserController : Controller
    {
        private readonly Context _context;

        public UserController(Context context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));
            var userName = HttpContext.Session.GetString("UserName") ?? "Kullanıcı";
            ViewData["UserName"] = userName;
            ViewData["UserProfilePicture"] = HttpContext.Session.GetString("UserProfilePicture") ?? "~/images/profile.png";

            // İLGİ ALANI VE GEÇMİŞ KATILDIĞI ETKİNLİKLER 
            var userInterests = _context.KullaniciKategorileri
                                        .Where(kc => kc.KullaniciId == userId)
                                        .Select(kc => kc.KategoriId)
                                        .ToList();

            var pastEventCategories = _context.Katilimlar
                                              .Where(k => k.KullaniciId == userId)
                                              .SelectMany(k => k.Etkinlik.EventCategories)
                                              .Select(ec => ec.KategoriId)
                                              .Distinct()
                                              .ToList();

            var combinedCategories = userInterests.Concat(pastEventCategories).Distinct().ToList();

            var interestAndPastBasedEvents = _context.EtkinlikKategorileri
                                                     .Where(ek => combinedCategories.Contains(ek.KategoriId) &&
                                                                  ek.Etkinlik.Status == EventStatus.Approved &&
                                                                  ek.Etkinlik.Date >= DateTime.Now)
                                                     .Select(ek => ek.Etkinlik)
                                                     .Distinct()
                                                     .Include(e => e.Participants)
                                                     .ToList();

            // Konum bazlı etkinlikler
            var userLocation = _context.Kullanicilar
                              .Where(u => u.Id == userId)
                              .Select(u => u.Location)
                              .FirstOrDefault();

            var locationBasedEvents = new List<Etkinlik>();
            if (!string.IsNullOrEmpty(userLocation))
            {
                foreach (var etkinlik in _context.Etkinlikler
                                                 .Where(e => e.Status == EventStatus.Approved &&
                                                             e.Date >= DateTime.Now)
                                                 .Include(e => e.Participants))
                {
                    if (!string.IsNullOrEmpty(etkinlik.Location))
                    {
                        var (distance, _) = await CalculateDistanceAndDuration(userLocation, etkinlik.Location, "AIzaSyBRXYjVWKwDEIRF1bA9JeCLsI78Nc9ycrk");
                        if (distance <= 50) // 50 km mesafedeki etkinlikler
                        {
                            locationBasedEvents.Add(etkinlik);
                        }
                    }
                }
            }

            // Tüm onaylanmış etkinlikler
            var allEvents = _context.Etkinlikler
                                    .Where(e => e.Status == EventStatus.Approved && e.Date >= DateTime.Now)
                                    .Include(e => e.Participants)
                                    .ToList();

            var viewModel = Tuple.Create(interestAndPastBasedEvents, locationBasedEvents, allEvents);

            return View(viewModel);
        }



        //*----------------------------------GOOGLE API MESAFE HESAPLMA -------------------------------------------------------------------------------------*//


        private async Task<(double Distance, double Duration)> CalculateDistanceAndDuration(string origin, string destination, string apiKey)
        {
            var client = new HttpClient();
            var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={Uri.EscapeDataString(origin)}&destinations={Uri.EscapeDataString(destination)}&key={apiKey}";

            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Google API isteği başarısız oldu: " + response.ReasonPhrase);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            // Google API Hata Kontrolü
            if (root.TryGetProperty("status", out var status) && status.GetString() != "OK")
            {
                throw new Exception("Google API isteği başarısız oldu: " + status.GetString());
            }

            if (root.TryGetProperty("rows", out var rows) && rows.GetArrayLength() > 0)
            {
                var elements = rows[0].GetProperty("elements");

                if (elements.GetArrayLength() > 0)
                {
                    var element = elements[0];
                    var distance = element.GetProperty("distance").GetProperty("value").GetDouble() / 1000.0; // Metreden km'ye çevir
                    var duration = element.GetProperty("duration").GetProperty("value").GetDouble() / 60.0;  // Saniyeden dakikaya çevir

                    return (distance, duration);
                }
            }

            throw new Exception("Google API yanıtı beklenen formatta değil.");
        }






















        // İlgi Alanı Ekleme
        public IActionResult SelectInterests()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var categories = _context.Kategoriler.ToList();
            return View(categories);
        }

        [HttpPost]
        public IActionResult SaveInterests(int[] selectedCategories)
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId"));

            var existingInterests = _context.KullaniciKategorileri.Where(kc => kc.KullaniciId == userId);
            _context.KullaniciKategorileri.RemoveRange(existingInterests);

            foreach (var categoryId in selectedCategories)
            {
                _context.KullaniciKategorileri.Add(new UserInterest
                {
                    KullaniciId = userId,
                    KategoriId = categoryId
                });
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "İlgi alanlarınız başarıyla kaydedildi.";
            return RedirectToAction("Index", "User");
        }

        // Etkinliklerim Kısmı
        public IActionResult MyEvent()
        {

            ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "Misafir Kullanıcı";
            ViewData["UserProfilePicture"] = HttpContext.Session.GetString("UserProfilePicture") ?? "~/images/profile.png";


            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var userId = int.Parse(HttpContext.Session.GetString("UserId"));

            // Kullanıcının oluşturduğu etkinlikler
            var createdEvents = _context.Etkinlikler
                                        .Where(e => e.CreatedByUserId == userId)
                                        .Include(e => e.Participants)
                                        .ToList();

            // Kullanıcının katıldığı etkinlikler
            var participatedEvents = _context.Katilimlar
                                             .Include(k => k.Etkinlik) // Etkinlik verilerini dahil et
                                             .ThenInclude(e => e.Participants) // Etkinlik katılımcılarını dahil et
                                             .Where(p => p.KullaniciId == userId)
                                             .Select(p => p.Etkinlik)
                                             .ToList();


            return View(Tuple.Create<IEnumerable<Etkinlik>, IEnumerable<Etkinlik>>(createdEvents, participatedEvents));
        }


        //*---------------------------------------------------------- PUAN TABLOSU--------------------------------------------------------------------------------------*//
        public IActionResult Leaderboard()
        {
            var leaderboard = _context.Puanlar
                                      .GroupBy(s => s.KullaniciId)
                                      .Select(g => new
                                      {
                                          KullaniciId = g.Key,
                                          TotalScore = g.Sum(s => s.Value),
                                          Kullanici = _context.Kullanicilar.FirstOrDefault(k => k.Id == g.Key)
                                      })
                                      .OrderByDescending(x => x.TotalScore)
                                      .ToList();

            return View(leaderboard);
        }


        //Profil Kısmı
        public IActionResult Profile()
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Lütfen giriş yapınız.";
                return RedirectToAction("Index", "Home");
            }

            var user = _context.Kullanicilar.Include(u => u.Scores).FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            // Kullanıcının toplam puanını hesapla
            var totalScore = _context.Puanlar.Where(s => s.KullaniciId == userId).Sum(s => s.Value);

            ViewData["TotalScore"] = totalScore;
            return View(user);
        }


        //İLGİLERİ GÜNCELLEME
        [HttpPost]
        public IActionResult UpdateProfile(Kullanici updatedUser, IFormFile profileImage)
        {
            var userId = int.Parse(HttpContext.Session.GetString("UserId") ?? "0");

            if (userId == 0)
            {
                TempData["ErrorMessage"] = "Lütfen giriş yapınız.";
                return RedirectToAction("Index", "Home");
            }

            var user = _context.Kullanicilar.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Profile");
            }

            // Bilgileri güncelle
            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.BirthDate = updatedUser.BirthDate;
            user.Gender = updatedUser.Gender;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Location = updatedUser.Location;

            if (profileImage != null && profileImage.Length > 0)
            {
                try
                {
                    var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profileImage.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);



                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        profileImage.CopyTo(stream);
                    }

                    user.ProfilePicture = "/images/" + fileName; // Resim URL'si
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Resim yüklenirken hata oluştu: {ex.Message}";
                }
            }


            _context.SaveChanges();
            TempData["SuccessMessage"] = "Profil başarıyla güncellendi.";
            return RedirectToAction("Profile");
        }
    }
}
