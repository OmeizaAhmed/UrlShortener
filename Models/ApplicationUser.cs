using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
namespace UrlShortener.Models
{
  public class ApplicationUser: IdentityUser
  {

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ShortUrl> ShortUrls { get; set; } = new List<ShortUrl>();
  }
}