using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;

namespace Server.Pages.Admin;

public class ReportDisplay
{
    public ReportDisplay() { }

    public ReportDisplay(Report report, IProfilePictureService pfpService)
    {
        ReportCount = report.ReportCount;
        Username = report.Username;
        Name = report.Name;
        Description = report.Description;
        PfpUrl = report.HasPfp ? pfpService.GetReportUrl(report.Username, report.PfpVersion) : null;
        Id = report.Id;
        IgHandle = report.IgHandle;
        Gender = report.Gender;
    }
    
    public int Id { get; set; }
    
    public int ReportCount { get; set; }
    
    public string Username { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set;}
    public string? IgHandle { get; set;}
    
    public string? PfpUrl { get; set; }
    
    public Gender Gender { get; set; }
}

public class Reports(ApplicationDbContext dbContext, IProfilePictureService pfpService) : PageModel
{
    public List<ReportDisplay> AllReports { get; set; }
    public async Task OnGetAsync()
    {
        AllReports = await dbContext.Reports.Select(r => new ReportDisplay(r, pfpService)).ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int reportId)
    {
        var report = await dbContext.Reports.FirstAsync(r => r.Id == reportId);
        if (report.HasPfp)
            await pfpService.DeleteReportPictureAsync(report.Username, report.PfpVersion);

        dbContext.Reports.Remove(report);
        await dbContext.SaveChangesAsync();

        return RedirectToPage("/Admin/Reports");
    }
}