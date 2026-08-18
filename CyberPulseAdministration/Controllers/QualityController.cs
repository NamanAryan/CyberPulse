using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CyberPulseAdministration.DataAccess;
using CyberPulseAdministration.Models;

namespace CyberPulseAdministration.Controllers
{
    // Re-using QualityFile class from old QualityInsideController
    // It's already defined there, but if we delete QualityInsideController, we should define it here.
    public class QualityFile
    {
        public string FileName { get; set; }
        public DateTime UploadDate { get; set; }
        public long FileSize { get; set; }
    }

    [Authorize]
    public class QualityController : Controller
    {
        private readonly QualityAnnouncementRepository _announcementRepo = new QualityAnnouncementRepository();

        // Every document sub-tab folder lives under this single parent folder.
        private const string DocumentsRoot = "QualityDocuments";

        // Sub-tab that only accepts PDF or image uploads.
        private const string CertificateTab = "QualityCertificate";
        private static readonly string[] CertificateAllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };

        private string GetUploadPath(string tabPath)
        {
            return Server.MapPath($"~/Uploads/QualityInside/{DocumentsRoot}/{tabPath}");
        }

        private List<QualityFile> GetFiles(string tabPath)
        {
            var list = new List<QualityFile>();
            try
            {
                var path = GetUploadPath(tabPath);
                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    foreach (var file in dirInfo.GetFiles())
                    {
                        list.Add(new QualityFile
                        {
                            FileName = file.Name,
                            UploadDate = file.CreationTime,
                            FileSize = file.Length
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading files for {tabPath}: {ex.Message}");
            }
            return list.OrderByDescending(f => f.UploadDate).ToList();
        }

        private void LoadQualityViewData()
        {
            try
            {
                // Documents Data
                ViewBag.ManualFiles = GetFiles("QualityManual");
                ViewBag.SystemFiles = GetFiles("QualitySystemProcedure");

                ViewBag.QualityStandardFiles = GetFiles("QualityStandardGuidelines/QualityStandard");
                ViewBag.GuidelinesFiles = GetFiles("QualityStandardGuidelines/Guidelines");

                ViewBag.TemplateFiles = GetFiles("TemplateFormChecklist/Template");
                ViewBag.FormFiles = GetFiles("TemplateFormChecklist/Form");
                ViewBag.ChecklistFiles = GetFiles("TemplateFormChecklist/Checklist");

                ViewBag.CertificateFiles = GetFiles(CertificateTab);

                // Announcements Data
                ViewBag.Announcements = _announcementRepo.GetAll();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading files or announcements: " + ex.Message;
                ViewBag.Announcements = new List<QualityAnnouncement>();
            }
        }

        // GET: Quality
        public ActionResult Index()
        {
            return RedirectToAction("Documents");
        }

        // GET: Quality/Documents
        public ActionResult Documents()
        {
            ViewBag.ActiveMainTab = "Documents";
            LoadQualityViewData();
            return View("Index");
        }

        // GET: Quality/Announcements
        public ActionResult Announcements()
        {
            ViewBag.ActiveMainTab = "Announcements";
            LoadQualityViewData();
            return View("Index");
        }

        // GET: Quality/Certificate
        public ActionResult Certificate()
        {
            ViewBag.ActiveMainTab = "Certificate";
            LoadQualityViewData();
            return View("Index");
        }

        // Uploads and deletes return to the main tab the file belongs to.
        private ActionResult RedirectToOwningTab(string tab)
        {
            var tabRoot = string.IsNullOrEmpty(tab) ? string.Empty : tab.Split('/')[0];
            return RedirectToAction(tabRoot == CertificateTab ? "Certificate" : "Documents");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(IEnumerable<HttpPostedFileBase> files, string tab)
        {
            try
            {
                if (files != null && files.Any(f => f != null && f.ContentLength > 0) && !string.IsNullOrEmpty(tab))
                {
                    long maxFileSize = 5 * 1024 * 1024; // 5 MB
                    if (files.Any(f => f != null && f.ContentLength > maxFileSize))
                    {
                        TempData["ErrorMessage"] = "One or more files exceed the maximum allowed size of 5MB.";
                        return RedirectToOwningTab(tab);
                    }

                    var tabRoot = tab.Split('/')[0];
                    TempData["ActiveTab"] = tabRoot == "QualityStandardGuidelines" ? "QualityStandardGuidelines"
                        : tabRoot == "TemplateFormChecklist" ? "TemplateFormChecklist"
                        : tabRoot;

                    if (tabRoot == CertificateTab)
                    {
                        var rejected = files
                            .Where(f => f != null && f.ContentLength > 0
                                && !CertificateAllowedExtensions.Contains(Path.GetExtension(f.FileName ?? string.Empty).ToLowerInvariant()))
                            .Select(f => Path.GetFileName(f.FileName))
                            .ToList();

                        if (rejected.Any())
                        {
                            TempData["ErrorMessage"] = "Only PDF or image files (.pdf, .jpg, .jpeg, .png) can be uploaded to Quality Certificate. Rejected: " + string.Join(", ", rejected);
                            return RedirectToOwningTab(tab);
                        }
                    }

                    string path = GetUploadPath(tab);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    
                    int successCount = 0;
                    foreach (var file in files)
                    {
                        if (file != null && file.ContentLength > 0)
                        {
                            string originalName = Path.GetFileName(file.FileName);
                            string uniqueName = Guid.NewGuid().ToString() + "_" + originalName;
                            
                            file.SaveAs(Path.Combine(path, uniqueName));
                            successCount++;
                        }
                    }
                    TempData["SuccessMessage"] = $"{successCount} file(s) uploaded successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "No files selected.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error uploading file: " + ex.Message;
            }

            return RedirectToOwningTab(tab);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string tab, string fileName)
        {
            try
            {
                if (!string.IsNullOrEmpty(tab) && !string.IsNullOrEmpty(fileName))
                {
                    var tabRoot = tab.Split('/')[0];
                    TempData["ActiveTab"] = tabRoot == "QualityStandardGuidelines" ? "QualityStandardGuidelines"
                        : tabRoot == "TemplateFormChecklist" ? "TemplateFormChecklist"
                        : tabRoot;
                    string path = Path.Combine(GetUploadPath(tab), fileName);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                        TempData["SuccessMessage"] = "File deleted successfully!";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting file: " + ex.Message;
            }
            return RedirectToOwningTab(tab);
        }

        public ActionResult Download(string tab, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(tab) || string.IsNullOrEmpty(fileName))
                    return HttpNotFound();

                string path = Path.Combine(GetUploadPath(tab), fileName);
                if (!System.IO.File.Exists(path))
                    return HttpNotFound();

                string mimeType = MimeMapping.GetMimeMapping(fileName);

                var underscoreIdx = fileName.IndexOf('_');
                var downloadName = (underscoreIdx >= 0 && underscoreIdx == 36)
                    ? fileName.Substring(37)
                    : fileName;

                return File(path, mimeType, downloadName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Quality Download Error: " + ex.Message);
                return HttpNotFound("File could not be downloaded.");
            }
        }
    }
}
