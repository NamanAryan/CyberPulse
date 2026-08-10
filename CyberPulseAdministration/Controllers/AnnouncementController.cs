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
    public class AnnouncementController : Controller
    {
        private readonly AnnouncementRepository _repo = new AnnouncementRepository();

        // GET: Announcement/Index
        public ActionResult Index()
        {
            try
            {
                var announcements = _repo.GetAll();
                return View(announcements);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = "Error loading announcements: " + ex.Message;
                return View(new List<Announcement>());
            }
        }

        // GET: Announcement/Create
        public ActionResult Create()
        {
            var model = new Announcement
            {
                Date = DateTime.Today,
                IsActive = true
            };
            return View(model);
        }

        // POST: Announcement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Announcement model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _repo.Insert(model);
                TempData["SuccessMessage"] = "Announcement created successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
                return View(model);
            }
        }

        // GET: Announcement/Edit/5
        public ActionResult Edit(int id)
        {
            var model = _repo.GetById(id);
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        // POST: Announcement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Announcement model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _repo.Update(model);
                TempData["SuccessMessage"] = "Announcement updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating: " + ex.Message);
                return View(model);
            }
        }

        // POST: Announcement/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            try
            {
                _repo.Delete(id);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: Announcement/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int id, bool isActive)
        {
            try
            {
                _repo.ToggleActive(id, isActive);
                var currentAnnouncement = _repo.GetById(id);
                string pageTitle = currentAnnouncement != null ? currentAnnouncement.PageTitle : "This announcement";
                string shortDesc = currentAnnouncement != null ? currentAnnouncement.ShortDescription : "";

                System.Diagnostics.Debug.WriteLine(
                    string.Format("MANUAL LOG: Toggled status for '{0}' | Short Desc: '{1}...' | Status: {2}",
                        pageTitle,
                        shortDesc.Substring(0, Math.Min(shortDesc.Length, 50)),
                        isActive ? "ACTIVE" : "INACTIVE"));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to toggle status: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: Announcement/BulkToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BulkToggleActive(List<int> ids, bool isActive)
        {
            try
            {
                if (ids != null)
                {
                    foreach (var id in ids)
                    {
                        _repo.ToggleActive(id, isActive);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BulkToggleActive error: " + ex.Message);
            }
            return Json(new { success = true });
        }

        // POST: Announcement/GenerateJson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateJson()
        {
            try
            {
                var activeList = _repo.GetActive() ?? new List<Announcement>();
                if (activeList.Count == 0)
                {
                    TempData["ErrorMessage"] = "No announcements selected. Please select at least one announcement.";
                    return RedirectToAction("Index");
                }

                var json = JsonConvert.SerializeObject(activeList, Formatting.Indented);

                var folderPath = Server.MapPath("~/JsonOutput");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, "announcements.json");
                System.IO.File.WriteAllText(filePath, json);

                TempData["SuccessMessage"] = "JSON export succeeded! File saved to: " + filePath;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "JSON export failed: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // GET: Announcement/GetAnnouncements
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetAnnouncements()
        {
            var activeList = _repo.GetActive() ?? new List<Announcement>();
            return Json(activeList, JsonRequestBehavior.AllowGet);
        }
    }
}
