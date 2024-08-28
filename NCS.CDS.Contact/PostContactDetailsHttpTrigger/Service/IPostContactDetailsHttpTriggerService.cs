namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public interface IPostContactDetailsHttpTriggerService
    {
        bool DoesContactDetailsExistForCustomer(Guid customerId);
        Task<Models.ContactDetails> CreateAsync(Models.ContactDetails contactdetails);
        Task SendToServiceBusQueueAsync(Models.ContactDetails contactdetails, string reqUrl);
    }
}
