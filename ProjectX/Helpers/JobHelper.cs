using Microsoft.EntityFrameworkCore;
using ProjectX.Data;
using ProjectX.Models;

namespace ProjectX.Helpers;

public class JobHelper
{
    public static bool IsValidProJob(Job job, bool isHighlight, bool isHot, bool isUrgent)
    {
        ArgumentNullException.ThrowIfNull(job);

        var jobDuration = (job.EndDate - job.StartDate).TotalDays;
        switch (jobDuration)
        {
            case > 14 when isHot:
            case > 7 when isUrgent:
            case > 14 when isHighlight:
                return false;
            default:
                return true;
        }
    }


    public static int CalcXToken4Highlight(DateTime startDate, DateTime endDate, int xTokenPrice)
    {
        var duration = (int)(endDate - startDate).TotalDays;
        return duration * xTokenPrice;
    }
}