using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly Context _context;

        public HomeController(Context context)
        {
            _context = context;
        }

        // Giriþ Sayfasý
        public IActionResult Index()
        {
            // Kullanýcý giriþ yapmamýþsa

            if (HttpContext.Session.GetString("UserId") == null)
            {
                return View(); // Giriþ ve kayýt formlarýný göster
            }

            // Kullanýcý giriþ yapmýþsa
            return RedirectToAction("Login"); // Kullanýcý paneline yönlendir
        }

 //*---------------------------------- Giriþ Ýþlemi ------------------------------------------------------------------------------------------------*//
        [HttpPost]
        public IActionResult Login(string UserName, string Password)
        {
            

            var user = _context.Kullanicilar.FirstOrDefault(u => u.UserName == UserName && u.Password == HashPassword(Password));
            if (user != null)
            {

                // Kullanýcýnýn rolüne göre yönlendirme
                if (user.Role == RoleType.Admin)
                {
                    HttpContext.Session.SetString("UserId", user.Id.ToString());
                    HttpContext.Session.SetString("UserName", user.UserName);
                    HttpContext.Session.SetString("UserRole", "Admin");
                    return RedirectToAction("Index", "Admin");
                }


                // Claims ekleyin
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim("Location", user.Location ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Kullanýcýyý oturum açtýrýn



                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("UserProfilePicture", user.ProfilePicture ?? "~/images/profile.png");
                ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "Misafir Kullanýcý";
                ViewData["UserProfilePicture"] = HttpContext.Session.GetString("UserProfilePicture") ?? "~/images/profile.png";




                var hasInterests = _context.KullaniciKategorileri.Any(kc => kc.KullaniciId == user.Id);
                if (!hasInterests)
                {
                    return RedirectToAction("SelectInterests", "User");
                }

                return RedirectToAction("Index", "User");
            }

            TempData["ErrorMessage"] = "Kullanýcý adý veya þifre yanlýþ.";
            return RedirectToAction("Index");
        }


 //*------------------------------------------------------ Kayýt Olma Ýþlemi ------------------------------------------------------------------------------------------------*//
        [HttpPost]
        public IActionResult Register(string UserName, string Email, string Password, string FirstName, string LastName, DateTime BirthDate, string Location, string Gender, string PhoneNumber, IFormFile Image)
        {
            // Kullanýcý adý veya e-posta kontrolü
            var existingUser = _context.Kullanicilar.FirstOrDefault(u => u.UserName == UserName || u.Email == Email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Kullanýcý Adý veya E-posta zaten mevcut.";
                return RedirectToAction("Index"); // Giriþ sayfasýna yönlendir
            }

            string imageUrl = "/images/default-profile.png";
            if (Image != null && Image.Length > 0)
            {
                try
                {
                    var fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(Image.FileName);
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        Image.CopyTo(stream);
                    }

                    imageUrl = "/images/" + fileName; // Resim URL'si
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Resim yüklenirken hata oluþtu: {ex.Message}";
                }
            }

            // Yeni Kullanýcý oluþtur
            var newUser = new Kullanici
            {
                UserName = UserName,
                Email = Email,
                Password = HashPassword(Password), // Þifreyi hashle
                FirstName = FirstName,
                LastName = LastName,
                BirthDate = BirthDate,
                Location = Location,
                Gender = Gender,
                PhoneNumber = PhoneNumber,
                ProfilePicture = imageUrl // Profil resmi varsa kaydet
            };

            // Veritabanýna ekle
            _context.Kullanicilar.Add(newUser);
            _context.SaveChanges();

            // Kayýt baþarý mesajý
            TempData["SuccessMessage"] = "Kayýt baþarýyla tamamlandý. Giriþ yapabilirsiniz.";
            return RedirectToAction("Index"); // Giriþ sayfasýna yönlendir
        }

        // Kullanýcý Paneli
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Index"); // Oturum yoksa giriþ sayfasýna yönlendir
            }

            return View(); // Kullanýcý panelini döndür
        }

        // Admin Paneli
        public IActionResult AdminDashboard()
        {
            if (HttpContext.Session.GetString("UserId") != "admin")
            {
                return RedirectToAction("Index"); // Admin deðilse giriþ sayfasýna yönlendir
            }

            return View(); // Admin panelini döndür
        }

        // Çýkýþ Ýþlemi
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Oturumu temizle
            TempData["SuccessMessage"] = "Oturum Kapatýldý";
            return RedirectToAction("Index"); // Giriþ sayfasýna yönlendir
        }



