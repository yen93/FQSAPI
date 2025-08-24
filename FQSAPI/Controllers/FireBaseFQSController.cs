using Microsoft.AspNetCore.Mvc;
using Dapper;
using System.Data.SqlClient;
using FQSAPI.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Server.HttpSys;
using System.ComponentModel.Design;
using Newtonsoft.Json.Linq;

namespace FQSAPI.Controllers
{
    [ApiController]
    [Route("FireBaseFQS")]
    public class FireBaseFQSController : ControllerBase
    {
        private readonly IHubContext<QueueHub> _hubContext;
        private readonly FirebaseClient _firebaseClient;

        public FireBaseFQSController(IHubContext<QueueHub> hubContext, FirebaseClient firebaseClient)
        {
            _hubContext = hubContext;
            _firebaseClient = firebaseClient;
        }

        [HttpPost("AddQueueCode")]
        public async Task<ActionResult<string>> AddQueueCode()
        {
            try
            {
                // Get all items
                var allItems = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                // Get today's date code
                string todayDateCode = DateTime.Today.ToString("yyyyMMdd");

                // Find the maxQueueCode (simpler approach)
                int maxQueueCode = allItems
                    .Where(item => item.Object != null)
                    .Where(item => item.Object.dateCode?.ToString() == todayDateCode)
                    .Select(item => item.Object.queueCode)
                    .Where(queueCode => queueCode != null)
                    .Select(queueCode =>
                    {
                        int.TryParse(queueCode.ToString(), out int result);
                        return result;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                int nextQueueCode;

                // Check if we have any items for today
                if (maxQueueCode == 0)
                {
                    nextQueueCode = 1;
                }
                else
                {
                    nextQueueCode = maxQueueCode + 1;
                }

                // Create an object to store in Firebase
                var queueData = new
                {
                    queueCode = nextQueueCode,
                    dateCode = todayDateCode,
                    timeCreated = DateTime.Now,
                    status = "Pending"
                };

                // Push the object to Firebase
                await _firebaseClient
                    .Child("tblQueue")
                    .PostAsync(queueData);

                // Send SignalR message to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceiveQueueCode", nextQueueCode + ":" + 1);

                return Ok(nextQueueCode+":"+1);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("UpdateQueueCode")]
        public async Task<ActionResult<string>> UpdateQueueCode([FromBody] FirebaseUpdateModel request)
        {
            try
            {
                string newFieldValue = "";
                string newStatusValue = "";
                if (request.NewStatus == 2)
                {
                    newFieldValue = "TimeStarted";
                    newStatusValue = "In Progress";
                }
                else if (request.NewStatus == 3)
                {
                    newFieldValue = "TimeCompleted";
                    newStatusValue = "Ready";
                }
                else if (request.NewStatus == 4)
                {
                    newFieldValue = "TimeCleared";
                    newStatusValue = "Done";
                }

                // First, get all items and filter locally (for debugging)
                var allItems = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                // Find the item with matching queueCode
                var targetItem = allItems.FirstOrDefault(item =>
                    item.Object != null &&
                    item.Object.queueCode == request.QueueCode);

                if (targetItem != null)
                {
                    await _firebaseClient
                        .Child("tblQueue")
                        .Child(targetItem.Key)
                        .Child("status")
                        .PutAsync(JsonConvert.SerializeObject(newStatusValue));

                    var newField = newFieldValue;

                    // Create a dictionary to dynamically set the field name
                    var updateData = new Dictionary<string, object>
                    {
                        { newField, DateTime.Now } // Use the value as field name
                    };

                    await _firebaseClient
                        .Child("tblQueue")
                        .Child(targetItem.Key)
                        .PatchAsync(updateData);

                    // Send SignalR message to all connected clients
                    await _hubContext.Clients.All.SendAsync("ReceiveQueueCode", request.QueueCode + ":" + request.NewStatus);

                    return Ok("QueueCode status updated successfully!");
                }
                else
                {
                    return NotFound($"Queue code {request.QueueCode} not found");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("GetActiveQueue")]
        public async Task<ActionResult<ActiveQueueModel>> GetActiveQueue()
        {
            try
            {
                // First, get all items and filter locally (for debugging)
                var allItems = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                var queued = new List<int>();
                //queued = allItems where status = "Pending";

                var started = new List<int>();
                //started = allItems where status = "In Progress";

                var done = new List<int>();
                //done = allItems where status = "Completed";

                foreach (var item in allItems)
                {
                    if(item.Object == null)
                    {
                        continue;
                    }
                    else
                    {
                        int queueCode = Int32.Parse(item.Object.queueCode.ToString());
                        string status = item.Object.status.ToString();

                        switch (status.ToLower())
                        {
                            case "pending":
                                queued.Add(queueCode);
                                break;
                            case "in progress":
                                started.Add(queueCode);
                                break;
                            case "ready":
                                done.Add(queueCode);
                                break;
                        }
                    }
                }

                return Ok(new ActiveQueueModel
                {
                    Queued = queued,
                    InProgress = started,
                    Completed = done
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("CreateMenuItem")]
        public async Task<ActionResult<ActiveQueueModel>> CreateMenuItem(MenuModel data)
        {
            try
            {
                // Get all items
                var allItems = await _firebaseClient
                    .Child("tblMenu")
                    .OnceAsync<dynamic>();

                // Find the max ID
                int maxID = allItems
                    .Where(item => item.Object != null)
                    .Select(item => item.Object.ID)
                    .Where(ID => ID != null)
                    .Select(ID =>
                    {
                        return int.Parse(ID.ToString());
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                //set Data ID
                data.ID = maxID+1;

                // Push the object to Firebase
                await _firebaseClient
                    .Child("tblMenu")
                    .PostAsync(data);

                return Ok("Menu item has been added!");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("GetMenuList")]
        public async Task<ActionResult<ActiveQueueModel>> GetMenuList()
        {
            try
            {
                // First, get all items and filter locally (for debugging)
                var allItems = await _firebaseClient
                    .Child("tblMenu")
                    .OnceAsync<dynamic>();

                var menuList = new List<MenuModel>();

                foreach (var item in allItems)
                {
                    if (item.Object == null)
                    {
                        continue;
                    }
                    else
                    {
                        var data = new MenuModel
                        {
                            ID = item.Object.ID,
                            Name = item.Object.Name,
                            Price = item.Object.Price,
                            Image = item.Object.Image
                        };

                        menuList.Add(data);
                    }
                }

                return Ok(menuList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("SubmitOrder")]
        public async Task<ActionResult<ActiveQueueModel>> SubmitOrder([FromBody] List<OrderItemModel> itemsList)
        {
            try
            {
                // Get all items
                var allItems = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                // Get today's date code
                string todayDateCode = DateTime.Today.ToString("yyyyMMdd");

                // Find the maxQueueCode (simpler approach)
                int maxQueueCode = allItems
                    .Where(item => item.Object != null)
                    .Where(item => item.Object.dateCode?.ToString() == todayDateCode)
                    .Select(item => item.Object.queueCode)
                    .Where(queueCode => queueCode != null)
                    .Select(queueCode =>
                    {
                        int.TryParse(queueCode.ToString(), out int result);
                        return result;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                int nextQueueCode;

                // Check if we have any items for today
                if (maxQueueCode == 0)
                {
                    nextQueueCode = 1;
                }
                else
                {
                    nextQueueCode = maxQueueCode + 1;
                }

                // Create an object to store in Firebase
                var queueData = new
                {
                    queueCode = nextQueueCode,
                    dateCode = todayDateCode,
                    timeCreated = DateTime.Now,
                    status = "Pending"
                };

                // Push the object to Firebase
                await _firebaseClient
                    .Child("tblQueue")
                    .PostAsync(queueData);

                // Create order object
                var orderData = new
                {
                    orderId = nextQueueCode,
                    dateCode = todayDateCode,
                    items = itemsList
                };

                // Push to Firebase tblOrders
                await _firebaseClient
                    .Child("tblOrders")
                    .PostAsync(orderData);

                // Send SignalR message to all connected clients
                await _hubContext.Clients.All.SendAsync("ReceiveQueueCode", nextQueueCode + ":" + 1);

                return Ok(nextQueueCode + ":" + 1);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("GetActiveOrders")]
        public async Task<ActionResult<List<ActiveQueueModel>>> GetActiveOrders()
        {
            try
            {
                // Fetch queue and orders
                var allQueue = await _firebaseClient
                    .Child("tblQueue")
                    .OnceAsync<dynamic>();

                var allActiveOrders = await _firebaseClient
                    .Child("tblOrders")
                    .OnceAsync<dynamic>();

                var result = new List<ActiveOrdersModel>();

                foreach (var q in allQueue)
                {
                    if (q.Object == null) continue;

                    string status = q.Object.status?.ToString();

                    // Skip if Done
                    if (status == "Done") continue;

                    // Find matching order
                    var order = allActiveOrders.FirstOrDefault(x =>
                        x.Object.orderId == q.Object.queueCode
                    );

                    var orderID = order.Object.orderId;
                    var itemsList = new List<OrderItemModel>();
                    itemsList = ((JArray)order.Object.items).ToObject<List<OrderItemModel>>();

                    if (order != null)
                    {
                        result.Add(new ActiveOrdersModel
                        {
                            OrderId = orderID,
                            Items = itemsList,
                            Status = q.Object.status
                        });
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

    }
}