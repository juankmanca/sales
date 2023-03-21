using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TEST2.Clases;
using TEST2.Models;

namespace TEST2.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly Test2Context _context;
        public EmployeeController(Test2Context context)
        {
            _context = context;
        }

        // Get
        public async Task<IActionResult> Index()
        {
            var users = await _context.Usuarios.ToListAsync();
            return View(users);
        }

        // Get by id
        public async Task<IActionResult> Detail(int id)
        {
            var user = await _context.Usuarios.FindAsync(id);
            return View(user);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            if(id == 0)
            {
                return NotFound();
            }

            var user = await _context.Usuarios.FindAsync(id);
            if(user == null)
            {
                return NotFound();
            }
            _context.Usuarios.Remove(user);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Post(Employee employee)
        {
            Empleado p = new Empleado(23, "juan", new Guid(), "Dev");

            List<Empleado> emp = new List<Empleado>();
            emp.Add(p);            

            if(employee == null)
            {
                return NotFound();
            }
            _context.Employees.Add(employee);
            return View(employee);
        }


    }
}
