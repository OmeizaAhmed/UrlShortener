namespace UrlShortener.Services;
public interface IPasswordService
{
  string HashPassword(string password);
  bool VerifyPassword(string text, string hash);
}

public class PasswordService: IPasswordService
{
  public string HashPassword(string password)
  {
    return BCrypt.Net.BCrypt.EnhancedHashPassword(password);
  }

  public bool VerifyPassword(string text, string hash)
  {
    return BCrypt.Net.BCrypt.EnhancedVerify(text, hash);
  }
}