//*---------------------------------- Þifre Unuttum Ýþlemi ------------------------------------------------------------------------------------------------*//

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            // Kullanýcýyý kontrol et
            var user = _context.Kullanicilar.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Bu e-posta adresi sistemde bulunamadý.";
                return View();
            }

            // Doðrulama kodu oluþtur
            string verificationCode = GenerateVerificationCode();

            HttpContext.Session.SetString("Email", email);
            HttpContext.Session.SetString("VerificationCode", verificationCode);

            // Doðrulama kodunu bir yere kaydet (veritabanýnda veya geçici bir saklama yerinde)
            TempData["VerificationCode"] = verificationCode;
            TempData["Email"] = email;



            // Doðrulama kodunu e-posta gönder
            SendEmail(user.Email, "Þifre Sýfýrlama Doðrulama Kodu", $"Doðrulama kodunuz: {verificationCode}");

            return RedirectToAction("VerifyCode");
        }

        private string GenerateVerificationCode()
        {
            return new Random().Next(100000, 999999).ToString(); // 6 haneli kod oluþtur
        }

        private void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var emailSettings = new
                {
                    SMTPHost = "smtp.gmail.com",
                    SMTPPort = 587,
                    SMTPUser = "hamzaefe149@gmail.com",
                    SMTPPass = "obnd kdko gacv erlh",
                    FromEmail = "hamzaefe149@gmail.com",
                    FromName = "EtkinlikPlatform"
                };

                using (var client = new SmtpClient(emailSettings.SMTPHost, emailSettings.SMTPPort))
                {
                    client.Credentials = new NetworkCredential(emailSettings.SMTPUser, emailSettings.SMTPPass);
                    client.EnableSsl = true;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(emailSettings.FromEmail, emailSettings.FromName),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(toEmail);

                    client.Send(mailMessage);
                    Console.WriteLine($"E-posta {toEmail} adresine baþarýyla gönderildi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-posta gönderim hatasý: {ex.Message}");
            }
        }
        //doðrulma kodu

        [HttpGet]
        public IActionResult VerifyCode()
        {
            var email = HttpContext.Session.GetString("Email"); // veya TempData["Email"]
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Doðrulama süresi doldu. Lütfen tekrar deneyin.";
                return RedirectToAction("ForgotPassword");
            }

            ViewData["Email"] = email; // View'e email deðerini gönder
            return View();
        }

       
        [HttpPost]
        public IActionResult VerifyCode(string email, string verificationCode, string newPassword)
        {
            // Session deðerlerini kontrol et
            var storedCode = TempData["VerificationCode"]?.ToString();
            var storedEmail = TempData["Email"]?.ToString();

            if (storedCode == null || storedEmail == null)
            {
                ViewBag.Error = "Doðrulama süresi doldu. Lütfen tekrar deneyin.";
                return RedirectToAction("ForgotPassword");
            }
            Console.WriteLine(storedCode);
            Console.WriteLine(storedEmail);
            if (storedCode != verificationCode || storedEmail != email)
            {
                ViewBag.Error = "Doðrulama kodu yanlýþ.";
                return View(); // VerifyCode sayfasýný tekrar göster
            }

            // Kullanýcýnýn þifresini güncelle
            var user = _context.Kullanicilar.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                user.Password = HashPassword(newPassword);
                _context.SaveChanges();

                TempData["Message"] = "Þifreniz baþarýyla sýfýrlandý. Yeni þifrenizle giriþ yapabilirsiniz.";
                return RedirectToAction("Index"); // Giriþ sayfasýna yönlendir
            }

            ViewBag.Error = "Bir hata oluþtu. Lütfen tekrar deneyin.";
            return View();
        }





        // Þifre Hashleme Yardýmcý Fonksiyonu
        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }
}
