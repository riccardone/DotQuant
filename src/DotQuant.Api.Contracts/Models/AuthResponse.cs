namespace DotQuant.Api.Contracts.Models;

public class AuthResponse
{
    public AuthResponse(string accessToken, string givenName, string surname, string role, string email, string address,
        string postCode, string city, string phone, string countryCode)
    {
        AccessToken = accessToken;
        GivenName = givenName;
        Surname = surname;
        Role = role;
        Email = email;
        Address = address;
        PostCode = postCode;
        City = city;
        Phone = phone;
        CountryCode = countryCode;
    }

    public string AccessToken { get; }
    public string GivenName { get; }
    public string Surname { get; }
    public string Email { get; }
    public string Address { get; }
    public string PostCode { get; }
    public string City { get; }
    public string Phone { get; }
    public string CountryCode { get; }
    public string Role { get; }
}