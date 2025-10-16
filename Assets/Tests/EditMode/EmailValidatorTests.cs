
using NUnit.Framework;
using System.Text.RegularExpressions;

public static class EmailValidator
{
    static readonly Regex Rx = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValid(string email) =>
        !string.IsNullOrWhiteSpace(email) && Rx.IsMatch(email);
}

public class EmailValidatorTests
{
    [TestCase("a@b.com", true)]
    [TestCase("user.name+tag@domain.io", true)]
    [TestCase("bad@", false)]
    [TestCase("@bad.com", false)]
    [TestCase("", false)]
    public void IsValid_ReturnsExpected(string email, bool expected)
        => Assert.AreEqual(expected, EmailValidator.IsValid(email));
}
