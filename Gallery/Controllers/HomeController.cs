using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Drawing;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace Gallery.Controllers
{
    public class HomeController : Controller
    {
        public static string title;
        public static string manufacturer;
        public static string modelOfCamera;
        public static string fileSize;
        public static string dateCreation;
        public static string dateUpload;

        //
        // Hash-Function
        // Input: String
        // Otput: String with ShaHash
        //
        public static string ComputeSha256Hash(string rawData)
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
        public bool Equality(Bitmap Bmp1, Bitmap Bmp2)
        {
            if (Bmp1.Size == Bmp2.Size)
            {
                for (int i = 0; i < Bmp1.Width; i++)
                    for (int j = 0; j < Bmp1.Height; j++)
                        if (Bmp1.GetPixel(i, j) != Bmp2.GetPixel(i, j)) 
                            return false;
                return true;
            }
            return false;
        }

       


        public static void LoadExifData(string LoadExifPath)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(LoadExifPath);
                FileStream fs = new FileStream(LoadExifPath, FileMode.Open);

                BitmapSource img = BitmapFrame.Create(fs);
                BitmapMetadata md = (BitmapMetadata)img.Metadata;

                
                //
                //title from FileInfo
                if (string.IsNullOrEmpty(fileInfo.Name))
                    title = "Data not found";
                else
                    title = fileInfo.Name;


                //
                //manufacturer from EXIF
                if (string.IsNullOrEmpty(md.CameraManufacturer))
                    manufacturer = "Data not found";
                else
                    manufacturer = md.CameraManufacturer;


                //
                //modelOfCamera from EXIF
                if (string.IsNullOrEmpty(md.CameraModel))
                    modelOfCamera = "Data not found";
                else
                    modelOfCamera = md.CameraModel;


                //
                //FileSize from FileInfo
                if (fileInfo.Length >= 1024)
                {
                    fileSize = Math.Round((fileInfo.Length / 1024f), 1).ToString() + " KB";
                    if ((fileInfo.Length / 1024f) >= 1024f)
                        fileSize = Math.Round((fileInfo.Length / 1024f) / 1024f, 2).ToString() + " MB";
                }
                else
                    fileSize = fileInfo.Length.ToString() + " B";


                //
                //DateUpload from FileInfo
                if (fileInfo.CreationTime == null)
                    dateUpload = "Data not found";
                else
                    dateUpload = fileInfo.CreationTime.ToString("dd.MM.yyyy HH:mm:ss");


                //
                //DateCreation from EXIF
                if (string.IsNullOrEmpty(md.DateTaken))
                    dateCreation = "Data not found";
                else
                    dateCreation = md.DateTaken;
                fs.Close();
            }
            catch(Exception err)
            {
               
                // need to static errors
            }
       
            
        }
        //Picture picture = new Picture();
        //[HttpGet]

        
        [HttpGet]
        public ActionResult Delete(string T = "")
        {
            try
            {
                if (T.Replace("/Content/Images/", "").Replace(Path.GetFileName(T), "").Replace("/", "") == ComputeSha256Hash(User.Identity.Name))
                {
                    if (T != "" && Directory.Exists(Server.MapPath(T.Replace(Path.GetFileName(T), ""))))
                        System.IO.File.Delete(Server.MapPath(T));
                    else
                    {
                        ViewBag.Error = "File not found!";
                        return View("Error");
                    }
                }
                else
                {
                    ViewBag.Error = "Authorisation Error!";
                    return View("Error");
                }
            }
            catch(Exception err)
            {
                ViewBag.Error = "Unexpected error: " + err.Message;
                return View("Error");
            }
            return RedirectToAction("Index");
        }


        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Error()
        {
            return View();
        }


        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase files)
        {
            try
            {
                if (files != null)
                {
                    if (!string.IsNullOrEmpty(User.Identity.Name))
                    {
                        if (files.ContentType == "image/jpeg")
                        {
                            FileStream TempFileStream;
                            // Verify that the user selected a file and User is logged in
                            if (files.ContentLength > 0)
                            {
                                bool IsLoad = true;

                                // Encrypted User's directory path
                                string DirPath = Server.MapPath("~/Content/Images/") + ComputeSha256Hash(User.Identity.Name);

                                // extract only the filename
                                var fileName = Path.GetFileName(files.FileName);
                                // store the file inside ~/Content/Temp folder
                                var TempPath = Path.Combine(Server.MapPath("~/Content/Temp"), fileName);
                                files.SaveAs(TempPath);
                                TempFileStream = new FileStream(TempPath, FileMode.Open);
                                BitmapSource img = BitmapFrame.Create(TempFileStream);
                                BitmapMetadata md = (BitmapMetadata)img.Metadata;
                                var DateTaken = md.DateTaken;
                                TempFileStream.Close();

                                if (!string.IsNullOrEmpty(DateTaken))
                                {
                                    if (Convert.ToDateTime(DateTaken) >= DateTime.Now.AddYears(-1))
                                    {
                                        TempFileStream = new FileStream(TempPath, FileMode.Open);
                                        Bitmap TempBmp = new Bitmap(TempFileStream);
                                        TempBmp = new Bitmap(TempBmp, 64, 64);
                                        TempFileStream.Close();

                                        // List of all Directories names
                                        List<string> dirsname = Directory.GetDirectories(Server.MapPath("~/Content/Images/")).ToList<string>();

                                        FileStream CheckFileStream;
                                        Bitmap CheckBmp;

                                        List<string> filesname;

                                        // foreach inside foreach in order to check a new photo for its copies in all folders of all users
                                        foreach (string dir in dirsname)
                                        {
                                            filesname = Directory.GetFiles(dir).ToList<string>();
                                            foreach (string fl in filesname)
                                            {
                                                CheckFileStream = new FileStream(fl, FileMode.Open);
                                                CheckBmp = new Bitmap(CheckFileStream);
                                                CheckBmp = new Bitmap(CheckBmp, 64, 64);

                                                CheckFileStream.Close();

                                                if (Equality(TempBmp, CheckBmp))
                                                {
                                                    IsLoad = false;
                                                    ViewBag.Error = "Photo already exists!";
                                                    CheckBmp.Dispose();
                                                    break;
                                                }
                                                else
                                                    CheckBmp.Dispose();
                                            }
                                        }
                                    }
                                    else
                                    {
                                        ViewBag.Error = "Photo created more than a year ago!";
                                        IsLoad = false;
                                    }
                                }
                                else
                                {
                                    ViewBag.Error = "Photo creation date not found!";
                                    IsLoad = false;
                                }

                                if (IsLoad)
                                {
                                    // extract only the filename
                                    var OriginalFileName = Path.GetFileName(files.FileName);
                                    // store the file inside User's folder
                                    var OriginalPath = Path.Combine(DirPath, OriginalFileName);
                                    //System.Windows.MessageBox.Show(OriginalPath);
                                    files.SaveAs(OriginalPath);
                                    System.IO.File.Delete(TempPath);
                                }
                                else
                                {
                                    System.IO.File.Delete(TempPath);
                                    return View("Error");
                                }

                            }
                            else
                            {
                                ViewBag.Error = "File too small!";
                                return View("Error");
                            }
                            // redirect back to the index action to show the form once again

                        }
                        else
                        {
                            ViewBag.Error = "Inappropriate format!";
                            return View("Error");
                        }
                    }
                    else
                    {
                        ViewBag.Error = "Log in please!";
                        return View("Error");
                    }
                }
                else
                {
                    //System.Windows.MessageBox.Show("pusto");
                    return View();
                }
            }
            catch (Exception err)
            {

                //ViewBag.Error = "Unexpected error: " + err.Message;
                //return View("Error");

            }
            return RedirectToAction("Index");
        }

        public ActionResult Upload()
        {
            return View();
        }

    }
}