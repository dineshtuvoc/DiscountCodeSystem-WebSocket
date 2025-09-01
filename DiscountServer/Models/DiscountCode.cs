namespace DiscountServer.Models;

public class DiscountCode
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
}