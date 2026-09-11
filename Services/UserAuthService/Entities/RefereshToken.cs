namespace UserAuthService.Entities;

public class RefreshToken{
    public int Id{get;set;}
    public int UserId{get;set;}
    public string TokenHash{get;set;}=default!;

    public DateTime IssuedAt{get;set;}

    public DateTime ExpiresAt{get;set;}

    public DateTime? RevokedAt{get;set;}

    public User User {get;set;}=default!;
}