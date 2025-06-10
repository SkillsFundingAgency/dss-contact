using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.ServiceBus
{
    public interface IServiceBusClient
    {
        Task SendPostMessageAsync(ContactDetails contactDetails, string reqUrl);
        Task SendPatchMessageAsync(ContactDetails contactDetails, Guid customerId, string reqUrl);
    }
}
