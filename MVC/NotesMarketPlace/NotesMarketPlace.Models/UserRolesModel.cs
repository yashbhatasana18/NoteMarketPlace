﻿using System;

namespace NotesMarketPlace.Models
{
    public class UserRolesModel
    {
        public int UserRolesID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public int? ModifiedBy { get; set; }
        public bool IsActive { get; set; }

        public static implicit operator int(UserRolesModel v)
        {
            throw new NotImplementedException();
        }
    }
}
