using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AzureConsoleRemoting.Models
{
    public class GuestInfoRows
    {
        public List<GuestInfoRow> items { get; set; }
    }
    public class GuestInfoRow
    {
        public string userPrincipalName { get; set; }
        public string objectId { get; set; }
        public DateTime? lastLogin { get; set; }
        public bool? loginSucceeded { get; set; }
        public string application { get; set; }
    }

}
