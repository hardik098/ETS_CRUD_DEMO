using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ETS_CRUD_DEMO.Data;
using ETS_CRUD_DEMO.Models;
using OfficeOpenXml;
using CsvHelper;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace ETS_CRUD_DEMO.Controllers
{
    [Authorize]
    public class CitiesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CitiesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Cities
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Cities.Include(c => c.State);
            return View(await applicationDbContext.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> GetCities([FromForm] DataTableParameters parameters)
        {
            // Base query including related data
            var query = _context.Cities
                .Include(c => c.State)
                .Select(city => new
                {
                    city.CityId,
                    city.CityName,
                    StateName = city.State != null ? city.State.StateName : "N/A"
                });

            // Apply search filter if search value is present
            if (!string.IsNullOrWhiteSpace(parameters.Search?.Value))
            {
                string searchValue = parameters.Search.Value.ToLower();
                query = query.Where(city =>
                    city.CityName.ToLower().Contains(searchValue) ||
                    (city.StateName ?? "").ToLower().Contains(searchValue)
                );
            }

            // Sorting
            if (parameters.Order.Any())
            {
                var order = parameters.Order.First();
                bool ascending = order.Dir == "asc";

                query = order.Column switch
                {
                    1 => ascending ? query.OrderBy(c => c.CityName) : query.OrderByDescending(c => c.CityName),
                    2 => ascending ? query.OrderBy(c => c.StateName) : query.OrderByDescending(c => c.StateName),
                    _ => query // Ignore sorting on CityId if no valid column specified
                };
            }

            // Total record count before pagination
            int recordsTotal = await _context.Cities.CountAsync();

            // Apply pagination
            var data = await query
                .Skip(parameters.Start)
                .Take(parameters.Length)
                .ToListAsync();

            // Prepare result data
            var resultData = data.Select(city => new
            {
                city.CityId,
                city.CityName,
                StateName = city.StateName
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



        // GET: Cities/Export
        public IActionResult Export()
        {
            var cities = _context.Cities.Include(c => c.State).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Cities");
                worksheet.Cells[1, 1].Value = "CityName";
                worksheet.Cells[1, 2].Value = "StateName";

                for (int i = 0; i < cities.Count; i++)
                {
                    worksheet.Cells[i + 2, 1].Value = cities[i].CityName;
                    worksheet.Cells[i + 2, 2].Value = cities[i].State.StateName;
                }

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = "Cities.xlsx";
                var contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                return File(stream, contentType, fileName);
            }
        }

        // POST: Cities/Import
        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                return BadRequest("Please upload a valid file.");
            }

            if (file.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" &&
                file.ContentType != "text/csv")
            {
                return BadRequest("Only Excel (.xlsx) and CSV files are supported.");
            }

            try
            {
                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                var cities = new List<City>();

                if (file.ContentType == "text/csv")
                {
                    // Handle CSV file
                    stream.Position = 0;
                    using var reader = new StreamReader(stream);
                    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                    // Validate headers
                    csv.Read();
                    csv.ReadHeader();
                    var headerRow = csv.HeaderRecord;

                    var requiredColumns = new List<string> { "CityName", "StateName" };
                    var missingColumns = requiredColumns.Where(column => !headerRow.Contains(column)).ToList();

                    if (missingColumns.Any())
                    {
                        return BadRequest($"Missing required columns: {string.Join(", ", missingColumns)}.");
                    }

                    while (csv.Read())
                    {
                        var cityName = csv.GetField<string>("CityName");
                        var stateName = csv.GetField<string>("StateName");

                        // Find or create State
                        var state = await _context.States
                            .FirstOrDefaultAsync(s => s.StateName == stateName);

                        if (state == null)
                        {
                            state = new State
                            {
                                StateId = Guid.NewGuid(),
                                StateName = stateName
                            };
                            _context.States.Add(state);
                            await _context.SaveChangesAsync();
                        }

                        // Create City
                        var city = new City
                        {
                            CityId = Guid.NewGuid(),
                            CityName = cityName,
                            StateId = state.StateId
                        };
                        cities.Add(city);
                    }
                }
                else
                {
                    // Handle Excel file
                    stream.Position = 0;
                    using var package = new ExcelPackage(stream);
                    var worksheet = package.Workbook.Worksheets[0];

                    // Validate headers
                    var requiredColumns = new List<string> { "CityName", "StateName" };
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

                    // Get column indices
                    var cityNameIndex = headerRow.IndexOf("CityName") + 1;
                    var stateNameIndex = headerRow.IndexOf("StateName") + 1;

                    for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                    {
                        var cityName = worksheet.Cells[row, cityNameIndex].Text;
                        var stateName = worksheet.Cells[row, stateNameIndex].Text;

                        if (string.IsNullOrWhiteSpace(cityName) || string.IsNullOrWhiteSpace(stateName))
                        {
                            continue; // Skip rows with empty values
                        }

                        // Find or create State
                        var state = await _context.States
                            .FirstOrDefaultAsync(s => s.StateName == stateName);

                        if (state == null)
                        {
                            state = new State
                            {
                                StateId = Guid.NewGuid(),
                                StateName = stateName
                            };
                            _context.States.Add(state);
                            await _context.SaveChangesAsync();
                        }

                        // Create City
                        var city = new City
                        {
                            CityId = Guid.NewGuid(),
                            CityName = cityName,
                            StateId = state.StateId
                        };
                        cities.Add(city);
                    }
                }

                // Batch insert cities
                await _context.Cities.AddRangeAsync(cities);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log the exception here
                return BadRequest($"Error processing file: {ex.Message}");
            }
        }

        // GET: Cities/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var city = await _context.Cities
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.CityId == id);
            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        // GET: Cities/Create
        public IActionResult Create()
        {
            ViewData["StateId"] = new SelectList(_context.States, "StateId", "StateName");
            return View();
        }

        // POST: Cities/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CityId,CityName,StateId")] City city)
        {
            //if (ModelState.IsValid)
            {
                city.CityId = Guid.NewGuid();
                _context.Add(city);
                await _context.SaveChangesAsync();

                ViewData["StateId"] = new SelectList(_context.States, "StateId", "StateName", city.StateId);

                return RedirectToAction(nameof(Index));
            }
            //ViewData["StateId"] = new SelectList(_context.States, "StateId", "StateName", city.StateId);
            return View(city);
        }

        // GET: Cities/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var city = await _context.Cities.FindAsync(id);
            if (city == null)
            {
                return NotFound();
            }
            ViewData["StateId"] = new SelectList(_context.States, "StateId", "StateName", city.StateId);
            return View(city);
        }

        // POST: Cities/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("CityId,CityName,StateId")] City city)
        {
            if (id != city.CityId)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(city);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CityExists(city.CityId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                ViewData["StateId"] = new SelectList(_context.States, "StateId", "StateName", city.StateId);
                return RedirectToAction(nameof(Index));
            }
            //ViewData["StateId"] = new SelectList(_context.States, "StateId", "StateName", city.StateId);
            return View(city);
        }

        // GET: Cities/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var city = await _context.Cities
                .Include(c => c.State)
                .FirstOrDefaultAsync(m => m.CityId == id);
            if (city == null)
            {
                return NotFound();
            }

            return View(city);
        }

        // POST: Cities/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city != null)
            {
                _context.Cities.Remove(city);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CityExists(Guid id)
        {
            return _context.Cities.Any(e => e.CityId == id);
        }
    }
}
