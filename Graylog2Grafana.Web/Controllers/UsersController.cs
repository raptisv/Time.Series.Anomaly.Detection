using Graylog2Grafana.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Time.Series.Anomaly.Detection.Data.Abstractions;

namespace Graylog2Grafana.Web.Controllers
{
    [Authorize(Roles = "admin")]
    public class UsersController : Controller
    {
        private UserManager<IdentityUser> _userManager;
        private readonly IUsersService _usersService;

        public UsersController(
            UserManager<IdentityUser> userManager, 
            IUsersService usersService)
        {
            _userManager = userManager;
            _usersService = usersService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _usersService.GetAllAsync();

            return View(users);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Create(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            IdentityUser newUser = new IdentityUser()
            {
                UserName = model.Username,
                Email = model.Username,
                NormalizedEmail = model.Username.ToUpper(),
                NormalizedUserName = model.Username.ToUpper(),
                LockoutEnabled = false,
                TwoFactorEnabled = false,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            PasswordHasher<IdentityUser> passwordHasher = new PasswordHasher<IdentityUser>();
            newUser.PasswordHash = passwordHasher.HashPassword(newUser, model.Password);

            var result = await _userManager.CreateAsync(newUser);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Users");
            }
            else
            {
                ModelState.AddModelError("CustomError", string.Join(", ", result.Errors.Select(x => x.Description)));
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            var userRoles = await _userManager.GetRolesAsync(user);

            if (userRoles.Any(x => x == "admin"))
            {
                throw new Exception("Cannot delete admin");
            }

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(IdentityUser model)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                var userRoles = await _userManager.GetRolesAsync(user);

                if (userRoles.Any(x => x == "admin"))
                {
                    throw new Exception("Cannot delete admin");
                }

                await _usersService.DeleteAsync(model.Id);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("CustomError", ex.Message);
                return View(model);
            }

            return RedirectToAction("Index", "Users");
        }
    }
}
