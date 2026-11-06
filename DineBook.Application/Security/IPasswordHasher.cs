namespace DineBook.Application.Security
{
    public interface IPasswordHasher
    {
        /// <summary>Hashes a password with a new random salt.</summary>
        string Hash(string plaintextPassword);

        /// <summary>Checks a password against a hash made by <see cref="Hash"/>. Takes the same time for any mismatch.</summary>
        bool Verify(string plaintextPassword, string hashedPassword);
    }
}
