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

        //public int UserProfile(UserProfileModel model)
        //{
        //    using (var context = new NotesMarketPlaceEntities())
        //    {
        //        UserProfile user = new UserProfile()
        //        {
        //            UserID = model.UserID,
        //            DOB = model.DOB,
        //            Gender = model.Gender,
        //            SecondaryEmailAddress = model.SecondaryEmailAddress,
        //            PhoneNumberCountryCode = model.PhoneNumberCountryCode,
        //            PhoneNumber = model.PhoneNumber,
        //            ProfilePicture = model.ProfilePicture,
        //            AddressLine1 = model.AddressLine1,
        //            AddressLine2 = model.AddressLine2,
        //            City = model.City,
        //            State = model.State,
        //            ZipCode = model.ZipCode,
        //            Country = model.Country,
        //            University = model.University,
        //            College = model.College,
        //            CreatedDate = DateTime.Now,
        //            CreatedBy = model.UserID,
        //            ModifiedDate = DateTime.Now,
        //            ModifiedBy = model.UserID
        //        };

        //        context.UserProfile.Add(user);
        //        context.SaveChanges();
        //        return user.UserProfileID;
        //    }
        //}

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
                var user = new Users();
                if (user != null)
                {
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.EmailID = model.EmailID;
                    user.Password = model.Password;
                    user.ModifiedDate = DateTime.Now;
                    user.ModifiedBy = model.UserID;
                }
                context.Entry(user).State = System.Data.Entity.EntityState.Modified;
                context.SaveChanges();
                return true;
            }
        }

        public bool DeleteUsers(int id)
        {
            using (var context = new NotesMarketPlaceEntities())
            {
                var user = new Users()
                {
                    UserID = id
                };
                context.Entry(user).State = System.Data.Entity.EntityState.Deleted;
                context.SaveChanges();

                return false;
            }
        }
    }
}
