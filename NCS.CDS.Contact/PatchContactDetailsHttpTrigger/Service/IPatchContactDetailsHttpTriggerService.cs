using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NCS.DSS.ContactDetails.PatchContactDetailsHttpTrigger.Service
{
    public interface IPatchContactDetailsHttpTriggerService
    {
        Task<ContactDetails.Models.ContactDetails> UpdateAsync(ContactDetails.Models.ContactDetails contactdetails, ContactDetails.Models.ContactDetailsPatch contactdetailsPatch);
        Task<ContactDetails.Models.ContactDetails> GetContactDetailsForCustomerAsync(Guid customerId, Guid contactdetailsId);

    }
}
