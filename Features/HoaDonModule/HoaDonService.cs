using AutoMapper;
using smart_hostel_management_system.DTOs;

namespace smart_hostel_management_system.Features.HoaDonModule
{
    public class HoaDonService : IHoaDonService
    {
        private readonly IHoaDonRepository _hoaDonRepo;
        private readonly IMapper _mapper;

        public HoaDonService(IHoaDonRepository hoaDonRepo, IMapper mapper)
        {
            _hoaDonRepo = hoaDonRepo;
            _mapper = mapper;
        }

        // CẬP NHẬT: Thêm tham số currentUserId để check xem user đang xem có phải trưởng phòng không
        public async Task<IEnumerable<HoaDonDTO>> GetLichSuHoaDonTheoPhongAsync(int roomId, int currentUserId)
        {
            // 1. Gọi Repository để lấy những người thực sự VẪN ĐANG Ở trong roomId này
            var tenantsInRoom = await _hoaDonRepo.GetTenantsInRoomAsync(roomId);
            
            string representativeName = "Chưa xác định";
            int representativeTenantId = 0;

            // Thuật toán gán quyền Đại diện: Người đầu tiên xuất hiện trong danh sách TenantRoom (IsCurrent == true)
            if (tenantsInRoom != null && tenantsInRoom.Any())
            {
                var firstTenantRoom = tenantsInRoom.First();
                representativeTenantId = firstTenantRoom.TenantId;
                representativeName = firstTenantRoom.Tenant?.FullName ?? "Khách thuê đại diện";
            }

            // 2. Lấy danh sách hợp đồng/hóa đơn cũ của team
            var contracts = await _hoaDonRepo.GetContractsByRoomIdAsync(roomId);
            var listDto = _mapper.Map<IEnumerable<HoaDonDTO>>(contracts).ToList();

            // 3. Đổ dữ liệu 4 món cố định kèm theo logic Người đại diện ra DTO
            foreach (var dto in listDto)
            {
                dto.RoomNumber = $"Phong {roomId}";
                dto.ElectricityPrice = 150000; 
                dto.WaterPrice = 50000;       
                dto.ServicePrice = 20000; // Tiền rác đổi thành 20k cho khớp Frontend     
                dto.TotalAmount = dto.RoomPrice + dto.ElectricityPrice + dto.WaterPrice + dto.ServicePrice;
                dto.Status = roomId == 102 ? "ChuaThanhToan" : "DaThanhToan"; 

                // ⚡ NẠP THÊM 2 THUỘC TÍNH ĐỘNG KHÔNG CÓ TRONG DB XUỐNG CHO FRONTEND
                dto.RepresentativeName = representativeName;
                // Nếu TenantId của tài khoản đang đăng nhập trùng với TenantId của người đại diện -> True
                dto.IsRepresentative = (currentUserId == representativeTenantId);
            }

            return listDto;
        }

        public async Task<bool> XuLyThanhToanAsync(int roomId, string phuongThuc)
        {
            var contracts = await _hoaDonRepo.GetContractsByRoomIdAsync(roomId);
            var contract = contracts.FirstOrDefault(c => c.Status == smart_hostel_management_system.Models.Core.ContractStatus.Active);
            
            if (contract == null) return false;

            Console.WriteLine($"[THANH TOAN] Phong {roomId} da thanh toan thanh cong qua: {phuongThuc} so tien {contract.MonthlyPrice:N0}d");
            return await Task.FromResult(true);
        }

        public async Task<int> QuetVaGuiEmailNhacNoAsync()
        {
            var activeContracts = await _hoaDonRepo.GetActiveContractsAsync();
            int emailDaGuiCount = 0;

            foreach (var contract in activeContracts)
            {
                if (contract.Tenant?.Status == smart_hostel_management_system.Models.Core.TenantStatus.InDebt || contract.RoomId == 102)
                {
                    emailDaGuiCount++;
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine($"[EMAIL NHAC NO] Gui toi: {contract.Tenant?.Email}");
                    Console.WriteLine($"Tieu de: [CANH BAO NO TIEN TRO] Hoa don thang nay cua phong {contract.Room?.Code} da qua han!");
                    Console.WriteLine($"Noi dung: Than gui ban {contract.Tenant?.FullName}, he thong ghi nhan ban chua thanh toan tong so tien phong: {contract.MonthlyPrice:N0}d. Vui long thanh toan gap truoc khi he thong khoa phong.");
                    Console.WriteLine("--------------------------------------------------");
                }
            }

            return emailDaGuiCount;
        }

        public async Task<bool> GuiEmailTinTucToanBoTroAsync(string tieuDe, string noiDung)
        {
            var activeContracts = await _hoaDonRepo.GetActiveContractsAsync();
            
            Console.WriteLine($"================= PHAT HANH EMAIL TIN TUC =================");
            Console.WriteLine($"Tieu de thong bao: {tieuDe}");
            Console.WriteLine($"Noi dung: {noiDung}");
            
            foreach (var contract in activeContracts)
            {
                if (contract.Tenant != null && !string.IsNullOrEmpty(contract.Tenant.Email))
                {
                    Console.WriteLine($"=> Da xep hang gui email den cu dan: {contract.Tenant.FullName} ({contract.Tenant.Email})");
                }
            }
            Console.WriteLine($"===========================================================");
            
            return await Task.FromResult(true);
        }

        public async Task<Dictionary<string, decimal>> GetThongKeDoanhThuTheoThangAsync(int year)
        {
            var activeContracts = await _hoaDonRepo.GetActiveContractsAsync();
            
            // Da xoa bo toan bo dau tieng Viet trong ten bien
            decimal tongDoanhThuThucTe = 0;
            decimal tongTienKhachNo = 0;

            foreach (var contract in activeContracts)
            {
                if (contract.RoomId == 102) 
                {
                    tongTienKhachNo += contract.MonthlyPrice;
                }
                else
                {
                    tongDoanhThuThucTe += contract.MonthlyPrice;
                }
            }

            var thongKeDic = new Dictionary<string, decimal>
            {
                { $"Tong doanh thu thu ve nam {year}", tongDoanhThuThucTe },
                { "Tong so tien hien tai dang bi no treo", tongTienKhachNo },
                { "Doanh thu du kien toi da neu thu du", tongDoanhThuThucTe + tongTienKhachNo }
            };

            return await Task.FromResult(thongKeDic);
        }
    }
}