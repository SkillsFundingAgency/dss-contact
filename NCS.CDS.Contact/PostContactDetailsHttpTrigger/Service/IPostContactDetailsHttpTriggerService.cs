using System.Threading.Tasks;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public interface IPostContactDetailsHttpTriggerService
    {
        Task<Models.ContactDetails> CreateAsync(Models.ContactDetails contactdetails);
        Task SendToServiceBusQueueAsync(Models.ContactDetails contactdetails, string reqUrl);
    }
}
