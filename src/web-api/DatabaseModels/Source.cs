using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WomPlatform.Web.Api.DatabaseModels
{
    public class Source
    {
        public int Id { get; set; }
        public int Id_Contact { get; set; }
        public string Key { get; set; }
        public DateTime CreationDate { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string URLSource { get; set; }
        public int GenerationRequest_Id { get; set; }
        public int Aim_Id { get; set; }
    }
}
