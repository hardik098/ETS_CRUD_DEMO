using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ETS_CRUD_DEMO.Data;
using ETS_CRUD_DEMO.Models;
using Microsoft.CodeAnalysis.Elfie.Serialization;
using System.Globalization;
using OfficeOpenXml; // For handling Excel files
using CsvHelper;
using CsvReader = CsvHelper.CsvReader;
using CsvWriter = CsvHelper.CsvWriter;
using ETS_CRUD_DEMO.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NPOI.XSSF.UserModel;
using System.Text.RegularExpressions;
using System.ComponentModel.DataAnnotations; // For handling CSV files

namespace ETS_CRUD_DEMO.Controllers
{
    [Authorize]

    public class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public EmployeesController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
        }

        [HttpGet]
        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Employees.Include(e => e.City).Include(e => e.Department).Include(e => e.Role).Include(e => e.State);
            return View(await applicationDbContext.ToListAsync());
        }
        [HttpPost]
        public async Task<IActionResult> GetEmployees([FromForm] DataTableParameters parameters)
        {
            // Base query including related data
            var query = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .Include(e => e.State)
                .Include(e => e.City)
                .Select(emp => new
                {
                    emp.EmployeeId,
                    emp.FirstName,
                    emp.LastName,
                    DOB = emp.DOB,  // Keep as DateTime for sorting
                    Department = emp.Department != null ? emp.Department.DepartmentName : "N/A",
                    Role = emp.Role != null ? emp.Role.RoleName : "N/A",
                    IsActive = emp.IsActive ? "Yes" : "No",
                    State = emp.State != null ? emp.State.StateName : "N/A",
                    City = emp.City != null ? emp.City.CityName : "N/A",
                    JoiningDate = emp.JoiningDate // Keep as DateTime for sorting
                });

            // Apply search filter if search value is present
            if (!string.IsNullOrWhiteSpace(parameters.Search?.Value))
            {
                string searchValue = parameters.Search.Value.ToLower();
                query = query.Where(emp =>
                    emp.FirstName.ToLower().Contains(searchValue) ||
                    emp.LastName.ToLower().Contains(searchValue) ||
                    (emp.Department ?? "").ToLower().Contains(searchValue) ||
                    (emp.Role ?? "").ToLower().Contains(searchValue) ||
                    (emp.State ?? "").ToLower().Contains(searchValue) ||
                    (emp.City ?? "").ToLower().Contains(searchValue)
                );
            }

            // Sorting
            if (parameters.Order.Any())
            {
                var order = parameters.Order.First();
                bool ascending = order.Dir == "asc";

                query = order.Column switch
                {
                    1 => ascending ? query.OrderBy(e => e.FirstName) : query.OrderByDescending(e => e.FirstName),
                    2 => ascending ? query.OrderBy(e => e.LastName) : query.OrderByDescending(e => e.LastName),
                    3 => ascending ? query.OrderBy(e => e.DOB) : query.OrderByDescending(e => e.DOB),
                    4 => ascending ? query.OrderBy(e => e.Department) : query.OrderByDescending(e => e.Department),
                    5 => ascending ? query.OrderBy(e => e.Role) : query.OrderByDescending(e => e.Role),
                    6 => ascending ? query.OrderBy(e => e.IsActive) : query.OrderByDescending(e => e.IsActive),
                    7 => ascending ? query.OrderBy(e => e.State) : query.OrderByDescending(e => e.State),
                    8 => ascending ? query.OrderBy(e => e.City) : query.OrderByDescending(e => e.City),
                    9 => ascending ? query.OrderBy(e => e.JoiningDate) : query.OrderByDescending(e => e.JoiningDate),
                    _ => query // Ignore sorting on EmployeeId if no valid column specified
                };
            }

            // Total record count before pagination
            int recordsTotal = await _context.Employees.CountAsync();

            // Apply pagination
            var data = await query
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToListAsync();

            // Convert date fields to strings in the final data set
            var resultData = data.Select(emp => new
            {
                emp.EmployeeId,
                emp.FirstName,
                emp.LastName,
                DOB = emp.DOB.ToString("dd-MM-yyyy"),  // Format date as string
                Department = emp.Department,
                Role = emp.Role,
                IsActive = emp.IsActive,
                State = emp.State,
                City = emp.City,
                JoiningDate = emp.JoiningDate.ToString("dd-MM-yyyy")  // Format date as string
            });

            // Return data in JSON format expected by DataTables
            return Json(new
            {
                draw = parameters.Draw,
                recordsFiltered = recordsTotal,
                recordsTotal = recordsTotal,
                data = resultData
            });
        }

        public async Task<IActionResult> ExportEmployees()
        {
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .Include(e => e.State)
                .Include(e => e.City)
                .ToListAsync();

            // Project the required fields only
            var exportData = employees.Select(e => new
            {
                e.EmployeeId,
                e.FirstName,
                e.LastName,
                e.Email,
                e.PhoneNumber,
                e.Gender,
                DOB = e.DOB.ToString("yyyy-MM-dd"), // Format date
                Skills = string.Join(", ", e.Skills), // Join skills if Skills is a collection
                Department = e.Department?.DepartmentName,
                Role = e.Role?.RoleName,
                IsActive = e.IsActive ? "Yes" : "No",
                State = e.State?.StateName,
                City = e.City?.CityName,
                JoiningDate = e.JoiningDate.ToString("yyyy-MM-dd")
            }).ToList();

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Employees");

            // Add header row
            worksheet.Cells[1, 1].Value = "EmployeeId";
            worksheet.Cells[1, 2].Value = "FirstName";
            worksheet.Cells[1, 3].Value = "LastName";
            worksheet.Cells[1, 4].Value = "Email";
            worksheet.Cells[1, 5].Value = "PhoneNumber";
            worksheet.Cells[1, 6].Value = "Gender";
            worksheet.Cells[1, 7].Value = "DOB";
            worksheet.Cells[1, 8].Value = "Skills";
            worksheet.Cells[1, 9].Value = "Department";
            worksheet.Cells[1, 10].Value = "Role";
            worksheet.Cells[1, 11].Value = "IsActive";
            worksheet.Cells[1, 12].Value = "State";
            worksheet.Cells[1, 13].Value = "City";
            worksheet.Cells[1, 14].Value = "JoiningDate";

            // Add data rows
            for (int i = 0; i < exportData.Count; i++)
            {
                var data = exportData[i];
                worksheet.Cells[i + 2, 1].Value = data.EmployeeId;
                worksheet.Cells[i + 2, 2].Value = data.FirstName;
                worksheet.Cells[i + 2, 3].Value = data.LastName;
                worksheet.Cells[i + 2, 4].Value = data.Email;
                worksheet.Cells[i + 2, 5].Value = data.PhoneNumber;
                worksheet.Cells[i + 2, 6].Value = data.Gender;
                worksheet.Cells[i + 2, 7].Value = data.DOB;
                worksheet.Cells[i + 2, 8].Value = data.Skills;
                worksheet.Cells[i + 2, 9].Value = data.Department;
                worksheet.Cells[i + 2, 10].Value = data.Role;
                worksheet.Cells[i + 2, 11].Value = data.IsActive;
                worksheet.Cells[i + 2, 12].Value = data.State;
                worksheet.Cells[i + 2, 13].Value = data.City;
                worksheet.Cells[i + 2, 14].Value = data.JoiningDate;
            }

            // Set column width for better readability
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var fileName = $"Employees_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var excelData = package.GetAsByteArray();

            return File(excelData, mimeType, fileName);
        }

        // Action to import employees
        /*[HttpPost]
        public async Task<IActionResult> ImportEmployees(IFormFile file)
        {
            if (file != null && (file.ContentType == "text/csv" || file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                var employees = new List<Employee>();

                if (file.ContentType == "text/csv")
                {
                    // Handle CSV file
                    stream.Position = 0;
                    using var reader = new StreamReader(stream);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                    // Check if the header contains required columns
                    csv.Read();
                    csv.ReadHeader();
                    var headerRow = csv.HeaderRecord;

                    var requiredColumns = new List<string> { "FirstName", "LastName", "Email", "PhoneNumber", "Gender", "DOB", "Skills", "Department", "Role", "IsActive", "State", "City", "JoiningDate" };
                    var missingColumns = requiredColumns.Where(column => !headerRow.Contains(column)).ToList();

                    if (missingColumns.Any())
                    {
                        return BadRequest($"Missing required columns: {string.Join(", ", missingColumns)}.");
                    }

                    while (csv.Read())
                    {
                        var employee = new Employee
                        {
                            EmployeeId = Guid.NewGuid(), // Generate a new ID
                            FirstName = csv.GetField<string>("FirstName"),
                            LastName = csv.GetField<string>("LastName"),
                            Email = csv.GetField<string>("Email"),
                            PhoneNumber = csv.GetField<string>("PhoneNumber"),
                            Gender = Enum.TryParse<GenderOptions>(csv.GetField<string>("Gender"), true, out var gender) ? gender : GenderOptions.Other,
                            DOB = DateTime.Parse(csv.GetField<string>("DOB")),
                            Skills = csv.GetField<string>("Skills")?.Split(';').ToList(),
                            IsActive = bool.Parse(csv.GetField<string>("IsActive")),
                            JoiningDate = DateTime.Parse(csv.GetField<string>("JoiningDate")),
                            // Get IDs for Department, Role, State, and City
                            DepartmentId = await GetDepartmentIdAsync(csv.GetField<string>("Department")),
                            RoleId = await GetRoleIdAsync(csv.GetField<string>("Role")),
                            StateId = await GetStateIdAsync(csv.GetField<string>("State")),
                            CityId = await GetCityIdAsync(csv.GetField<string>("City"))
                        };
                        employees.Add(employee);
                    }

                    // Save employees to the database
                    await _context.Employees.AddRangeAsync(employees);
                    await _context.SaveChangesAsync();
                }
                else if (file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                {
                    // Handle Excel file
                    stream.Position = 0;
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets[0];

                    var requiredColumns = new List<string> { "FirstName", "LastName", "Email", "PhoneNumber", "Gender", "DOB", "Skills", "Department", "Role", "IsActive", "State", "City", "JoiningDate" };

                    // Check if the first row contains required columns
                    var headerRow = new List<string>();
                    for (int col = 1; col <= worksheet.Dimension.End.Column; col++)
                    {
                        headerRow.Add(worksheet.Cells[1, col].Text);
                    }

                    var missingColumns = requiredColumns.Where(column => !headerRow.Contains(column)).ToList();

                    if (missingColumns.Any())
                    {
                        return BadRequest($"Missing required columns: {string.Join(", ", missingColumns)}.");
                    }

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var employee = new Employee
                        {
                            EmployeeId = Guid.NewGuid(), // Generate a new ID
                            FirstName = worksheet.Cells[row, 1].Text,
                            LastName = worksheet.Cells[row, 2].Text,
                            Email = worksheet.Cells[row, 3].Text,
                            PhoneNumber = worksheet.Cells[row, 4].Text,
                            Gender = Enum.TryParse<GenderOptions>(worksheet.Cells[row, 5].Text, true, out var gender) ? gender : GenderOptions.Other,
                            DOB = DateTime.Parse(worksheet.Cells[row, 6].Text),
                            Skills = worksheet.Cells[row, 7].Text?.Split(';').ToList(),
                            IsActive = bool.Parse(worksheet.Cells[row, 10].Text),
                            JoiningDate = DateTime.Parse(worksheet.Cells[row, 13].Text),
                            // Get IDs for Department, Role, State, and City
                            DepartmentId = await GetDepartmentIdAsync(worksheet.Cells[row, 8].Text),
                            RoleId = await GetRoleIdAsync(worksheet.Cells[row, 9].Text),
                            StateId = await GetStateIdAsync(worksheet.Cells[row, 11].Text),
                            CityId = await GetCityIdAsync(worksheet.Cells[row, 12].Text)
                        };
                        employees.Add(employee);
                    }

                    // Save employees to the database
                    await _context.Employees.AddRangeAsync(employees);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            return BadRequest("Invalid file format.");
        }
*/


        // GET: Employees/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.City)
                .Include(e => e.Department)
                .Include(e => e.Role)
                .Include(e => e.State)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }


        // GET: Employee/Create
        public IActionResult Create()
        {
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName");
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName");
            ViewData["StateId"] = new SelectList(_context.States, "StateId", "StateName");
            return View();
        }

        // POST: Employee/Create
        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Create(
           [Bind("FirstName,LastName,Email,PhoneNumber,Gender,DOB,DepartmentId,RoleId,IsActive,StateId,CityId,JoiningDate")]
           Employee employee,
            IFormFile profileImage, List<string> SelectedSkills)
        {
            //if (ModelState.IsValid)
            {
                // Handle profile picture upload
                if (profileImage != null && profileImage.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + profileImage.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImage.CopyToAsync(fileStream);
                    }

                    employee.ProfilePicture = "/uploads/" + uniqueFileName;
                }

                // Handle skills
                if (SelectedSkills != null && SelectedSkills.Count > 0)
                {
                    employee.Skills = SelectedSkills;
                }

                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // If we got this far, something failed, redisplay form
            ViewData["DepartmentId"] = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", employee.DepartmentId);
            ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleName", employee.RoleId);
            ViewData["StateId"] = new SelectList(_context.States, "StateId", "StateName", employee.StateId);
            return View(employee);
        }

        // AJAX endpoint for getting cities by state
        [HttpGet]
        public JsonResult GetCitiesByState(Guid stateId)
        {
            var cities = _context.Cities
                .Where(c => c.StateId == stateId)
                .Select(c => new { value = c.CityId, text = c.CityName })
                .ToList();
            return Json(cities);
        }

        [Authorize(Policy = "CanUpdate")]
        // GET: Employees/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }


            // Populate ViewBags for dropdowns
            ViewBag.DepartmentId = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", employee.DepartmentId);
            ViewBag.RoleId = new SelectList(_context.Roles, "RoleId", "RoleName", employee.RoleId);
            ViewBag.StateId = new SelectList(_context.States, "StateId", "StateName", employee.StateId);
            // If State is selected, populate cities
            if (employee.StateId.HasValue)
            {
                ViewBag.CityId = new SelectList(_context.Cities.Where(c => c.StateId == employee.StateId),
                    "CityId", "CityName", employee.CityId);
            }
            return View(employee);
        }

        // POST: Employees/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanUpdate")]

        public async Task<IActionResult> Edit(Guid id, [Bind("EmployeeId,FirstName,LastName,Email,PhoneNumber,Gender,DOB,DepartmentId,RoleId,IsActive,StateId,CityId,JoiningDate,ProfilePicture")] Employee employee, IFormFile? profileImage, string[] SelectedSkills)
        {
            if (id != employee.EmployeeId)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            {
                try
                {
                    // Get existing employee to check for changes
                    var existingEmployee = await _context.Employees
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.EmployeeId == id);

                    if (existingEmployee == null)
                    {
                        return NotFound();
                    }

                    // Handle profile picture upload
                    if (profileImage != null)
                    {
                        // Delete old image if exists
                        if (!string.IsNullOrEmpty(existingEmployee.ProfilePicture))
                        {
                            var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, existingEmployee.ProfilePicture.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Save new image
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + profileImage.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Create directory if it doesn't exist
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await profileImage.CopyToAsync(fileStream);
                        }

                        employee.ProfilePicture = "/uploads/" + uniqueFileName;
                    }
                    else
                    {
                        // Keep existing profile picture if no new one is uploaded
                        employee.ProfilePicture = existingEmployee.ProfilePicture;
                    }

                    // Handle skills
                    employee.Skills = SelectedSkills?.ToList() ?? new List<string>();

                    // Update the employee
                    _context.Update(employee);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Employee updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error
                    ModelState.AddModelError("", "An error occurred while updating the employee. Please try again." + ex);
                    // Log ex.Message or use proper logging
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.DepartmentId = new SelectList(_context.Departments, "DepartmentId", "DepartmentName", employee.DepartmentId);
            ViewBag.RoleId = new SelectList(_context.Roles, "RoleId", "RoleName", employee.RoleId);
            ViewBag.StateId = new SelectList(_context.States, "StateId", "StateName", employee.StateId);
            if (employee.StateId.HasValue)
            {
                ViewBag.CityId = new SelectList(_context.Cities.Where(c => c.StateId == employee.StateId),
                    "CityId", "CityName", employee.CityId);
            }

            return View(employee);
        }


        [Authorize(Policy = "CanUpdate")]
        // GET: Employees/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employees
                .Include(e => e.City)
                .Include(e => e.Department)
                .Include(e => e.Role)
                .Include(e => e.State)
                .FirstOrDefaultAsync(m => m.EmployeeId == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employees/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "CanUpdate")]


        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            try
            {
                // Delete profile picture if exists
                if (!string.IsNullOrEmpty(employee.ProfilePicture))
                {
                    var imagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        employee.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Employee deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the error
                TempData["ErrorMessage"] = "An error occurred while deleting the employee." + ex;
                return RedirectToAction(nameof(Index));
            }
        }

        private bool EmployeeExists(Guid id)
        {
            return _context.Employees.Any(e => e.EmployeeId == id);
        }

        public async Task<Guid> GetDepartmentIdAsync(string departmentName)
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentName == departmentName);
            return department.DepartmentId;
        }

        public async Task<Guid> GetRoleIdAsync(string roleName)
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName == roleName);
            return role.RoleId;
        }

        public async Task<Guid> GetStateIdAsync(string stateName)
        {
            var state = await _context.States
                .FirstOrDefaultAsync(s => s.StateName == stateName);
            return state.StateId;
        }

        public async Task<Guid> GetCityIdAsync(string cityName)
        {
            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.CityName == cityName);


            if (city == null)
            {
                // Handle the case when city is not found
                Console.WriteLine($"City '{cityName}' not found in database.");

                throw new KeyNotFoundException($"City '{cityName}' not found in database.");
            }
            return city.CityId;
        }


        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || !(file.ContentType == "text/csv" || file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
            {
                var error = "Please upload a valid CSV or Excel file.";
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = error });
                }
                TempData["ImportErrors"] = new List<string> { error };
                return View("ImportErrors");
            }

            var employees = new List<Employee>();
            var errors = new List<string>();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);

            if (file.ContentType == "text/csv")
            {
                // CSV file processing
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                try
                {
                    csv.Read();
                    csv.ReadHeader();
                    var headerRow = csv.HeaderRecord;
                    var requiredColumns = new List<string> { "FirstName", "LastName", "Email", "PhoneNumber", "Gender", "DOB", "Skills", "Department", "Role", "IsActive", "State", "City", "JoiningDate" };
                    var missingColumns = requiredColumns.Where(column => !headerRow.Contains(column)).ToList();

                    if (missingColumns.Any())
                    {
                        var error = $"Missing required columns: {string.Join(", ", missingColumns)}.";
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                        {
                            return Json(new { success = false, message = error });
                        }
                        TempData["ImportErrors"] = new List<string> { error };
                        return View("ImportErrors");
                    }

                    while (csv.Read())
                    {
                        try
                        {
                            var rowNumber = csv.Context.Parser.Row;
                            bool rowIsValid = true;

                            // First Name validation
                            var firstName = csv.GetField<string>("FirstName");
                            if (string.IsNullOrWhiteSpace(firstName))
                            {
                                errors.Add($"Row {rowNumber}, Column FirstName: First Name is required.");
                                rowIsValid = false;
                            }

                            // Last Name validation
                            var lastName = csv.GetField<string>("LastName");
                            if (string.IsNullOrWhiteSpace(lastName))
                            {
                                errors.Add($"Row {rowNumber}, Column LastName: Last Name is required.");
                                rowIsValid = false;
                            }

                            // Email validation
                            var email = csv.GetField<string>("Email");
                            if (!new EmailAddressAttribute().IsValid(email))
                            {
                                errors.Add($"Row {rowNumber}, Column Email: Invalid Email format.");
                                rowIsValid = false;
                            }
                            else
                            {
                                var existingEmployee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
                                if (existingEmployee != null)
                                {
                                    errors.Add($"Row {rowNumber}, Column Email: Email '{email}' already exists in the database.");
                                    rowIsValid = false;
                                }
                            }

                            // Phone Number validation
                            var phoneNumber = csv.GetField<string>("PhoneNumber");
                            if (!Regex.IsMatch(phoneNumber ?? "", @"^\d{10}$"))
                            {
                                errors.Add($"Row {rowNumber}, Column PhoneNumber: Invalid phone number format. Must be 10 digits.");
                                rowIsValid = false;
                            }

                            // Gender validation
                            var genderStr = csv.GetField<string>("Gender")?.ToLower();
                            if (genderStr != "male" && genderStr != "female" && genderStr != "other")
                            {
                                errors.Add($"Row {rowNumber}, Column Gender: Gender must be 'Male', 'Female', or 'Other'.");
                                rowIsValid = false;
                            }

                            // DOB validation
                            var dobStr = csv.GetField<string>("DOB");
                            if (!DateTime.TryParse(dobStr, out DateTime dob))
                            {
                                errors.Add($"Row {rowNumber}, Column DOB: Invalid date format.");
                                rowIsValid = false;
                            }

                            // Department validation
                            var departmentName = csv.GetField<string>("Department");
                            var department = await _context.Departments
                                .FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == departmentName.ToLower());
                            if (department == null)
                            {
                                errors.Add($"Row {rowNumber}, Column Department: Department '{departmentName}' does not exist.");
                                rowIsValid = false;
                            }

                            // Role validation
                            var roleName = csv.GetField<string>("Role");
                            var role = await _context.Roles
                                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.ToLower());
                            if (role == null)
                            {
                                errors.Add($"Row {rowNumber}, Column Role: Role '{roleName}' not found.");
                                rowIsValid = false;
                            }

                            // State validation
                            var stateName = csv.GetField<string>("State");
                            var state = await _context.States
                                .FirstOrDefaultAsync(s => s.StateName.ToLower() == stateName.ToLower());
                            if (state == null)
                            {
                                errors.Add($"Row {rowNumber}, Column State: State '{stateName}' not found.");
                                rowIsValid = false;
                            }

                            // City validation
                            var cityName = csv.GetField<string>("City");
                            var city = await _context.Cities
                                .FirstOrDefaultAsync(c => c.CityName.ToLower() == cityName.ToLower());
                            if (city == null)
                            {
                                errors.Add($"Row {rowNumber}, Column City: City '{cityName}' not found.");
                                rowIsValid = false;
                            }

                            if (rowIsValid)
                            {
                                var employee = new Employee
                                {
                                    EmployeeId = Guid.NewGuid(),
                                    FirstName = firstName,
                                    LastName = lastName,
                                    Email = email,
                                    PhoneNumber = phoneNumber,
                                    Gender = Enum.Parse<GenderOptions>(genderStr, true),
                                    DOB = dob,
                                    Skills = csv.GetField<string>("Skills")?.Split(';').ToList() ?? new List<string>(),
                                    IsActive = csv.GetField<bool>("IsActive"),
                                    JoiningDate = DateTime.Parse(csv.GetField<string>("JoiningDate")),
                                    DepartmentId = department.DepartmentId,
                                    RoleId = role.RoleId,
                                    StateId = state.StateId,
                                    CityId = city.CityId
                                };
                                employees.Add(employee);
                            }
                        }
                        catch (Exception ex)
                        {
                            errors.Add($"Error processing row {csv.Context.Parser.Row}: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing CSV file: {ex.Message}");
                }
            }

            // Similar validation for Excel files...
            else if (file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                // Excel processing with similar validations
                // ... (Similar validation logic for Excel)
            }

            if (errors.Any())
            {
                TempData["ImportErrors"] = errors;
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return View("ImportErrors");
                }
                return View("ImportErrors");
            }

            try
            {
                await _context.Employees.AddRangeAsync(employees);
                await _context.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = true, message = $"Successfully imported {employees.Count} employees." });
                }
                TempData["SuccessMessage"] = $"Successfully imported {employees.Count} employees.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                errors.Add($"Database error: {ex.Message}");
                TempData["ImportErrors"] = errors;

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return View("ImportErrors");
                }
                return View("ImportErrors");
            }
        }
        public IActionResult ExportCsv()
        {
            var employees = _context.Employees.ToList();

            using (var workbook = new XSSFWorkbook())
            {
                var sheet = workbook.CreateSheet("Employees");

                // Create header row
                var headerRow = sheet.CreateRow(0);
                headerRow.CreateCell(0).SetCellValue("EmployeeId");
                headerRow.CreateCell(1).SetCellValue("First Name");
                headerRow.CreateCell(2).SetCellValue("Last Name");
                headerRow.CreateCell(3).SetCellValue("Email");
                headerRow.CreateCell(4).SetCellValue("Phone Number");
                headerRow.CreateCell(5).SetCellValue("Gender");
                headerRow.CreateCell(6).SetCellValue("DOB");
                headerRow.CreateCell(7).SetCellValue("Skills");
                headerRow.CreateCell(8).SetCellValue("DepartmentId");
                headerRow.CreateCell(9).SetCellValue("RoleId");
                headerRow.CreateCell(10).SetCellValue("IsActive");
                headerRow.CreateCell(11).SetCellValue("Profile Picture");
                headerRow.CreateCell(12).SetCellValue("StateId");
                headerRow.CreateCell(13).SetCellValue("CityId");
                headerRow.CreateCell(14).SetCellValue("Joining Date");

                // Populate rows with employee data
                for (int i = 0; i < employees.Count; i++)
                {
                    var row = sheet.CreateRow(i + 1);
                    var emp = employees[i];
                    row.CreateCell(0).SetCellValue(emp.EmployeeId.ToString());
                    row.CreateCell(1).SetCellValue(emp.FirstName);
                    row.CreateCell(2).SetCellValue(emp.LastName);
                    row.CreateCell(3).SetCellValue(emp.Email);
                    row.CreateCell(4).SetCellValue(emp.PhoneNumber);
                    row.CreateCell(5).SetCellValue(emp.Gender.ToString());
                    row.CreateCell(6).SetCellValue(emp.DOB.ToString("yyyy-MM-dd"));
                    row.CreateCell(7).SetCellValue(string.Join(",", emp.Skills));
                    row.CreateCell(8).SetCellValue(emp.DepartmentId.ToString());
                    row.CreateCell(9).SetCellValue(emp.RoleId.ToString());
                    row.CreateCell(10).SetCellValue(emp.IsActive ? "Yes" : "No");
                    row.CreateCell(11).SetCellValue(emp.ProfilePicture);
                    row.CreateCell(12).SetCellValue(emp.StateId?.ToString());
                    row.CreateCell(13).SetCellValue(emp.CityId?.ToString());
                    row.CreateCell(14).SetCellValue(emp.JoiningDate.ToString("yyyy-MM-dd"));
                }

                using (var stream = new MemoryStream())
                {
                    workbook.Write(stream);
                    var content = stream.ToArray();
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Employees.xlsx");
                }
            }
        }
        private DateTime ParseDate(string dateStr, int row, string columnName, List<string> errors)
        {
            if (DateTime.TryParseExact(dateStr, new[] { "yyyy-MM-dd", "MM/dd/yyyy", "dd-MM-yyyy" },
                                       CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }
            errors.Add($"Row {row + 1}, Column {columnName}: Invalid date format.");
            return DateTime.MinValue; // Default value for invalid dates
        }

        private GenderOptions ParseGender(string genderStr, int row, List<string> errors)
        {
            if (Enum.TryParse(typeof(GenderOptions), genderStr, true, out var gender))
            {
                return (GenderOptions)gender;
            }
            errors.Add($"Row {row + 1}, Column Gender: Invalid gender value (allowed: Male, Female, Other).");
            return GenderOptions.Other; // Default value for invalid enums
        }

        private Guid ParseGuid(string guidStr, int row, string columnName, List<string> errors)
        {
            if (Guid.TryParse(guidStr, out var guid))
            {
                return guid;
            }
            errors.Add($"Row {row + 1}, Column {columnName}: Invalid GUID format.");
            return Guid.Empty;
        }

        private Guid? ParseNullableGuid(string guidStr, int row, string columnName, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(guidStr)) return null;
            return Guid.TryParse(guidStr, out var guid) ? guid : (Guid?)null;
        }

        private bool ParseBoolean(string boolStr, int row, string columnName, List<string> errors)
        {
            if (bool.TryParse(boolStr, out var boolValue))
            {
                return boolValue;
            }
            errors.Add($"Row {row + 1}, Column {columnName}: Invalid boolean value (allowed: true, false).");
            return false;
        }

    }
}
