using System;
using System.Collections.Generic;
using System.Linq;
using NotesMarketPlace.Models;

namespace NotesMarketPlace.DB.DBOperations
{
    public class SellYourNotesRepository
    {
        public List<SellerNotesModel> GetAllPublishedNotes()
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var result = context.SellerNotes
                   .Select(x => new SellerNotesModel()
                   {
                       SellerNotesID = x.SellerNotesID,
                       SellerID = x.SellerID,
                       Status = x.Status,
                       ActionedBy = x.ActionedBy,
                       AdminRemarks = x.AdminRemarks,
                       PublishedDate = x.PublishedDate,
                       Title = x.Title,
                       Category = x.Category,
                       DisplayPicture = x.DisplayPicture,
                       NoteType = x.NoteType,
                       NumberOfPages = x.NumberOfPages,
                       Description = x.Description,
                       UniversityName = x.UniversityName,
                       Country = x.Country,
                       Course = x.Course,
                       CourseCode = x.CourseCode,
                       Professor = x.Professor,
                       IsPaid = x.IsPaid,
                       SellingPrice = x.SellingPrice,
                       NotesPreview = x.NotesPreview,
                       CreatedDate = x.CreatedDate,
                       CreatedBy = x.CreatedBy,
                       ModifiedDate = x.ModifiedDate,
                       ModifiedBy = x.ModifiedBy,
                       IsActive = x.IsActive,

                       CategoryName = x.NoteCategories.Name,
                       StatusValue = x.ReferenceData.Value
                   }).ToList();

                return result;
            }
        }
    }
}
