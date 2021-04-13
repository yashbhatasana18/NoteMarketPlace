using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotesMarketPlace.Models.Admin
{
    public class SystemConfigurationsModel
    {
        public int SystemConfigurationsID { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        [Required(ErrorMessage = "Required field")]
        [EmailAddress]
        public string SupportEmail { get; set; }

        [Required(ErrorMessage = "Required field")]
        public string SupportContact { get; set; }

        public string Emails { get; set; }

        public string FacebookUrl { get; set; }

        public string TwitterUrl { get; set; }

        public string LinkedinUrl { get; set; }

        //[Required(ErrorMessage = "Required field")]
        public string DefaulProfileImg { get; set; }

        //[Required(ErrorMessage = "Required field")]
        public string DefaultNoteImg { get; set; }

        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }
    }
}
