using Microsoft.EntityFrameworkCore;
using smart_hostel_management_system.Data;
using smart_hostel_management_system.Models.Core;

namespace smart_hostel_management_system.Features.HoaDonModule
{
    public class HoaDonRepository : IHoaDonRepository
    {
        private readonly AppDbContext _context;

        // Inject DbContext 
        public HoaDonRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Contract>> GetActiveContractsAsync()
        {
            return await _context.Contracts
                                 .Include(c => c.Room)
                                 .Include(c => c.Tenant)
                                 .Where(c => c.Status == ContractStatus.Active)
                                 .ToListAsync();
        }

        // for feature find contract history 
        public async Task<IEnumerable<Contract>> GetContractsByRoomIdAsync(int roomId)
        {
            return await _context.Contracts
                                 .Include(c => c.Room)
                                 .Include(c => c.Tenant)
                                 .Where(c => c.RoomId == roomId)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<TenantRoom>> GetTenantsInRoomAsync(int roomId)
        {
            return await _context.TenantRooms
                .Include(tr => tr.Tenant) // Lôi kèm bảng Tenant để lấy thông tin FullName công dân
                .Where(tr => tr.RoomId == roomId && tr.IsCurrent == true) // Lọc đúng người "vẫn đang ở"
                .ToListAsync();
        }
    }
}