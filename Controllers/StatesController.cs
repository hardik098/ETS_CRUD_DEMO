using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ETS_CRUD_DEMO.Data;
using ETS_CRUD_DEMO.Models;
using System.Globalization;
using OfficeOpenXml;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;

namespace ETS_CRUD_DEMO.Controllers
{
    [Authorize]

    public class StatesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: States
        public async Task<IActionResult> Index()
        {
            return View(await _context.States.ToListAsync());
        }

        // Import states from CSV or Excel file
        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || (file.ContentType != "text/csv" && file.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"))
            {
                return BadRequest("Invalid file format.");
            }

            var states = new List<State>();

            if (file.ContentType == "text/csv")
            {
                using var reader = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

                // Check if header contains "StateName" only
                csv.Read();
                csv.ReadHeader();
                if (!csv.HeaderRecord.Contains("StateName"))
                {
                    return BadRequest("The CSV file must contain only the 'StateName' column.");
                }

                // Read records and generate GUIDs for each state
                while (csv.Read())
                {
                    var stateName = csv.GetField<string>("StateName");
                    states.Add(new State
                    {
                        StateId = Guid.NewGuid(),  // Generate a new GUID for each state
                        StateName = stateName
                    });
                }
            }
            else if (file.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];

                // Check if header in first row contains "StateName" only
                var header = worksheet.Cells[1, 1].Text;
                if (header != "StateName")
                {
                    return BadRequest("The Excel file must contain only the 'StateName' column.");
                }

                // Read rows and generate GUIDs for each state
                for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                {
                    var stateName = worksheet.Cells[row, 1].Text;
                    states.Add(new State
                    {
                        StateId = Guid.NewGuid(),  // Generate a new GUID for each state
                        StateName = stateName
                    });
                }
            }

            // Save states to the database
            _context.States.AddRange(states);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }



        // Export states to CSV file
        public async Task<IActionResult> ExportToCsv()
        {
            var states = await _context.States.ToListAsync();
            var memoryStream = new MemoryStream();
            using (var writer = new StreamWriter(memoryStream, leaveOpen: true))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(states);
            }

            memoryStream.Position = 0;
            return File(memoryStream, "text/csv", "States.csv");
        }

        // Export states to Excel file
        public async Task<IActionResult> ExportToExcel()
        {
            var states = await _context.States.ToListAsync();
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("States");

            worksheet.Cells[1, 1].Value = "State Name";

            for (int i = 0; i < states.Count; i++)
            {
                worksheet.Cells[i + 2, 1].Value = states[i].StateName;
            }

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "States.xlsx");
        }


        // GET: States/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var state = await _context.States
                .FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                return NotFound();
            }

            return View(state);
        }

        // GET: States/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: States/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StateId,StateName")] State state)
        {
            //if (ModelState.IsValid)
            {
                state.StateId = Guid.NewGuid();
                _context.Add(state);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(state);
        }

        // GET: States/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var state = await _context.States.FindAsync(id);
            if (state == null)
            {
                return NotFound();
            }
            return View(state);
        }

        // POST: States/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("StateId,StateName")] State state)
        {
            if (id != state.StateId)
            {
                return NotFound();
            }

            //if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(state);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StateExists(state.StateId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(state);
        }

        // GET: States/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var state = await _context.States
                .FirstOrDefaultAsync(m => m.StateId == id);
            if (state == null)
            {
                return NotFound();
            }

            return View(state);
        }

        // POST: States/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var state = await _context.States.FindAsync(id);
            if (state != null)
            {
                _context.States.Remove(state);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StateExists(Guid id)
        {
            return _context.States.Any(e => e.StateId == id);
        }
    }
}
