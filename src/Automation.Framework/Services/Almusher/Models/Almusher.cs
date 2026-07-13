using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automation.Framework.Services.Almusher.Models
{
    public class CreatePaymentChainResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Statement { get; set; } = string.Empty;
    }
}
