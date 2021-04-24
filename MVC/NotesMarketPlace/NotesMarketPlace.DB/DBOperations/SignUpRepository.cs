using NotesMarketPlace.Models;
using System;
using System.Linq;


namespace NotesMarketPlace.DB.DBOperations
{
    public class SignUpRepository
    {
        public int AddUser(SignUpModel model)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var users = context.Users.Where(m => m.EmailID == model.EmailID).ToList();

                if (users.Count == 0)
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
                return 0;
            }
        }
    }
}
