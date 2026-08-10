using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Xml;

namespace CyberPulseAdministration.Controllers
{
    public class HealthTipController : Controller
    {
        private string HealthTipFolder
        {
            get { return Server.MapPath("~/Uploads/HealthTip"); }
        }

        // GET: HealthTip
        [Authorize]
        public ActionResult Index()
        {
            try
            {
                EnsureFolderExists();
                var currentFile = GetCurrentFile();
                
                ViewBag.CurrentFile = currentFile;
                if (!string.IsNullOrEmpty(currentFile))
                {
                    string physicalPath = Path.Combine(HealthTipFolder, currentFile);
                    ViewBag.IsImage = HealthTipHelper.IsImageFile(currentFile);
                    ViewBag.TextContent = HealthTipHelper.GetTextContent(physicalPath);
                }
                else
                {
                    ViewBag.IsImage = false;
                    ViewBag.TextContent = null;
                }
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading Health Tip: " + ex.Message;
            }
            
            return View();
        }

        // POST: HealthTip/Upload
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(HttpPostedFileBase imageFile)
        {
            try
            {
                if (imageFile == null || imageFile.ContentLength == 0)
                {
                    TempData["ErrorMessage"] = "Please select a file to upload.";
                    return RedirectToAction("Index");
                }

                string extension = Path.GetExtension(imageFile.FileName).ToLower();
                string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".doc", ".docx", ".txt" };

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Invalid file format. Only image files (PNG, JPG, etc.) and document files (DOC, DOCX, TXT) are allowed. Excel and other formats are blocked.";
                    return RedirectToAction("Index");
                }

                // 5 MB limit
                if (imageFile.ContentLength > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File size exceeds 5 MB limit.";
                    return RedirectToAction("Index");
                }

                EnsureFolderExists();

                // Delete all existing files in the folder first
                DeleteAllFilesInFolder();

                // Save new file with original name
                string fileName = Path.GetFileName(imageFile.FileName);
                string physicalPath = Path.Combine(HealthTipFolder, fileName);
                imageFile.SaveAs(physicalPath);

                TempData["SuccessMessage"] = "Health Tip uploaded successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error uploading file: " + ex.Message;
            }

            return RedirectToAction("Index");
        }

        // GET: HealthTip/GetImage (Public API for client viewer)
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetImage()
        {
            try
            {
                EnsureFolderExists();
                var currentFile = GetCurrentFile();

                if (currentFile != null)
                {
                    string physicalPath = Path.Combine(HealthTipFolder, currentFile);
                    return Json(new 
                    { 
                        exists = true, 
                        filePath = "/Uploads/HealthTip/" + currentFile,
                        isImage = HealthTipHelper.IsImageFile(currentFile),
                        textContent = HealthTipHelper.GetTextContent(physicalPath)
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("HealthTip API Error: " + ex.Message);
            }

            return Json(new { exists = false, filePath = (string)null }, JsonRequestBehavior.AllowGet);
        }

        // --- Helpers ---

        private void EnsureFolderExists()
        {
            if (!Directory.Exists(HealthTipFolder))
            {
                Directory.CreateDirectory(HealthTipFolder);
            }
        }

        private void DeleteAllFilesInFolder()
        {
            if (Directory.Exists(HealthTipFolder))
            {
                foreach (var file in Directory.GetFiles(HealthTipFolder))
                {
                    System.IO.File.Delete(file);
                }
            }
        }

        private string GetCurrentFile()
        {
            if (!Directory.Exists(HealthTipFolder)) return null;

            var files = Directory.GetFiles(HealthTipFolder);
            if (files.Length == 0) return null;

            return Path.GetFileName(files[0]);
        }
    }

    /// <summary>
    /// Helper class to extract text from documents and identify images, 
    /// keeping the controller lightweight.
    /// </summary>
    public static class HealthTipHelper
    {
        public static bool IsImageFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return false;
            string ext = Path.GetExtension(fileName).ToLower();
            string[] imageExts = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            return imageExts.Contains(ext);
        }

        public static string GetTextContent(string physicalPath)
        {
            if (string.IsNullOrEmpty(physicalPath)) return null;
            string fileName = Path.GetFileName(physicalPath);

            if (IsImageFile(fileName) || !File.Exists(physicalPath)) 
                return null;

            string ext = Path.GetExtension(fileName).ToLower();

            try
            {
                if (ext == ".txt")
                {
                    return File.ReadAllText(physicalPath);
                }
                else if (ext == ".docx")
                {
                    return ExtractTextFromDocx(physicalPath);
                }
                else if (ext == ".doc")
                {
                    return ExtractTextFromDoc(physicalPath);
                }
            }
            catch (Exception)
            {
                return "Unable to extract text content.";
            }

            return "Unsupported text format.";
        }

        private static string ExtractTextFromDocx(string path)
        {
            try
            {
                using (ZipArchive archive = ZipFile.OpenRead(path))
                {
                    ZipArchiveEntry entry = archive.GetEntry("word/document.xml");
                    if (entry == null) return "No text found.";

                    using (Stream stream = entry.Open())
                    {
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.Load(stream);
                        XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                        nsmgr.AddNamespace("w", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
                        var nodes = xmlDoc.SelectNodes("//w:t", nsmgr);
                        
                        StringBuilder sb = new StringBuilder();
                        foreach (XmlNode node in nodes)
                        {
                            sb.Append(node.InnerText + " ");
                        }
                        return sb.ToString().Trim();
                    }
                }
            }
            catch
            {
                return "Could not read .docx file.";
            }
        }

        private static string ExtractTextFromDoc(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] >= 32 && bytes[i] <= 126)
                    {
                        sb.Append((char)bytes[i]);
                    }
                }
                string text = sb.ToString();
                var matches = Regex.Matches(text, @"[A-Za-z0-9\s]{4,}");
                StringBuilder result = new StringBuilder();
                foreach (Match match in matches)
                {
                    result.Append(match.Value + " ");
                }
                string extracted = result.ToString().Trim();
                return string.IsNullOrEmpty(extracted) ? "No readable text found." : extracted.Substring(0, Math.Min(extracted.Length, 500)) + "...";
            }
            catch
            {
                return "Could not read .doc file.";
            }
        }
    }
}
