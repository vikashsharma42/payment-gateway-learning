namespace DineBook.Infrastructure.Database
{
    /// <summary>
    /// Supplies the signed-in user's name for the CreatedBy and UpdatedBy audit fields.
    /// The host registers it. Without one, "system" is written.
    /// </summary>
    public interface ICurrentUserProvider
    {
        string UserName { get; }
    }
}
