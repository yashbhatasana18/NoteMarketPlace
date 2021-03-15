using System;
using System.Collections.Generic;
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

        public List<SignUpModel> GetAllUser()
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var result = context.Users
                    .Select(x => new SignUpModel()
                    {
                        UserID = x.UserID,
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        EmailID = x.EmailID,
                        Password = x.Password,
                        ConfirmPassword = x.Password,
                        IsEmailVerified = x.IsEmailVerified,
                        CreatedDate = x.CreatedDate,
                        CreatedBy = x.CreatedBy,
                        ModifiedDate = x.ModifiedDate,
                        ModifiedBy = x.ModifiedBy,
                        IsActive = x.IsActive,
                        UserRoles = new UserRolesModel()
                        {
                            UserRolesID = x.UserRoles.UserRolesID,
                            Name = x.UserRoles.Name
                        }
                    }).ToList();

                return result;
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
                        FirstName = x.FirstName,
                        LastName = x.LastName,
                        EmailID = x.EmailID,
                        Password = x.Password,
                        ConfirmPassword = x.Password,
                        IsEmailVerified = x.IsEmailVerified,
                        CreatedDate = x.CreatedDate,
                        CreatedBy = x.CreatedBy,
                        ModifiedDate = x.ModifiedDate,
                        ModifiedBy = x.ModifiedBy,
                        IsActive = x.IsActive,
                        UserRoles = new UserRolesModel()
                        {
                            UserRolesID = x.UserRoles.UserRolesID,
                            Name = x.UserRoles.Name
                        }
                    }).FirstOrDefault();

                return result;
            }
        }

        public bool EditUsers(int id, SignUpModel model)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var user = context.Users.FirstOrDefault(x => x.UserID == id);
                if (user != null)
                {
                    user.FirstName = model.FirstName;
                    user.RoleID = model.RoleID;
                    user.LastName = model.LastName;
                    user.EmailID = model.EmailID;
                    user.Password = model.Password;
                    user.IsEmailVerified = model.IsEmailVerified;
                    user.ModifiedDate = DateTime.Now;
                    user.ModifiedBy = model.UserID;
                }
                context.SaveChanges();
                return true;
            }
        }
    }
}
