using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Features.HoaDonModule
{
    public interface IHoaDonRepository
    {
        Task<IEnumerable<Contract>> GetActiveContractsAsync();
        Task<IEnumerable<Contract>> GetContractsByRoomIdAsync(int roomId);
        Task<IEnumerable<TenantRoom>> GetTenantsInRoomAsync(int roomId);
    }
}