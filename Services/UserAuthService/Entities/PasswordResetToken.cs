namespace UserAuthService.Entities;

public class PasswordResetToken
{
    public int Id{get;set;}

    public int UserId{get;set;}
    public string TokenHash{get;set;}=default!;
    public DateTime ExpiresAt{get;set;}
    public DateTime? UsedAt{get;set;}


    public User User { get; set; } = default!;
}