using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.Mvc;
using CyberPulseAdministration.DataAccess;
using CyberPulseAdministration.Models;
using Newtonsoft.Json;

namespace CyberPulseAdministration.Controllers
{
    [Authorize]
    public class NewsArticleController : Controller
    {
        private readonly NewsArticleRepository _repo = new NewsArticleRepository();

        // GET: NewsArticle/Index
        public ActionResult Index()
        {
            var articles = _repo.GetAll();
            return View(articles);
        }

        // GET: NewsArticle/Create
        public ActionResult Create()
        {
            var model = new NewsArticle
            {
                Date = DateTime.Today,
                IsActive = true
            };
            return View(model);
        }

        // POST: NewsArticle/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(NewsArticle model, HttpPostedFileBase ImageFile)
        {
            if (ImageFile == null || ImageFile.ContentLength == 0)
            {
                ModelState.AddModelError("ImageFile", "An image is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var uploadsFolder = Server.MapPath("~/Uploads/NewsArticles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    ImageFile.SaveAs(filePath);

                    model.ImageName = Path.GetFileNameWithoutExtension(fileName);
                    model.ImagePath = "/" + fileName;
                }

                _repo.Insert(model);
                TempData["SuccessMessage"] = "News article created successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while saving: " + ex.Message);
                return View(model);
            }
        }

        // GET: NewsArticle/Edit/5
        public ActionResult Edit(int id)
        {
            var model = _repo.GetById(id);
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        // POST: NewsArticle/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(NewsArticle model, HttpPostedFileBase ImageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                if (ImageFile != null && ImageFile.ContentLength > 0)
                {
                    var uploadsFolder = Server.MapPath("~/Uploads/NewsArticles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var fileName = Path.GetFileName(ImageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    ImageFile.SaveAs(filePath);

                    model.ImageName = Path.GetFileNameWithoutExtension(fileName);
                    model.ImagePath = "/" + fileName;
                }

                _repo.Update(model);
                TempData["SuccessMessage"] = "News article updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An error occurred while updating: " + ex.Message);
                return View(model);
            }
        }

        // POST: NewsArticle/Delete/5
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

        // POST: NewsArticle/ToggleActive
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleActive(int id, bool isActive)
        {
            try
            {
                _repo.ToggleActive(id, isActive);
                var current = _repo.GetById(id);
                string desc = current != null ? current.Description : "";

                System.Diagnostics.Debug.WriteLine(
                    string.Format("MANUAL LOG: Toggled news article '{0}' | Desc: '{1}...' | Status: {2}",
                        current != null ? current.ImageName : "Unknown",
                        desc != null ? desc.Substring(0, Math.Min(desc.Length, 50)) : "",
                        isActive ? "ACTIVE" : "INACTIVE"));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to toggle status: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // POST: NewsArticle/BulkToggleActive
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

        // POST: NewsArticle/GenerateJson
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GenerateJson()
        {
            try
            {
                var activeList = _repo.GetActive() ?? new List<NewsArticle>();
                if (activeList.Count == 0)
                {
                    TempData["ErrorMessage"] = "No news articles selected. Please select at least one article.";
                    return RedirectToAction("Index");
                }

                var json = JsonConvert.SerializeObject(activeList, Formatting.Indented);

                var folderPath = Server.MapPath("~/JsonOutput");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var filePath = Path.Combine(folderPath, "newsarticles.json");
                System.IO.File.WriteAllText(filePath, json);

                TempData["SuccessMessage"] = "JSON export succeeded! File saved to: " + filePath;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "JSON export failed: " + ex.Message;
            }
            return RedirectToAction("Index");
        }

        // GET: NewsArticle/GetArticles
        [HttpGet]
        [AllowAnonymous]
        public JsonResult GetArticles()
        {
            var activeList = _repo.GetActive() ?? new List<NewsArticle>();
            return Json(activeList, JsonRequestBehavior.AllowGet);
        }
    }
}
