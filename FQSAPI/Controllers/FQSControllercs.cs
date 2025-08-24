using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Data.SqlClient;
using System.Text.Json;
using Microsoft.VisualBasic;
using FQSAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Firebase.Database;
using LiteDB;

namespace FQSAPI.Controllers
{
    [ApiController]
    [Route("FQS")]
    public class FQSController : ControllerBase
    {
        static string connString = @"data source = localhost\MSSQLSERVER2; user id = sa; password = D3f@ult!; initial catalog = FQS ;";
        SqlConnection conn = new SqlConnection(connString);

        private readonly IHubContext<QueueHub> _hubContext;
        private readonly string _connectionString;

        public FQSController(IHubContext<QueueHub> hubContext, IConfiguration configuration)
        {
            _hubContext = hubContext;
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // Add or post request
        [HttpPost("AddQueueCode")]
        public async Task<ActionResult<string>> AddQueueCode([FromBody] FQSModel input)
        {
            string query = "insert into tblQueue (queueCode) values ('" + input.queueCode + "')";
            conn.Query<FQSModel>(query);

            // Send SignalR message to all connected clients (including WPF app)
            await _hubContext.Clients.All.SendAsync("ReceiveQueueCode", input.queueCode);

            return Ok("Added");
        }



    }
}
