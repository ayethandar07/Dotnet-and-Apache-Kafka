using Confluent.Kafka;
using EmployeeApplicationWebApi.Database;
using EmployeeApplicationWebApi.Models;
using EmployeeApplicationWebApi.Services.Producer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.Json;

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

        [HttpPost]
        public async Task<ActionResult<Employee>> CreateEmployee(string name, string surname)
        {
            var employee = new Employee(Guid.NewGuid(), name, surname);
            _dbContext.Employees.Add(employee);
            await _dbContext.SaveChangesAsync();

            await _producerService.ProduceAsync(employee.Id.ToString(), employee);

            return CreatedAtAction(nameof(CreateEmployee), new { id = employee.Id }, employee);

        }
    }
}
