using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;
namespace WsUtaSystem.Application.Services;
public class WorkExperiencesService : Service<WorkExperiences, int>, IWorkExperiencesService
{
    public WorkExperiencesService(IWorkExperiencesRepository repo) : base(repo) { }
}
