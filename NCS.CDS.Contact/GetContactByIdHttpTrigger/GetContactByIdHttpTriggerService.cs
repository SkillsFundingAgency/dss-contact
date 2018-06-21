using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NCS.DSS.ContactDetails.Models;

namespace NCS.DSS.ContactDetails.GetContactByIdHttpTrigger
{
    class GetContactByIdHttpTriggerService
    {
        public async Task<List<Models.ContactDetails>> GetContactDetails(Guid contactid)
        {
            var result = GenerateSampleData();
            result.FirstOrDefault(x => x.ContactID == contactid);
            return await Task.FromResult(result);
        }

        private List<Models.ContactDetails> GenerateSampleData()
        {
            var cList = new List<Models.ContactDetails>();

            cList.Add(new Models.ContactDetails { ContactID = Guid.NewGuid(), HomeNumber = "1111111", MobileNumber = "0777888990", EmailAddress = "x_100@x.com" });
            cList.Add(new Models.ContactDetails { ContactID = Guid.NewGuid(), HomeNumber = "2222222", MobileNumber = "0777888991", EmailAddress = "a_200@x.com" });
            cList.Add(new Models.ContactDetails { ContactID = Guid.NewGuid(), HomeNumber = "3333333", MobileNumber = "0777888992", EmailAddress = "b_300@x.com" });
            cList.Add(new Models.ContactDetails { ContactID = Guid.NewGuid(), HomeNumber = "4444444", MobileNumber = "0777888993", EmailAddress = "c_400@x.com" });
            cList.Add(new Models.ContactDetails { ContactID = Guid.NewGuid(), HomeNumber = "5555555", MobileNumber = "0777888994", EmailAddress = "d_500@x.com" });

            return cList;
        }


    }
}
