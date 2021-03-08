using System;
using System.Linq;
using NotesMarketPlace.Models;


namespace NotesMarketPlace.DB.DBOperations
{
    public class SignUpRepository
    {
        public int AddUser(SignUpModel model)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                Users user = new Users()
                {
                    RoleID = 3,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailID = model.EmailID,
                    Password = model.Password,
                    IsEmailVerified = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = 1,
                    ModifiedDate = DateTime.Now,
                    ModifiedBy = 1,
                    IsActive = false
                };

                context.Users.Add(user);
                context.SaveChanges();
                return user.UserID;
            }
        }

        public SignUpModel GetUser(int id)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var result = context.Users
                    .Where(x => x.UserID == id)
                    .Select(x => new SignUpModel()
                    {
                        UserID = x.UserID,
                        FirstName = x.FirstName
                    }).FirstOrDefault();
                return result;
            }
        }
    }
}
