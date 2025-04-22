using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;
using Server.Services.Interfaces;

namespace Server.Pages.Admin
{
    public class UsersModel(UserManager<ApplicationUser> userManager, ApplicationDbContext dbContext, IProfilePictureService pfpService)
        : PageModel
    {
        public async Task OnGetAsync()
        {
            Users = await userManager.Users
                .OrderBy(u => u.CreatedAt == null)
                .ThenBy(u => u.CreatedAt)
                .Skip(PageId * 10).Take(10).ToListAsync();
        }

        public async Task<IActionResult> OnPostShadowBanAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            user.ShadowBanned = !user.ShadowBanned;

            await userManager.UpdateAsync(user);
            
            return RedirectToPage(new { pageId = PageId });
        }

        public async Task<IActionResult> OnPostDeleteAsync(string userId)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (user.HasPfp)
            {
                await pfpService.RemoveProfilePictureAsync(user.UserName);
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            
            var reports = await dbContext.Reports.Where(r => r.Username == user.UserName).ToListAsync();
            
            foreach (var report in reports.Where(report => report.HasPfp))
            {
                await pfpService.DeleteReportPictureAsync(report.Username, report.PfpVersion);
            }
            dbContext.Reports.RemoveRange(reports);
            await dbContext.SaveChangesAsync();
            
            return RedirectToPage(new { pageId = PageId });
        }

        [FromRoute]
        public int PageId { get; set; }

        public List<ApplicationUser> Users { get; set; }
    }
}
