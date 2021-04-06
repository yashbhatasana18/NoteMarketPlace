using System;
using System.Linq;
using NotesMarketPlace.Models;
namespace NotesMarketPlace.DB.DBOperations
{
    public class AddNotesRepository
    {
        public int AddNotes(AddNotesModel model, string Command)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                SellerNotes addNotes = new SellerNotes();

                addNotes.SellerID = model.SellerID;
                addNotes.Title = model.Title;
                addNotes.Category = model.Category;
                addNotes.ActionedBy = model.SellerID;
                addNotes.DisplayPicture = model.DisplayPicture;
                addNotes.NotesPreview = model.NotesPreview;
                addNotes.NoteType = model.NoteType;
                addNotes.Status = Command == "Save" ? 6 : 7;
                addNotes.NumberOfPages = model.NumberOfPages;
                addNotes.Description = model.Description;
                addNotes.Country = model.Country;
                addNotes.UniversityName = model.UniversityName;
                addNotes.Course = model.Course;
                addNotes.CourseCode = model.CourseCode;
                addNotes.Professor = model.Professor;
                addNotes.SellingPrice = model.SellingPrice;
                addNotes.PublishedDate = DateTime.Now;
                addNotes.CreatedDate = DateTime.Now;
                addNotes.CreatedBy = model.SellerID;
                addNotes.ModifiedDate = DateTime.Now;
                addNotes.ModifiedBy = model.SellerID;
                addNotes.IsPaid = model.IsPaid;
                addNotes.IsActive = true;

                if (addNotes.Status == 7)
                {
                    addNotes.PublishedDate = DateTime.Now;
                    context.SaveChanges();
                }

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
