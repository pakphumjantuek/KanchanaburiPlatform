using Application.DTOs;
using Application.Results;

namespace Core.Interfaces;

public interface IAdminDashboardService
{
    Task<ServiceResult<AdminDashboardDto>> GetDashboardAsync(
        DateTime? from,
        DateTime? to);
}
