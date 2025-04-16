namespace ProjectX.Helpers;

public static class OrderHelper
{
    public static decimal CalculateHighlightCost(int days)
    {
        if (days < 1 || days > 30) throw new ArgumentException("Days must be between 1 and 30.");

        decimal cost = 10000;
        if (days > 1)
        {
            var days2to7 = Math.Min(days - 1, 6);
            cost += days2to7 * 2000;
        }

        if (days <= 7) return cost;
        var days8to30 = days - 7;
        cost += days8to30 * 3000;

        return cost;
    }

    public static int CalculateHighlightDays(DateTime startDate, DateTime endDate)
    {
        var totalDays = (endDate - startDate).TotalDays;
        if (totalDays is < 1 or > 30) throw new ArgumentException("Total days must be between 1 and 30.");

        return (int)totalDays;
    }
}