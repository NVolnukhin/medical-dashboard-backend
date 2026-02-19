using System.Security.Cryptography;

namespace AuthService.Services.Password
{
    public class OneTimePasswordGenerator : IOneTimePasswordGenerator
    {
        private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const string SpecialChars = "!@#$%^&*";
        private const string AllChars = UpperCase + LowerCase + Digits + SpecialChars;

        public string GeneratePassword(int length)
        {
            if (length < 8)
                throw new ArgumentException("Password length must be at least 8 characters.", nameof(length));

            // Ensure at least one character from each category
            var password = new char[length];
            var random = new Random(GetSecureSeed());

            password[0] = UpperCase[random.Next(UpperCase.Length)];
            password[1] = LowerCase[random.Next(LowerCase.Length)];
            password[2] = Digits[random.Next(Digits.Length)];
            password[3] = SpecialChars[random.Next(SpecialChars.Length)];

            // Fill the rest randomly
            for (int i = 4; i < length; i++)
            {
                password[i] = AllChars[random.Next(AllChars.Length)];
            }

            // Shuffle the password
            return new string(password.OrderBy(_ => random.Next()).ToArray());
        }

        private static int GetSecureSeed()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] seedBytes = new byte[4];
                rng.GetBytes(seedBytes);
                return BitConverter.ToInt32(seedBytes, 0);
            }
        }
    }
}