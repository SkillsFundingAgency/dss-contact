﻿using NCS.DSS.Contact.Models;

namespace NCS.DSS.Contact.PostContactDetailsHttpTrigger.Service
{
    public interface IPostContactDetailsHttpTriggerService
    {
        bool DoesContactDetailsExistForCustomer(Guid customerId);
        Task<ContactDetails> CreateAsync(ContactDetails contactdetails);
        Task SendToServiceBusQueueAsync(ContactDetails contactdetails, string reqUrl);
    }
}
