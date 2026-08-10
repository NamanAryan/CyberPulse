using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Mvc;
using CyberPulseAdministration.DataAccess;
using CyberPulseAdministration.Models;
using Newtonsoft.Json;

namespace CyberPulseAdministration.Controllers
{
    [Authorize]
    public class HrAnnouncementController : Controller
    {
        private readonly HrAnnouncementRepository _repo = new HrAnnouncementRepository();

        // GET: HrAnnouncement/Create
        public ActionResult Create()
        {
            var model = new HrAnnouncement
            {
                Date = DateTime.Today,
                IsActive = true
            };
            return View(model);
        }

        // POST: HrAnnouncement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(HrAnnouncement model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _repo.Insert(model);
                TempData["SuccessMessage"] = "HR Announcement created successfully.";
                return RedirectToAction("Announcements", "Hr");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
                return View(model);
            }
        }

        // GET: HrAnnouncement/Edit/5
        public ActionResult Edit(int id)
        {
            var model = _repo.GetById(id);
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        // POST: HrAnnouncement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(HrAnnouncement model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _repo.Update(model);
                TempData["SuccessMessage"] = "HR Announcement updated successfully.";
                return RedirectToAction("Announcements", "Hr");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating: " + ex.Message);
                return View(model);
            }
        }

        // POST: HrAnnouncement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                _repo.Delete(id);
                TempData["SuccessMessage"] = "HR Announcement deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting HR announcement: " + ex.Message;
            }
            return RedirectToAction("Announcements", "Hr");
        }

        // POST: HrAnnouncement/BulkToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkToggleActive(List<int> ids, bool isActive)
        {
            if (ids != null && ids.Count > 0)
            {
                try
                {
                    foreach (var id in ids)
                    {
                        _repo.ToggleActive(id, isActive);
                    }
                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "No records selected." });
        }

        // POST: HrAnnouncement/GenerateJson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateJson()
        {
            try
            {
                var activeAnnouncements = _repo.GetActive() ?? new List<HrAnnouncement>();
                if (activeAnnouncements.Count == 0)
                {
                    TempData["ErrorMessage"] = "No HR announcements selected. Please select at least one announcement.";
                    return RedirectToAction("Announcements", "Hr");
                }
                
                string jsonOutput = JsonConvert.SerializeObject(activeAnnouncements, Formatting.Indented);
                
                // Save JSON to a specific directory (create if doesn't exist)
                string folderPath = Server.MapPath("~/JsonOutput");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                
                string filePath = Path.Combine(folderPath, "hrannouncements.json");
                System.IO.File.WriteAllText(filePath, jsonOutput);

                TempData["SuccessMessage"] = "JSON export succeeded! File saved to: " + filePath;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error generating JSON: " + ex.Message;
            }

            return RedirectToAction("Announcements", "Hr");
        }
    }
}
