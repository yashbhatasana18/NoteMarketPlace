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
                    SellerID = 1,
                    Title = model.Title,
                    Category = model.Category,
                    //DisplayPicture = model.DisplayPicture,
                    //NotesPreview = model.NotesPreview,
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
                    CreatedDate = DateTime.Now,
                    CreatedBy = 1,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = 1,
                    IsActive = true,
                };

                SellerNotesAttachements sellerNotesAttachements = new SellerNotesAttachements()
                {
                    NoteID = model.SellerNotesID,
                    //FilePath = model.UploadFile,
                    //FileName = model.FileName,
                    IsActive = true
                };

                try
                {
                    context.SellerNotes.Add(addNotes);
                    //context.SellerNotesAttachements.Add(sellerNotesAttachements);
                    context.SaveChanges();
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    Exception raise = dbEx;
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            string message = string.Format("{0}:{1}",
                                validationErrors.Entry.Entity.ToString(),
                                validationError.ErrorMessage);
                            // raise a new exception nesting  
                            // the current instance as InnerException  
                            raise = new InvalidOperationException(message, raise);
                        }
                    }
                    throw raise;
                }

                return addNotes.SellerID;
            }
        }
    }
}
