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
                    Title = model.Title,
                    Category = model.Category,
                    DisplayPicture = model.DisplayPicture,
                    NotesPreview = model.NotesPreview,
                    NoteType = model.NoteType,
                    Status = model.Status,
                    NumberOfPages = model.NumberOfPages,
                    Description = model.Description,
                    Country = model.Country,
                    UniversityName = model.UniversityName,
                    Course = model.Course,
                    CourseCode = model.CourseCode,
                    Professor = model.Professor,
                    SellingPrice = model.SellingPrice,
                    IsPaid = model.IsPaid,
                    CreatedDate = DateTime.Now,
                    CreatedBy = 1,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = 1,
                    IsActive = true,
                };

                SellerNotesAttachements sellerNotesAttachements = new SellerNotesAttachements()
                {
                    NoteID = model.SellerNotesID,
                    FilePath = model.UploadFile,
                    FileName = model.FileName,
                    IsActive = true
                };

                context.SellerNotes.Add(addNotes);
                context.SellerNotesAttachements.Add(sellerNotesAttachements);
                context.SaveChanges();
                return addNotes.SellerID;
            }
        }
    }
}
