using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class BooksService : Service<Books, int>, IBooksService
{
    public BooksService(IBooksRepository repo) : base(repo) { }
}
