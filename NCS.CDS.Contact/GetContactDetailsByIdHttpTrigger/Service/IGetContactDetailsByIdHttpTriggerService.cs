using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.ContactDetails.Models;

namespace NCS.DSS.ContactDetails.GetContactDetailsByIdHttpTrigger.Service
{
    public interface IGetContactDetailsByIdHttpTriggerService
    {
        Task<Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactId);

    }
}
