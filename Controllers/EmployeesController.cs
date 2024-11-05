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
using System.ComponentModel;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Text;

namespace ETS_CRUD_DEMO.Controllers
{
    [Authorize]

    public class ImportValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Employee Employee { get; set; }
    }

    public partial class EmployeesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public EmployeesController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        // GET: Employees
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Employees.Include(e => e.City).Include(e => e.Department).Include(e => e.Role).Include(e => e.State);
            return View(await applicationDbContext.ToListAsync());
        }

        /*public async Task<IActionResult> ExportEmployees()
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
        [HttpPost]
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

        public async Task<IActionResult> ExportEmployees()
        {
            // Get employees data with related entities
            var employees = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Role)
                .Include(e => e.State)
                .Include(e => e.City)
                .ToListAsync();

            // Project the required fields
            var exportData = employees.Select(e => new
            {
                e.EmployeeId,
                e.FirstName,
                e.LastName,
                e.Email,
                e.PhoneNumber,
                e.Gender,
                DOB = e.DOB.ToString("yyyy-MM-dd"),
                Skills = string.Join(", ", e.Skills),
                Department = e.Department?.DepartmentName,
                Role = e.Role?.RoleName,
                IsActive = e.IsActive ? "Yes" : "No",
                State = e.State?.StateName,
                City = e.City?.CityName,
                JoiningDate = e.JoiningDate.ToString("yyyy-MM-dd")
            }).ToList();

            // Create new workbook and sheet
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Employees");

            // Create header row with style
            var headerRow = sheet.CreateRow(0);
            var headerStyle = workbook.CreateCellStyle();
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerStyle.SetFont(headerFont);

            // Define headers
            var headers = new[]
            {
        "EmployeeId", "FirstName", "LastName", "Email", "PhoneNumber",
        "Gender", "DOB", "Skills", "Department", "Role", "IsActive",
        "State", "City", "JoiningDate"
    };

            // Add headers with style
            for (var i = 0; i < headers.Length; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(headers[i]);
                cell.CellStyle = headerStyle;
            }

            // Add data rows
            for (var i = 0; i < exportData.Count; i++)
            {
                var row = sheet.CreateRow(i + 1);
                var data = exportData[i];

                row.CreateCell(0).SetCellValue(data.EmployeeId.ToString());
                row.CreateCell(1).SetCellValue(data.FirstName);
                row.CreateCell(2).SetCellValue(data.LastName);
                row.CreateCell(3).SetCellValue(data.Email);
                row.CreateCell(4).SetCellValue(data.PhoneNumber);
                row.CreateCell(5).SetCellValue(data.Gender.ToString());
                row.CreateCell(6).SetCellValue(data.DOB);
                row.CreateCell(7).SetCellValue(data.Skills);
                row.CreateCell(8).SetCellValue(data.Department);
                row.CreateCell(9).SetCellValue(data.Role);
                row.CreateCell(10).SetCellValue(data.IsActive);
                row.CreateCell(11).SetCellValue(data.State);
                row.CreateCell(12).SetCellValue(data.City);
                row.CreateCell(13).SetCellValue(data.JoiningDate);
            }

            // Autosize columns
            for (var i = 0; i < headers.Length; i++)
            {
                sheet.AutoSizeColumn(i);
            }

            // Convert workbook to byte array
            using var memoryStream = new MemoryStream();
            workbook.Write(memoryStream, true);
            var fileBytes = memoryStream.ToArray();

            // Generate filename with timestamp
            var fileName = $"Employees_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }

        [HttpPost]
        public async Task<IActionResult> ImportEmployees(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var validationResults = new List<ImportValidationResult>();
            var successCount = 0;
            var errorCount = 0;

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                IWorkbook workbook;
                if (file.FileName.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook(stream);
                }
                else if (file.FileName.EndsWith(".xls"))
                {
                    workbook = new HSSFWorkbook(stream);
                }
                else
                {
                    return BadRequest("Invalid file format. Please upload an Excel file (.xlsx or .xls)");
                }

                var sheet = workbook.GetSheetAt(0);
                var headerRow = sheet.GetRow(0);

                // Validate header structure
                var headerValidation = ValidateHeaderRow(headerRow);
                if (!headerValidation.IsValid)
                {
                    return BadRequest(string.Join("\n", headerValidation.Errors));
                }

                // Get column indexes
                var columnIndexes = GetColumnIndexes(headerRow);

                // Process each row
                for (int rowNum = 1; rowNum <= sheet.LastRowNum; rowNum++)
                {
                    var row = sheet.GetRow(rowNum);
                    if (row == null) continue;

                    var validationResult = await ValidateAndCreateEmployee(row, columnIndexes, rowNum + 1);
                    validationResults.Add(validationResult);

                    if (validationResult.IsValid)
                    {
                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }

                // If there are any errors, prepare detailed error report
                if (errorCount > 0)
                {
                    var errorMessage = new StringBuilder();
                    errorMessage.AppendLine($"Found {errorCount} errors in the import file:");
                    foreach (var result in validationResults.Where(r => !r.IsValid))
                    {
                        errorMessage.AppendLine(string.Join("\n", result.Errors));
                    }

                    TempData["ErrorMessage"] = errorMessage.ToString();
                    return RedirectToAction(nameof(Index));
                }

                // Save valid employees to database
                var validEmployees = validationResults
                    .Where(r => r.IsValid)
                    .Select(r => r.Employee)
                    .ToList();

                await _context.Employees.AddRangeAsync(validEmployees);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Successfully imported {successCount} employees.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                return BadRequest($"Error processing file: {ex.Message}");
            }
        }

        private (bool IsValid, List<string> Errors) ValidateHeaderRow(IRow headerRow)
        {
            var errors = new List<string>();
            var requiredColumns = new[]
            {
                "FirstName", "LastName", "Email", "PhoneNumber", "Gender",
                "DOB", "Skills", "Department", "Role", "IsActive",
                "State", "City", "JoiningDate"
            };

            var headerColumns = new List<string>();
            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell != null)
                {
                    headerColumns.Add(cell.StringCellValue.Trim());
                }
            }

            foreach (var required in requiredColumns)
            {
                if (!headerColumns.Contains(required))
                {
                    errors.Add($"Missing required column: {required}");
                }
            }

            return (errors.Count == 0, errors);
        }

        private Dictionary<string, int> GetColumnIndexes(IRow headerRow)
        {
            var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); // Make case-insensitive
            for (int i = 0; i < headerRow.LastCellNum; i++)
            {
                var cell = headerRow.GetCell(i);
                if (cell != null)
                {
                    indexes[cell.StringCellValue.Trim()] = i;
                }
            }
            return indexes;
        }

        private async Task<ImportValidationResult> ValidateAndCreateEmployee(
            IRow row,
            Dictionary<string, int> columnIndexes,
            int rowNumber)
        {
            var result = new ImportValidationResult();
            var errors = new List<string>();

            try
            {
                // Basic data extraction
                var firstName = GetCellValueAsString(row.GetCell(columnIndexes["FirstName"]));
                var lastName = GetCellValueAsString(row.GetCell(columnIndexes["LastName"]));
                var email = GetCellValueAsString(row.GetCell(columnIndexes["Email"]));
                var phoneNumber = GetCellValueAsString(row.GetCell(columnIndexes["PhoneNumber"]));
                var genderString = GetCellValueAsString(row.GetCell(columnIndexes["Gender"]));
                var dobCell = row.GetCell(columnIndexes["DOB"]);
                var joiningDateCell = row.GetCell(columnIndexes["JoiningDate"]);
                var skills = GetCellValueAsString(row.GetCell(columnIndexes["Skills"]));
                var department = GetCellValueAsString(row.GetCell(columnIndexes["Department"]));
                var role = GetCellValueAsString(row.GetCell(columnIndexes["Role"]));
                var isActiveString = GetCellValueAsString(row.GetCell(columnIndexes["IsActive"]));
                var state = GetCellValueAsString(row.GetCell(columnIndexes["State"]));
                var city = GetCellValueAsString(row.GetCell(columnIndexes["City"]));

                // Required field validation
                if (string.IsNullOrWhiteSpace(firstName))
                    errors.Add($"Row {rowNumber}: FirstName is required");
                if (string.IsNullOrWhiteSpace(lastName))
                    errors.Add($"Row {rowNumber}: LastName is required");
                if (string.IsNullOrWhiteSpace(email))
                    errors.Add($"Row {rowNumber}: Email is required");

                // Email format validation
                if (!string.IsNullOrWhiteSpace(email) && !IsValidEmail(email))
                    errors.Add($"Row {rowNumber}: Invalid email format");

                // Gender validation (case-insensitive)
                GenderOptions gender;
                if (!Enum.TryParse<GenderOptions>(genderString, true, out gender))
                {
                    errors.Add($"Row {rowNumber}: Invalid gender value. Must be one of: {string.Join(", ", Enum.GetNames<GenderOptions>())} (case-insensitive)");
                }

                // Date validations
                DateTime dob;
                if (!TryGetDateFromCell(dobCell, out dob))
                    errors.Add($"Row {rowNumber}: Invalid DOB format");
                else if (dob > DateTime.Now)
                    errors.Add($"Row {rowNumber}: DOB cannot be in the future");

                DateTime joiningDate;
                if (!TryGetDateFromCell(joiningDateCell, out joiningDate))
                    errors.Add($"Row {rowNumber}: Invalid Joining Date format");
                else if (joiningDate > DateTime.Now)
                    errors.Add($"Row {rowNumber}: Joining Date cannot be in the future");

                // Department validation
                var departmentId = await GetDepartmentIdAsync(department);
                if (departmentId == Guid.Empty)
                    errors.Add($"Row {rowNumber}: Invalid Department");

                // Role validation
                var roleId = await GetRoleIdAsync(role);
                if (roleId == Guid.Empty)
                    errors.Add($"Row {rowNumber}: Invalid Role");

                // State and City validation - allowing null values
                Guid? stateId = null;
                Guid? cityId = null;

                if (!string.IsNullOrWhiteSpace(state))
                {
                    var foundStateId = await GetStateIdAsync(state);
                    if (foundStateId == Guid.Empty)
                        errors.Add($"Row {rowNumber}: Invalid State");
                    else
                        stateId = foundStateId;
                }

                if (!string.IsNullOrWhiteSpace(city))
                {
                    var foundCityId = await GetCityIdAsync(city);
                    if (foundCityId == Guid.Empty)
                        errors.Add($"Row {rowNumber}: Invalid City");
                    else
                        cityId = foundCityId;
                }

                // Create employee if no errors
                if (errors.Count == 0)
                {
                    result.Employee = new Employee
                    {
                        EmployeeId = Guid.NewGuid(),
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        PhoneNumber = phoneNumber,
                        Gender = gender,
                        DOB = dob,
                        Skills = !string.IsNullOrWhiteSpace(skills)
                            ? skills.Split(';').Select(s => s.Trim()).ToList()
                            : new List<string>(),
                        DepartmentId = departmentId,
                        RoleId = roleId,
                        IsActive = string.IsNullOrWhiteSpace(isActiveString) ? false :
                            bool.Parse(isActiveString.Trim()),
                        StateId = stateId,
                        CityId = cityId,
                        JoiningDate = joiningDate
                    };
                    result.IsValid = true;
                }
                else
                {
                    result.IsValid = false;
                    result.Errors = errors;
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Row {rowNumber}: Unexpected error - {ex.Message}");
            }

            return result;
        }

        // Update the helper methods to be case-insensitive
        public async Task<Guid> GetDepartmentIdAsync(string departmentName)
        {
            if (string.IsNullOrWhiteSpace(departmentName))
                return Guid.Empty;

            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.DepartmentName.ToLower() == departmentName.Trim().ToLower());
            return department?.DepartmentId ?? Guid.Empty;
        }

        public async Task<Guid> GetRoleIdAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return Guid.Empty;

            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.RoleName.ToLower() == roleName.Trim().ToLower());
            return role?.RoleId ?? Guid.Empty;
        }

        public async Task<Guid> GetStateIdAsync(string stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName))
                return Guid.Empty;

            var state = await _context.States
                .FirstOrDefaultAsync(s => s.StateName.ToLower() == stateName.Trim().ToLower());
            return state?.StateId ?? Guid.Empty;
        }

        public async Task<Guid> GetCityIdAsync(string cityName)
        {
            if (string.IsNullOrWhiteSpace(cityName))
                return Guid.Empty;

            var city = await _context.Cities
                .FirstOrDefaultAsync(c => c.CityName.ToLower() == cityName.Trim().ToLower());
            return city?.CityId ?? Guid.Empty;
        }

        private string GetCellValueAsString(ICell cell)
        {
            if (cell == null) return string.Empty;

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue?.Trim() ?? string.Empty;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                        return cell.DateCellValue.ToString();   // ToString("yyyy-MM-dd");
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                default:
                    return string.Empty;
            }
        }
        private bool TryGetDateFromCell(ICell cell, out DateTime result)
        {
            result = DateTime.MinValue;
            if (cell == null) return false;

            try
            {
                if (cell.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(cell))
                {
                    result = (DateTime)cell.DateCellValue;
                    return true;
                }

                if (cell.CellType == CellType.String)
                {
                    var dateString = cell.StringCellValue;
                    var formats = new[] {
                        "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy",
                        "dd-MM-yyyy", "MM-dd-yyyy", "yyyy/MM/dd"
                    };

                    foreach (var format in formats)
                    {
                        if (DateTime.TryParseExact(dateString, format,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out result))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

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

        public async Task<IActionResult> Create([Bind("FirstName,LastName,Email,PhoneNumber,Gender,DOB,DepartmentId,RoleId,IsActive,StateId,CityId,JoiningDate")] Employee employee,
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

        /* public async Task<Guid> GetDepartmentIdAsync(string departmentName)
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
             return city.CityId;
         }*/
    }
}
