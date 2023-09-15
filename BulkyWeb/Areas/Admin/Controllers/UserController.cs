using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.AspNetCore.Mvc.Rendering;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using BulkyBook.DataAccess.Repository;
using Microsoft.AspNetCore.Identity;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        public UserController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult RoleManagment(string userId)
        {
            string RoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == userId).RoleId;

            ApplicationUserRoleVM applicationUserRoleVM = new ApplicationUserRoleVM()
            {
                ApplicationUser = _db.ApplicationUsers.Include(u => u.Company).FirstOrDefault(u => u.Id == userId),
                CompanyList = _db.Companies.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                RoleList = _db.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                })
            };

            applicationUserRoleVM.ApplicationUser.Role = _db.Roles.FirstOrDefault(u => u.Id == RoleID).Name;

            return View(applicationUserRoleVM);
        }

        [HttpPost]
        public IActionResult RoleManagment(ApplicationUserRoleVM applicationUserRoleVM)
        {
            string RoleID = _db.UserRoles.FirstOrDefault(u => u.UserId == applicationUserRoleVM.ApplicationUser.Id).RoleId;
            string oldRole = _db.Roles.FirstOrDefault(u => u.Id == RoleID).Name;

            if(!(applicationUserRoleVM.ApplicationUser.Role == oldRole))
            {
                // Role has been updated
                ApplicationUser applicationUser = _db.ApplicationUsers.FirstOrDefault(u => u.Id == applicationUserRoleVM.ApplicationUser.Id);
                if(applicationUserRoleVM.ApplicationUser.Role == SD.Role_Company)
                {
                    applicationUser.CompanyId = applicationUserRoleVM.ApplicationUser.CompanyId;

                }
                if(oldRole == SD.Role_Company)
                {
                    applicationUser.CompanyId = null;
                }

                _userManager.RemoveFromRoleAsync(applicationUser, oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(applicationUser, applicationUserRoleVM.ApplicationUser.Role).GetAwaiter().GetResult();
            }

            return RedirectToAction(nameof(Index));
        }

        #region APICALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            List<ApplicationUser> objUsersList = _db.ApplicationUsers.Include(u=>u.Company).ToList();


            var userRoles = _db.UserRoles.ToList();
            var roles = _db.Roles.ToList();

            foreach(var user in objUsersList)
            {
                var roleId = userRoles.FirstOrDefault(u=>u.UserId == user.Id).RoleId;
                user.Role = roles.FirstOrDefault(u=>u.Id==roleId).Name;

                if(user.Company == null) {
                    user.Company = new() { Name = "" };
                }
            }

            return Json(new { data = objUsersList });
        }

        [HttpPost]
        public IActionResult LockUnlock([FromBody]string? id)
        {

            var objFromDb = _db.ApplicationUsers.FirstOrDefault(u => u.Id == id);
            if (objFromDb == null)
            {
                return Json(new { success = false, message = "Error while Locking/Unlocking" });
            }

            if (objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //user is currently locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;
            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            
            _db.SaveChanges();

            return Json(new { success = true, message = "Operation Successful" });
        }


        #endregion
    }
}
