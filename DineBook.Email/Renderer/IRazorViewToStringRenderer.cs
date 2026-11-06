using System.Threading.Tasks;

namespace DineBook.Email.Renderer
{
    public interface IRazorViewToStringRenderer
    {
        Task<string> RenderViewToStringAsync<TModel>(string viewName, TModel model);
    }
}