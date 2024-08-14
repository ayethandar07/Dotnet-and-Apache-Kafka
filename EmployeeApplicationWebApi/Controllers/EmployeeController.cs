using EmployeeApplicationWebApi.Database;
using EmployeeApplicationWebApi.Models;
using EmployeeApplicationWebApi.Services.Producer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeApplicationApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeDbContext _dbContext;
        private readonly ILogger<EmployeeController> _logger;
        private readonly KafkaProducerService _producerService;

        public EmployeeController(EmployeeDbContext dbContext, ILogger<EmployeeController> logger, KafkaProducerService producerService)
        {
            _dbContext = dbContext;
            _logger = logger;
            _producerService = producerService;
        }

        [HttpGet]
        public async Task<IEnumerable<Employee>> GetEmployees()
        {
            _logger.LogInformation("Requesting all employees");
            return await _dbContext.Employees.ToListAsync();
        }

        [HttpPost("single")]
        public async Task<ActionResult<Employee>> CreateEmployee(string name, string surname)
        {
            var employee = new Employee(Guid.NewGuid(), name, surname);
            _dbContext.Employees.Add(employee);
            await _dbContext.SaveChangesAsync();

            await _producerService.ProduceAsync(employee.Id.ToString(), employee);

            return CreatedAtAction(nameof(CreateEmployee), new { id = employee.Id }, employee);

        }

        [HttpPost("bulk")]
        public async Task<IActionResult> CreateEmployees([FromBody] List<EmployeeCreationDto> employeesDto)
        {
            if (employeesDto == null || employeesDto.Count == 0)
            {
                return BadRequest("No employees provided");
            }

            var employees = employeesDto.Select(dto => new Employee(Guid.NewGuid(), dto.Name, dto.Surname!)).ToList();

            _dbContext.Employees.AddRange(employees);
            await _dbContext.SaveChangesAsync();

            // Produce the list as a JSON array
            await _producerService.ProduceAsync(Guid.NewGuid().ToString(), employees);

            return CreatedAtAction(nameof(CreateEmployees), new { Count = employees.Count });
        }
    }
}
