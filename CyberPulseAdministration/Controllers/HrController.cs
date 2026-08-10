using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CyberPulseAdministration.DataAccess;
using CyberPulseAdministration.Models;

namespace CyberPulseAdministration.Controllers
{
    [Authorize]
    public class HrController : Controller
    {
        private readonly HrFileRepository _fileRepo = new HrFileRepository();
        private readonly HrAnnouncementRepository _announcementRepo = new HrAnnouncementRepository();

        private void LoadHrViewData()
        {
            try
            {
                ViewBag.Files = _fileRepo.GetAll();
                ViewBag.Announcements = _announcementRepo.GetAll();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading HR files or announcements: " + ex.Message;
                ViewBag.Files = new System.Collections.Generic.List<HrFile>();
                ViewBag.Announcements = new System.Collections.Generic.List<HrAnnouncement>();
            }
        }

        // GET: Hr
        public ActionResult Index()
        {
            return RedirectToAction("Documents");
        }

        // GET: Hr/Documents
        public ActionResult Documents()
        {
            ViewBag.ActiveMainTab = "Documents";
            LoadHrViewData();
            return View("Index", ViewBag.Files);
        }

        // GET: Hr/Announcements
        public ActionResult Announcements()
        {
            ViewBag.ActiveMainTab = "Announcements";
            LoadHrViewData();
            return View("Index", ViewBag.Files);
        }

        // POST: Hr/Upload
        [HttpPost]
        public ActionResult Upload(HttpPostedFileBase[] files)
        {
            try
            {
                // Support both model-bound files and Request.Files
                int fileCount = (files != null && files.Length > 0) ? files.Length : Request.Files.Count;
                
                if (fileCount == 0)
                {
                    if (Request.IsAjaxRequest())
                        return Json(new { success = false, message = "No files selected." });
                    TempData["ErrorMessage"] = "No files selected.";
                    return RedirectToAction("Documents");
                }

                int successCount = 0;
                var errors = new System.Collections.Generic.List<string>();

                for (int i = 0; i < fileCount; i++)
                {
                    HttpPostedFileBase file = null;
                    if (files != null && i < files.Length && files[i] != null && files[i].ContentLength > 0)
                        file = files[i];
                    else if (i < Request.Files.Count)
                        file = Request.Files[i];
                    
                    if (file == null || string.IsNullOrEmpty(file.FileName)) continue;
                    
                    if (file.ContentLength == 0)
                    {
                        errors.Add($"File is empty (0 bytes): {file.FileName}");
                        continue;
                    }

                    string extension = Path.GetExtension(file.FileName).ToLower();
                    string fileType = "";

                    // Auto-detect category
                    if (extension == ".pdf") fileType = "pdf";
                    else if (extension == ".doc" || extension == ".docx") fileType = "word";
                    else if (extension == ".xls" || extension == ".xlsx" || extension == ".csv") fileType = "excel";
                    else if (extension == ".txt") fileType = "txt";
                    else if (extension == ".mp4" || extension == ".mov" || extension == ".avi" || extension == ".mkv") fileType = "video";
                    else if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".gif" || extension == ".webp") fileType = "photo";
                    
                    if (string.IsNullOrEmpty(fileType))
                    {
                        errors.Add($"Invalid format ({extension}): {file.FileName}");
                        continue;
                    }

                    // File Size Limits validation (5MB for general files, 25MB for videos)
                    long maxBytes = fileType == "video" ? 25L * 1024 * 1024 : 5L * 1024 * 1024;
                    if (file.ContentLength > maxBytes)
                    {
                        errors.Add($"File too large: {file.FileName}");
                        continue;
                    }

                    string title = Path.GetFileNameWithoutExtension(file.FileName);
                    string targetDir = Path.Combine(Server.MapPath("~/Uploads/HrPortal"), fileType);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    string originalName = Path.GetFileName(file.FileName);
                    string uniqueName = Guid.NewGuid().ToString() + "_" + originalName;
                    string physicalPath = Path.Combine(targetDir, uniqueName);
                    
                    file.SaveAs(physicalPath);

                    var hrFile = new HrFile
                    {
                        Title = title,
                        FileName = originalName,
                        FilePath = $"/Uploads/HrPortal/{fileType}/{uniqueName}",
                        FileType = fileType,
                        FileSize = file.ContentLength,
                        UploadDate = DateTime.Now
                    };

                    _fileRepo.Insert(hrFile);
                    successCount++;
                }

                string message = errors.Count > 0
                    ? (successCount > 0 ? $"Uploaded {successCount} files. Errors: {string.Join(", ", errors)}" : string.Join(", ", errors))
                    : $"{successCount} file(s) uploaded successfully!";

                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = successCount > 0, message = message });
                }

                TempData[errors.Count > 0 ? "ErrorMessage" : "SuccessMessage"] = message;
                return RedirectToAction("Documents");
            }
            catch (Exception ex)
            {
                if (Request.IsAjaxRequest())
                    return Json(new { success = false, message = "An error occurred: " + ex.Message });
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return RedirectToAction("Documents");
            }
        }

        // POST: Hr/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                var file = _fileRepo.GetById(id);
                if (file != null)
                {
                    // 1. Delete physical file
                    string physicalPath = Server.MapPath("~" + file.FilePath);
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }

                    // 2. Delete DB record
                    _fileRepo.Delete(id);
                    TempData["SuccessMessage"] = "File deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "File not found.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting file: " + ex.Message;
            }
            return RedirectToAction("Documents");
        }

        // GET: Hr/Download/5
        public ActionResult Download(int id)
        {
            var file = _fileRepo.GetById(id);
            if (file == null)
            {
                return HttpNotFound("File record not found in the portal.");
            }

            string physicalPath = Server.MapPath("~" + file.FilePath);
            if (!System.IO.File.Exists(physicalPath))
            {
                return HttpNotFound("Physical file is missing from the server storage.");
            }

            // Determine mime type
            string mimeType = MimeMapping.GetMimeMapping(file.FileName);
            
            // Return file stream for download or inline viewing
            return File(physicalPath, mimeType, file.FileName);
        }

        // GET: Hr/GetFiles
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetFiles()
        {
            var files = _fileRepo.GetAll();
            return Json(files, JsonRequestBehavior.AllowGet);
        }
    }
}
