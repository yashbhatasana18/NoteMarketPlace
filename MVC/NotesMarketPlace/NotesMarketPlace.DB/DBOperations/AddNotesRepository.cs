using System;
using System.Linq;
using NotesMarketPlace.Models;
namespace NotesMarketPlace.DB.DBOperations
{
    public class AddNotesRepository
    {
        public int AddNotes(AddNotesModel model)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                SellerNotes addNotes = new SellerNotes()
                {
                    SellerID = model.SellerID,
                    Title = model.Title,
                    Category = model.Category,
                    ActionedBy = model.SellerID,
                    DisplayPicture = model.DisplayPicture,
                    NotesPreview = model.NotesPreview,
                    NoteType = model.NoteType,
                    Status = 5,
                    NumberOfPages = model.NumberOfPages,
                    Description = model.Description,
                    Country = model.Country,
                    UniversityName = model.UniversityName,
                    Course = model.Course,
                    CourseCode = model.CourseCode,
                    Professor = model.Professor,
                    SellingPrice = model.SellingPrice,
                    IsPaid = model.IsPaid,
                    PublishedDate = DateTime.Now,
                    CreatedDate = DateTime.Now,
                    CreatedBy = model.SellerID,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = model.SellerID,
                    IsActive = true,
                };

                context.SellerNotes.Add(addNotes);
                context.SaveChanges();

                SellerNotesAttachements sellerNotesAttachements = new SellerNotesAttachements()
                {
                    NoteID = addNotes.SellerNotesID,
                    FilePath = model.FilePath,
                    FileName = model.FileName,
                    CreatedDate = DateTime.Now,
                    CreatedBy = model.SellerID,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = model.SellerID,
                    IsActive = true
                };

                context.SellerNotesAttachements.Add(sellerNotesAttachements);
                context.SaveChanges();

                return addNotes.SellerID;
            }
        }
    }
}
