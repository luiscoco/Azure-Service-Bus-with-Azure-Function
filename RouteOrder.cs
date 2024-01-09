using System;
using System.Text;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using ServiceBusTriggerAttribute = Microsoft.Azure.Functions.Worker.ServiceBusTriggerAttribute;

namespace RouteOrder
{
    public class RouteOrder
    {
        private readonly ILogger<RouteOrder> _logger;

        public RouteOrder(ILogger<RouteOrder> logger)
        {
            _logger = logger;
        }

        [FunctionName("RouteOrder")]
        [return: ServiceBus("Ordertofullfillmentcentre", Connection = "FFillCenterQueueconnection")]
        public static ServiceBusMessage Run([ServiceBusTrigger("queue.custorder", Connection = "OrderQueueconnection")] string myQueueItem, ILogger log)
        {
            string supplyWarehouse = "ERROR";
            var updateMessage = UpdateWarehouseLocation(myQueueItem, out supplyWarehouse, log);
            log.LogInformation($"C# ServiceBus queue trigger function processed message: {updateMessage}");
            var outputMessage = new ServiceBusMessage(Encoding.ASCII.GetBytes(updateMessage));
            outputMessage.ApplicationProperties.Add("SupplyWarehouse", supplyWarehouse);
            return outputMessage;
        }

        private static string UpdateWarehouseLocation(string order, out string swarehouse, ILogger log)
        {
            var customerOrder = JObject.Parse(order);
            var sku = customerOrder["Item"]["sku"].ToString();
            var quantity = customerOrder["Item"]["quantity"].ToString();
            var city = customerOrder["Item"]["city"].ToString();
            swarehouse = DummyCheckStock(sku, Convert.ToInt32(quantity), city, log);
            customerOrder.Add("SupplyWarehouse", swarehouse);

            return customerOrder.ToString();
        }

        private static string DummyCheckStock(string sku, int quantity, string city, ILogger log)
        {
            string finalwarehouse = city;
            var rand = new Random();
            var result = rand.Next() % 4;
            switch (result)
            {
                case 0:
                    finalwarehouse = "Liverpool";
                    break;
                case 1:
                    finalwarehouse = "Birmingham";
                    break;
                case 2:
                    finalwarehouse = "Manchester";
                    break;
                default:
                    finalwarehouse = "VENDOR_REORDER";
                    break;
            }
            log.LogInformation($"Stock check result: {finalwarehouse}");
            return finalwarehouse;
        }
    }
}
