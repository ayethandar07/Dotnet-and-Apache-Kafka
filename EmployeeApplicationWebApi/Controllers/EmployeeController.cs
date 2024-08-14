using Confluent.Kafka;
using EmployeeApplicationWebApi.Database;
using EmployeeApplicationWebApi.Models;
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
        private readonly KafkaSettings _kafkaSettings;

        public EmployeeController(EmployeeDbContext dbContext, ILogger<EmployeeController> logger, IOptions<KafkaSettings> kafkaSettings)
        {
            _dbContext = dbContext;
            _logger = logger;
            _kafkaSettings = kafkaSettings.Value;
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

            var message = new Message<string, string>()
            {
                Key = employee.Id.ToString(),
                Value = JsonSerializer.Serialize(employee)
            };

            // client
            var producerConfig = new ProducerConfig()
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
                Acks = Enum.Parse<Acks>(_kafkaSettings.Acks!, true),
            };

            var producer = new ProducerBuilder<string, string>(producerConfig).Build();
            await producer.ProduceAsync("employeeTopic", message);
            producer.Dispose();

            return CreatedAtAction(nameof(CreateEmployee), new {id = employee.Id}, employee);
        }
    }
}
