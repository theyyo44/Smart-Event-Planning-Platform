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

        // Giri� Sayfas�
        public IActionResult Index()
        {
            // Kullan�c� giri� yapmam��sa

            if (HttpContext.Session.GetString("UserId") == null)
            {
                return View(); // Giri� ve kay�t formlar�n� g�ster
            }

            // Kullan�c� giri� yapm��sa
            return RedirectToAction("Login"); // Kullan�c� paneline y�nlendir
        }

 //*---------------------------------- Giri� ��lemi ------------------------------------------------------------------------------------------------*//
        [HttpPost]
        public IActionResult Login(string UserName, string Password)
        {
            

            var user = _context.Kullanicilar.FirstOrDefault(u => u.UserName == UserName && u.Password == HashPassword(Password));
            if (user != null)
            {

                // Kullan�c�n�n rol�ne g�re y�nlendirme
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

                // Kullan�c�y� oturum a�t�r�n



                HttpContext.Session.SetString("UserId", user.Id.ToString());
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("UserProfilePicture", user.ProfilePicture ?? "~/images/profile.png");
                ViewData["UserName"] = HttpContext.Session.GetString("UserName") ?? "Misafir Kullan�c�";
                ViewData["UserProfilePicture"] = HttpContext.Session.GetString("UserProfilePicture") ?? "~/images/profile.png";




                var hasInterests = _context.KullaniciKategorileri.Any(kc => kc.KullaniciId == user.Id);
                if (!hasInterests)
                {
                    return RedirectToAction("SelectInterests", "User");
                }

                return RedirectToAction("Index", "User");
            }

            TempData["ErrorMessage"] = "Kullan�c� ad� veya �ifre yanl��.";
            return RedirectToAction("Index");
        }


 //*------------------------------------------------------ Kay�t Olma ��lemi ------------------------------------------------------------------------------------------------*//
        [HttpPost]
        public IActionResult Register(string UserName, string Email, string Password, string FirstName, string LastName, DateTime BirthDate, string Location, string Gender, string PhoneNumber, IFormFile Image)
        {
            // Kullan�c� ad� veya e-posta kontrol�
            var existingUser = _context.Kullanicilar.FirstOrDefault(u => u.UserName == UserName || u.Email == Email);
            if (existingUser != null)
            {
                TempData["ErrorMessage"] = "Kullan�c� Ad� veya E-posta zaten mevcut.";
                return RedirectToAction("Index"); // Giri� sayfas�na y�nlendir
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
                    TempData["ErrorMessage"] = $"Resim y�klenirken hata olu�tu: {ex.Message}";
                }
            }

            // Yeni Kullan�c� olu�tur
            var newUser = new Kullanici
            {
                UserName = UserName,
                Email = Email,
                Password = HashPassword(Password), // �ifreyi hashle
                FirstName = FirstName,
                LastName = LastName,
                BirthDate = BirthDate,
                Location = Location,
                Gender = Gender,
                PhoneNumber = PhoneNumber,
                ProfilePicture = imageUrl // Profil resmi varsa kaydet
            };

            // Veritaban�na ekle
            _context.Kullanicilar.Add(newUser);
            _context.SaveChanges();

            // Kay�t ba�ar� mesaj�
            TempData["SuccessMessage"] = "Kay�t ba�ar�yla tamamland�. Giri� yapabilirsiniz.";
            return RedirectToAction("Index"); // Giri� sayfas�na y�nlendir
        }

        // Kullan�c� Paneli
        public IActionResult Dashboard()
        {
            if (HttpContext.Session.GetString("UserId") == null)
            {
                return RedirectToAction("Index"); // Oturum yoksa giri� sayfas�na y�nlendir
            }

            return View(); // Kullan�c� panelini d�nd�r
        }

        // Admin Paneli
        public IActionResult AdminDashboard()
        {
            if (HttpContext.Session.GetString("UserId") != "admin")
            {
                return RedirectToAction("Index"); // Admin de�ilse giri� sayfas�na y�nlendir
            }

            return View(); // Admin panelini d�nd�r
        }

        // ��k�� ��lemi
        public IActionResult Logout()
        {
            HttpContext.Session.Clear(); // Oturumu temizle
            TempData["SuccessMessage"] = "Oturum Kapat�ld�";
            return RedirectToAction("Index"); // Giri� sayfas�na y�nlendir
        }



//*---------------------------------- �ifre Unuttum ��lemi ------------------------------------------------------------------------------------------------*//

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            // Kullan�c�y� kontrol et
            var user = _context.Kullanicilar.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ViewBag.Error = "Bu e-posta adresi sistemde bulunamad�.";
                return View();
            }

            // Do�rulama kodu olu�tur
            string verificationCode = GenerateVerificationCode();

            HttpContext.Session.SetString("Email", email);
            HttpContext.Session.SetString("VerificationCode", verificationCode);

            // Do�rulama kodunu bir yere kaydet (veritaban�nda veya ge�ici bir saklama yerinde)
            TempData["VerificationCode"] = verificationCode;
            TempData["Email"] = email;



            // Do�rulama kodunu e-posta g�nder
            SendEmail(user.Email, "�ifre S�f�rlama Do�rulama Kodu", $"Do�rulama kodunuz: {verificationCode}");

            return RedirectToAction("VerifyCode");
        }

        private string GenerateVerificationCode()
        {
            return new Random().Next(100000, 999999).ToString(); // 6 haneli kod olu�tur
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
                    Console.WriteLine($"E-posta {toEmail} adresine ba�ar�yla g�nderildi.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"E-posta g�nderim hatas�: {ex.Message}");
            }
        }
        //do�rulma kodu

        [HttpGet]
        public IActionResult VerifyCode()
        {
            var email = HttpContext.Session.GetString("Email"); // veya TempData["Email"]
            if (string.IsNullOrEmpty(email))
            {
                TempData["Error"] = "Do�rulama s�resi doldu. L�tfen tekrar deneyin.";
                return RedirectToAction("ForgotPassword");
            }

            ViewData["Email"] = email; // View'e email de�erini g�nder
            return View();
        }

       
        [HttpPost]
        public IActionResult VerifyCode(string email, string verificationCode, string newPassword)
        {
            // Session de�erlerini kontrol et
            var storedCode = TempData["VerificationCode"]?.ToString();
            var storedEmail = TempData["Email"]?.ToString();

            if (storedCode == null || storedEmail == null)
            {
                ViewBag.Error = "Do�rulama s�resi doldu. L�tfen tekrar deneyin.";
                return RedirectToAction("ForgotPassword");
            }
            Console.WriteLine(storedCode);
            Console.WriteLine(storedEmail);
            if (storedCode != verificationCode || storedEmail != email)
            {
                ViewBag.Error = "Do�rulama kodu yanl��.";
                return View(); // VerifyCode sayfas�n� tekrar g�ster
            }

            // Kullan�c�n�n �ifresini g�ncelle
            var user = _context.Kullanicilar.FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                user.Password = HashPassword(newPassword);
                _context.SaveChanges();

                TempData["Message"] = "�ifreniz ba�ar�yla s�f�rland�. Yeni �ifrenizle giri� yapabilirsiniz.";
                return RedirectToAction("Index"); // Giri� sayfas�na y�nlendir
            }

            ViewBag.Error = "Bir hata olu�tu. L�tfen tekrar deneyin.";
            return View();
        }





        // �ifre Hashleme Yard�mc� Fonksiyonu
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
