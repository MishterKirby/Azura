using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Azura.Auth.Models
{
    public class Authorization(string code)
    {
        public string Code { get; } = code;
    }
}
