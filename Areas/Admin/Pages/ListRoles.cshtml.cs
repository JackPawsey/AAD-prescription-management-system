using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace AADWebApp.Areas.Admin.Pages
{
    [Authorize(Roles = "Admin")]
    public class ListRolesModel : PageModel
    {
        private readonly RoleManager<IdentityRole> RoleManager;

        [BindProperty]
        public string RoleID { get; set; }

        public ListRolesModel(RoleManager<IdentityRole> roleManager)
        {
            RoleManager = roleManager;
        }

        public void OnGet()
        {
            if (!User.Identity.IsAuthenticated)
            {
                Response.Redirect("/");
            }
        }

        public async Task<IActionResult> OnPost()
        {
            Console.WriteLine("Role to delete: " + RoleID);

            IdentityRole Role = await RoleManager.FindByIdAsync(RoleID);

            if (Role != null)
            {
                var result = await RoleManager.DeleteAsync(Role);

                if (result.Succeeded)
                {
                    Response.Redirect("/Admin/ListRoles");
                }
                else
                {
                    foreach (IdentityError error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            else
            {
                ModelState.AddModelError("", "Role not found");
            }
            
            return Page();
        }
    }
}
