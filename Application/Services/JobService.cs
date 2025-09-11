using AutoMapper;
using WsUtaSystem.Application.Common.Interfaces;
using WsUtaSystem.Application.Common.Services;
using WsUtaSystem.Application.DTOs.Jobs;
using WsUtaSystem.Application.Interfaces.Repositories;
using WsUtaSystem.Application.Interfaces.Services;
using WsUtaSystem.Models;

namespace WsUtaSystem.Application.Services
{
    public class JobService : Service<Job, int>, IJobService
    {
        private readonly IJobRepository _repository;
        private readonly IMapper _mapper;

        public JobService(IJobRepository repo) : base(repo)
        {
            _repository = repo;
        }
        public async Task<IEnumerable<Job>> GetActiveJobsAsync(CancellationToken ct)
        {
            return await _repository.GetActiveJobsAsync(ct);
        }

        public async Task<IEnumerable<Job>> SearchJobsByTitleAsync(string title, CancellationToken ct)
        {
            return await _repository.SearchJobsByTitleAsync(title,ct);
        }

    }
}
