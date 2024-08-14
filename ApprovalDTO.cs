using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DurableFunctionTriggeredByExternalEventDemo
{
    public class ApprovalDTO
    {
        public string InstanceId { get; set;}
        public bool IsApproved { get; set;}
    }
}
