//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using NotesMarketPlace.Models;

//namespace NotesMarketPlace.DB.DBOperations
//{
//    public class SearchRepository
//    {
//        public List<SearchNotesModel> GetNotes()
//        {
//            using (var context = new NotesMarketPlaceEntities())
//            {
//                var result = context.SellerNotes.Select(x => new SearchNotesModel()
//                {
//                    SellerID = x.SellerID,
//                    SellerNotesID = x.SellerNotesID,
//                    Title = x.Title,
//                    Status = x.Status,
//                    ActionedBy = x.ActionedBy,
//                    AdminRemarks = x.AdminRemarks,
//                    PublishedDate = x.PublishedDate,
//                    DisplayPicture = x.DisplayPicture,
//                    NoteType = x.NoteType,
//                    NumberOfPages = x.NumberOfPages,
//                    Description = x.Description,
//                    UniversityName = x.UniversityName,
//                    Country = x.Country,
//                    Course = x.Course,
//                    CourseCode = x.CourseCode,
//                    Professor = x.Professor,
//                    IsPaid = x.IsPaid,
//                    SellingPrice = x.SellingPrice,
//                    NotesPreview = x.NotesPreview,
//                    Category = new NoteCategoriesModel()
//                    {
//                        NoteCategoriesID = x.NoteCategories.NoteCategoriesID,
//                        Name = x.NoteCategories.Name
//                    }
//                }).ToList();
//                return result;
//            }
//        }


//    }
//}
