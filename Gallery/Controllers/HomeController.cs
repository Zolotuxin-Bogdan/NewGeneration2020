using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;


namespace Gallery.Controllers
{
    public class HomeController : Controller
    {
        //
        // Hash-Function
        // Input: String
        // Otput: String with ShaHash
        //
        static string ComputeSha256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        //
        // check for equality of pictures
        // Input: Bitmap1, Bitmap2
        // Output:
        //        true - is equal
        //        false - isn't equal
        //
        bool Equality(Bitmap Bmp1, Bitmap Bmp2)
        {
            if (Bmp1.Size == Bmp2.Size)
            {
                for (int i = 0; i < Bmp1.Width; i++)
                    for (int j = 0; j < Bmp1.Height; j++)
                        if (Bmp1.GetPixel(i, j) != Bmp2.GetPixel(i, j)) return false;
                return true;
            }
            else return false;
        }

        
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }
        [HttpPost]
        public ActionResult About(HttpPostedFileBase files)
        {
            
            if (files != null && files.ContentType == "image/jpeg")
            {
                // Verify that the user selected a file and User is logged in
                if (files.ContentLength > 0 && User.Identity.Name != "")
                {
                    // Directory path with all User's directories
                    string ImagesDirPath = Server.MapPath("~/Content/Images");

                    // Encrypted User's directory path
                    string DirPath = Server.MapPath("~/Content/Images/") + ComputeSha256Hash(User.Identity.Name);

                    // Directory path with temp files
                    string TempDirPath = Server.MapPath("~/Content/Temp");

                    if (!Directory.Exists(ImagesDirPath))
                    {
                        Directory.CreateDirectory(ImagesDirPath);
                    }

                    if (!Directory.Exists(TempDirPath))
                    {
                        Directory.CreateDirectory(TempDirPath);
                    }

                    if (!Directory.Exists(DirPath))
                    {
                        Directory.CreateDirectory(DirPath);
                    }

                    if (Directory.Exists(DirPath) && Directory.Exists(TempDirPath))
                    {
                        // extract only the filename
                        var fileName = Path.GetFileName(files.FileName);
                        // store the file inside ~/Content/Temp folder
                        var TempPath = Path.Combine(Server.MapPath("~/Content/Temp"), fileName);
                        files.SaveAs(TempPath);
                        FileStream TempFileStream = new FileStream(TempPath, FileMode.Open);
                        Bitmap TempBmp = new Bitmap(TempFileStream);

                        // List of all Directories names
                        List<string> dirsname = Directory.GetDirectories(Server.MapPath("~/Content/Images/")).ToList<string>();

                        FileStream CheckFileStream;
                        Bitmap CheckBmp;

                        bool IsLoad = true;
                        List<string> filesname;

                        // foreach inside foreach in order to check a new photo for its copies in all folders of all users
                        foreach (string dir in dirsname)
                        {
                            filesname = Directory.GetFiles(dir).ToList<string>();
                            foreach (string fl in filesname)
                            {
                                CheckFileStream = new FileStream(fl, FileMode.Open);
                                CheckBmp = new Bitmap(CheckFileStream);
                                if(Equality(TempBmp,CheckBmp))
                                {
                                    IsLoad = false;
                                    break;
                                }
                                CheckFileStream.Close();
                                CheckBmp.Dispose();
                            }
                        }


                        if(IsLoad)
                        { 
                            // extract only the filename
                            var OriginalFileName = Path.GetFileName(files.FileName);
                            // store the file inside User's folder
                            var OriginalPath = Path.Combine(DirPath, OriginalFileName);
                            files.SaveAs(OriginalPath);
                        }
                        TempFileStream.Close();
                        System.IO.File.Delete(TempPath);
                    }
                }
                // redirect back to the index action to show the form once again
                return RedirectToAction("Index");
            }
            else
            {
                // need error message

                // need error message

                return RedirectToAction("Index");
            }

        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}