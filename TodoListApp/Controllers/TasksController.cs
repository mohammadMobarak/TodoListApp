using Microsoft.AspNetCore.Mvc;
using TodoListApp.Data;
using TodoListApp.Models;
using Microsoft.EntityFrameworkCore;

namespace TodoListApp.Controllers
{
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public TasksController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Tasks
        public async Task<IActionResult> Index(string? timeFilter, int pageNumber = 1)
        {
            int pageSize = 5;
            var query = _context.TaskItems.AsQueryable();

            var now = DateTime.Now;
            if (timeFilter == "upcoming")
            {
                query = query.Where(t => t.Deadline > now);
            }
            else if (timeFilter == "overdue")
            {
                query = query.Where(t => t.Deadline < now);
            }
            else if (timeFilter == "urgent")
            {
                query = query.Where(t => t.Deadline >= now && t.Deadline <= now.AddHours(24));
            }

            query = query.OrderBy(t => t.Deadline);

            int totalTasks = await query.CountAsync();
            var tasks = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentFilter = timeFilter;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling(totalTasks / (double)pageSize);

            return View(tasks);
        }

        // GET: Tasks/DownloadFile/5
        public IActionResult DownloadFile(int id)
        {
            var task = _context.TaskItems.Find(id);
            if (task == null || string.IsNullOrEmpty(task.FilePath))
            {
                return NotFound();
            }

            string filePath = Path.Combine(_webHostEnvironment.WebRootPath, task.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound();
            }

            var mimeType = "application/octet-stream";
            return PhysicalFile(filePath, mimeType, task.OriginalFileName ?? "download");
        }

        // GET: Tasks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tasks/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskItem taskItem, IFormFile? uploadedFile)
        {
            if (ModelState.IsValid)
            {
                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + uploadedFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream);
                    }

                    taskItem.FilePath = "/uploads/" + uniqueFileName;
                    taskItem.OriginalFileName = uploadedFile.FileName;
                }

                _context.Add(taskItem);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(taskItem);
        }

        // GET: Tasks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem == null) return NotFound();

            return View(taskItem);
        }

        // POST: Tasks/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskItem taskItem, IFormFile? uploadedFile)
        {
            if (id != taskItem.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTask = await _context.TaskItems.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
                    if (existingTask == null) return NotFound();

                    if (uploadedFile != null && uploadedFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + uploadedFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await uploadedFile.CopyToAsync(fileStream);
                        }

                        taskItem.FilePath = "/uploads/" + uniqueFileName;
                        taskItem.OriginalFileName = uploadedFile.FileName;
                    }
                    else
                    {
                        // Keep existing file if no new one was uploaded
                        taskItem.FilePath = existingTask.FilePath;
                        taskItem.OriginalFileName = existingTask.OriginalFileName;
                    }

                    _context.Update(taskItem);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.TaskItems.Any(e => e.Id == taskItem.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(taskItem);
        }

        // GET: Tasks/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var taskItem = await _context.TaskItems.FindAsync(id);
            if (taskItem != null)
            {
                // Delete physical file from server if it exists
                if (!string.IsNullOrEmpty(taskItem.FilePath))
                {
                    string filePath = Path.Combine(_webHostEnvironment.WebRootPath, taskItem.FilePath.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.TaskItems.Remove(taskItem);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}